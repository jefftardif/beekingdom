using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Server;

// M056-CL: full, IRREVERSIBLE deletion of one player account, reachable only from the Admin-gated
// DELETE /accounts/v1/admin/accounts/{accountId} endpoint.
//
// Why a real delete and not a status flag: Jeff's requirement is that the email address becomes
// free to register a brand-new account with ("pour tester c'est un imperatif de pouvoir
// recommencer"). dbo.AuthenticationAccounts.Email carries a UNIQUE index, so anything short of
// removing that row leaves the address permanently burned. Note that BeeKingdom.Accounts'
// AccountService.DeleteAccount (AccountStatus.Deleted) is NOT this: it flags a record in the
// dormant AccountManager/AccountProfile subsystem that no login path ever consults.
//
// Ordering is deliberate. Everything that can fail loudly and is reversible-by-retry runs BEFORE
// the credential row is destroyed, so a mid-cascade failure leaves an account that is still
// findable and can simply be deleted again. The credential row - the one irreversible, identity-
// bearing step - is always last.
public sealed class AccountDeletionService(
    IAccountCredentialStore credentials,
    IHiveStateRepository hives,
    IChatRepository chat,
    AllianceService alliances,
    AuthenticationManager authentication)
{
    public sealed record Summary(
        Guid AccountId,
        Guid PlayerId,
        string Email,
        string? DisplayName,
        int HivesDeleted,
        IReadOnlyList<Guid> DeletedHiveIds,
        int ChatConversationsLeft,
        int ChatRecordsPurged,
        Guid? AllianceIdLeft,
        string? AllianceNameLeft,
        int AllianceApplicationsCancelled,
        int AllianceInvitationsRevoked,
        int SessionsRevoked,
        bool CredentialRowDeleted);

    public sealed record Detail(
        Guid AccountId,
        Guid PlayerId,
        string Email,
        string? DisplayName,
        AccountRole Role,
        AccountSecurityState SecurityState,
        bool IsOnboarded,
        bool HasGoogleLogin,
        int HiveCount,
        IReadOnlyList<Guid> HiveIds,
        Guid? AllianceId,
        string? AllianceName,
        string? AllianceRole);

    // Thrown when the account cannot be safely deleted as-is. Carries a stable code the endpoint
    // maps to an HTTP status and the UI maps to a message.
    public sealed class BlockedException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }

    public async Task<Detail?> LookupByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !credentials.TryGetByEmail(email.Trim(), out AuthenticationAccount account)) return null;
        return await DescribeAsync(account, cancellationToken);
    }

    public async Task<Detail?> LookupByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (!credentials.TryGetByAccountId(accountId, out AuthenticationAccount account)) return null;
        return await DescribeAsync(account, cancellationToken);
    }

    private async Task<Detail> DescribeAsync(AuthenticationAccount account, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> hiveIds = await hives.ListHiveIdsAsync(account.PlayerId.Value, cancellationToken);
        AllianceMembership? membership = alliances.FindActiveMembership(account.PlayerId);
        string? allianceName = membership is null ? null : alliances.FindAlliance(membership.AllianceId)?.Name;
        return new Detail(
            account.AccountId,
            account.PlayerId.Value,
            account.Email,
            account.DisplayName,
            account.Role,
            account.State,
            account.IsOnboarded,
            !string.IsNullOrEmpty(account.GoogleSubjectId),
            hiveIds.Count,
            hiveIds,
            membership?.AllianceId.Value,
            allianceName,
            membership?.Role.ToString());
    }

    // expectedEmail is a second, independent confirmation supplied by the caller: the admin UI
    // makes the operator TYPE the address, and we re-check it against the account id here at
    // execution time. Cheap insurance against a stale search result (or a copy-pasted id from an
    // older session) resolving to a different account than the one on screen.
    public async Task<Summary> DeleteAsync(Guid accountId, string expectedEmail, CancellationToken cancellationToken = default)
    {
        if (!credentials.TryGetByAccountId(accountId, out AuthenticationAccount account))
            throw new BlockedException("account.not_found", "No account with that id.");

        if (string.IsNullOrWhiteSpace(expectedEmail) || !string.Equals(account.Email.Trim(), expectedEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BlockedException("account.email_mismatch", "The confirmation email does not match this account id.");

        // An Admin deleting themselves would lock the last operator out of every admin surface,
        // including this one. Refuse outright - there is no legitimate reason for it here.
        if (account.Role == AccountRole.Admin)
            throw new BlockedException("account.admin_not_deletable", "Admin accounts cannot be deleted through this tool.");

        PlayerId playerId = account.PlayerId;

        // 1. Alliance. Runs first because it is the only step that can legitimately REFUSE - and it
        // must refuse before anything has been destroyed. Reuses the same removal path as
        // kick/leave so member counts, chat participation and help requests stay consistent.
        AllianceAccountDeletionResult? allianceResult;
        try
        {
            allianceResult = alliances.RemoveForAccountDeletion(playerId);
        }
        catch (InvalidOperationException ex) when (ex.Message == "leader_must_transfer_or_dissolve")
        {
            throw new BlockedException(
                "account.alliance_leader",
                "This player leads an alliance. Transfer leadership or dissolve the alliance first, then delete the account.");
        }

        // 2. Chat. Remove them as a participant everywhere they take part, then purge their
        // personal inbox/receipt rows. Messages they authored are deliberately LEFT IN PLACE - see
        // IChatRepository.DeletePlayerChatFootprint for the reasoning.
        int conversationsLeft = 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (ChatConversation conversation in chat.ListConversations(playerId, 0, ChatConversationSweepLimit))
        {
            if (chat.RemoveParticipant(conversation.ConversationId, playerId, now) != null) conversationsLeft++;
        }
        int chatRecordsPurged = chat.DeletePlayerChatFootprint(playerId);

        // 3. Hive state. One delete per owned hive; this is what actually removes Royal Seals,
        // reward ledger, VIP/Champion progression, SpeedUps and every stored receipt.
        var deletedHiveIds = new List<Guid>();
        foreach (Guid hiveId in await hives.ListHiveIdsAsync(playerId.Value, cancellationToken))
        {
            if (await hives.DeleteAsync(playerId.Value, hiveId, cancellationToken)) deletedHiveIds.Add(hiveId);
        }

        // 4. Sessions, then the credential row itself. Revoking first means no window exists where
        // a live bearer token outlives the account in the in-memory session store.
        int sessionsRevoked = authentication.LogoutAllSessions(accountId);
        bool credentialDeleted = credentials.DeleteAccount(accountId);

        return new Summary(
            accountId,
            playerId.Value,
            account.Email,
            account.DisplayName,
            deletedHiveIds.Count,
            deletedHiveIds,
            conversationsLeft,
            chatRecordsPurged,
            allianceResult?.AllianceId,
            allianceResult?.AllianceName,
            allianceResult?.CancelledApplications ?? 0,
            allianceResult?.RevokedInvitations ?? 0,
            sessionsRevoked,
            credentialDeleted);
    }

    // ListConversations is paged; no player in this game is in anywhere near this many
    // conversations (alliance + a handful of directs), and an unbounded sweep here would be a
    // needless full-table walk on a destructive path.
    private const int ChatConversationSweepLimit = 500;
}
