namespace BeeKingdom.Chat.Audience;

// M043Q-CL: same dependency-inversion seam as IAllianceMembershipResolver - ChatService needs the
// sender's real, onboarded public DisplayName (BeeKingdom.Authentication.AuthenticationAccounts via
// PlayerDirectoryService, the authoritative source established in M043P) to snapshot onto each
// ChatMessage, but BeeKingdom.Chat deliberately stays independent of BeeKingdom.Accounts (a lower
// module should not depend on a higher one just to resolve a display string). BeeKingdom.Accounts
// provides the real implementation (wrapping IPlayerDirectoryService.GetByPlayerId) and registers
// it in DI, same as BeeKingdom.Alliance does for IAllianceMembershipResolver; BeeKingdom.Chat ships
// a safe default (NullChatSenderDisplayNameResolver) so the chat module still compiles/runs
// standalone - null here means "resolver not wired up", which ChatService falls back from, never a
// fabricated name.
public interface IChatSenderDisplayNameResolver
{
    string? ResolveDisplayName(Guid playerId);
}

public sealed class NullChatSenderDisplayNameResolver : IChatSenderDisplayNameResolver
{
    public string? ResolveDisplayName(Guid playerId) => null;
}
