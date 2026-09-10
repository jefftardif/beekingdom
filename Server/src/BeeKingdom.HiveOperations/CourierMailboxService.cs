using System.Linq;

namespace BeeKingdom.HiveOperations;

public sealed class CourierMailboxOptions
{
    public const string SectionName = "CourierMailbox";
    public bool Enabled { get; set; }
}

// M076I-CL: le courrier (rapports de combat, invitations d'Alliance, recompenses) n'etait
// qu'un List<> cote client (HiveViewProductUiPresenter.courierMessages), perdu a chaque
// rechargement de domaine ou reinstallation - CEO explicite (apres un premier correctif
// PlayerPrefs local juge insuffisant) : "il faut que ce soit sauvegarde sur le serveur, nous
// ne conservons rien localement". Le CLIENT reste seul responsable de DERIVER chaque message
// (lire RecentClaimReceipts d'un combat deja resolu, les invitations d'Alliance en attente -
// deja lues depuis leurs propres systemes serveur authoritatifs ailleurs) ; ce service se
// contente de PERSISTER la boite de reception deja construite, jamais de nouvelle logique de
// combat/alliance elle-meme (Combat Patrol reste gele - voir CombatPatrolService/CLAUDE.md).
// Append idempotent par Id de message (deterministe cote client, ex. "combat:<encounterId>",
// "alliance:<invitationId>") plutot qu'une cle d'idempotence+revision separee comme QuestChain :
// un append est par nature commutatif, aucune ambiguite metier a rejouer "ajouter ce message"
// deux fois - le second appel est simplement ignore.
public sealed class CourierMailboxService(IHiveStateRepository repository, IServerClock clock, CourierMailboxOptions options)
{
    public const string ContractVersion = "living-hive-courier-mailbox-v1";
    public const int MaxMessages = 200;
    public const int MaxRewardsPerMessage = 16;

    private readonly CourierMailboxOptions o = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<CourierMailboxSnapshot> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s => s with { CourierMailbox = s.CourierMailbox ?? NewState() }, ct);
        return Snapshot(state);
    }

    public async Task<CourierMailboxSnapshot> AppendAsync(Guid playerId, Guid hiveId, AppendCourierMessageRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (request is null || !ValidMessageId(request.Id) || string.IsNullOrWhiteSpace(request.Category) || request.Category.Length > 32
            || string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 256
            || (request.Preview?.Length ?? 0) > 512 || (request.Body?.Length ?? 0) > 4096)
            throw new ArgumentException("Invalid courier message.");

        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s =>
        {
            CourierMailboxState mailbox = s.CourierMailbox ?? NewState();
            if (mailbox.Messages.Any(m => string.Equals(m.Id, request.Id, StringComparison.Ordinal))) return s with { CourierMailbox = mailbox };

            List<CourierRewardRecord> rewards = (request.Rewards ?? new List<CourierRewardRecord>())
                .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.ItemId) && r.ItemId.Length <= 64 && r.Amount >= 0)
                .Take(MaxRewardsPerMessage)
                .Select(r => r with { })
                .ToList();
            CourierMessageRecord message = new(request.Id, request.Category, request.Title, request.Preview ?? string.Empty, request.Body ?? string.Empty, Utc(), false, false, rewards);
            List<CourierMessageRecord> messages = new(mailbox.Messages.Count + 1) { message };
            messages.AddRange(mailbox.Messages);
            if (messages.Count > MaxMessages) messages = messages.Take(MaxMessages).ToList();
            return s with { CourierMailbox = mailbox with { Messages = messages } };
        }, ct);
        return Snapshot(state);
    }

    public Task<CourierMailboxSnapshot> SetReadAsync(Guid playerId, Guid hiveId, string messageId, bool read, CancellationToken ct = default)
        => MutateMessage(playerId, hiveId, messageId, m => m with { Read = read }, ct);

    public Task<CourierMailboxSnapshot> SetFavoriteAsync(Guid playerId, Guid hiveId, string messageId, bool favorite, CancellationToken ct = default)
        => MutateMessage(playerId, hiveId, messageId, m => m with { Favorite = favorite }, ct);

    public Task<CourierMailboxSnapshot> CollectRewardsAsync(Guid playerId, Guid hiveId, string messageId, CancellationToken ct = default)
        => MutateMessage(playerId, hiveId, messageId, m => m with { Rewards = m.Rewards.Select(r => r with { Collected = true }).ToList() }, ct);

    public async Task<CourierMailboxSnapshot> MarkAllReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s =>
        {
            CourierMailboxState mailbox = s.CourierMailbox ?? NewState();
            List<CourierMessageRecord> messages = mailbox.Messages.Select(m => m.Read ? m : m with { Read = true }).ToList();
            return s with { CourierMailbox = mailbox with { Messages = messages } };
        }, ct);
        return Snapshot(state);
    }

    public async Task<CourierMailboxSnapshot> DeleteReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s =>
        {
            CourierMailboxState mailbox = s.CourierMailbox ?? NewState();
            List<CourierMessageRecord> messages = mailbox.Messages.Where(m => !(m.Read && !m.Favorite)).ToList();
            return s with { CourierMailbox = mailbox with { Messages = messages } };
        }, ct);
        return Snapshot(state);
    }

    private async Task<CourierMailboxSnapshot> MutateMessage(Guid playerId, Guid hiveId, string messageId, Func<CourierMessageRecord, CourierMessageRecord> mutate, CancellationToken ct)
    {
        Ensure();
        if (!ValidMessageId(messageId)) throw new ArgumentException("Invalid message id.");
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s =>
        {
            CourierMailboxState mailbox = s.CourierMailbox ?? NewState();
            int index = mailbox.Messages.FindIndex(m => string.Equals(m.Id, messageId, StringComparison.Ordinal));
            if (index < 0) return s with { CourierMailbox = mailbox };
            List<CourierMessageRecord> messages = new(mailbox.Messages);
            messages[index] = mutate(messages[index]);
            return s with { CourierMailbox = mailbox with { Messages = messages } };
        }, ct);
        return Snapshot(state);
    }

    private void Ensure()
    {
        if (!o.Enabled) throw new InvalidOperationException("Courier mailbox is disabled");
    }

    private DateTimeOffset Utc()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("Server clock must be UTC");
        return now;
    }

    private static CourierMailboxState NewState() => new(new List<CourierMessageRecord>());
    private static bool ValidMessageId(string? id) => !string.IsNullOrWhiteSpace(id) && id.Trim() == id && id.Length <= 128;

    private static CourierMailboxSnapshot Snapshot(PlayerHiveState state)
    {
        CourierMailboxState mailbox = state.CourierMailbox ?? NewState();
        return new(state.PlayerId, state.HiveId, ContractVersion, mailbox.Messages);
    }
}

public sealed record CourierRewardRecord(string ItemId, int Amount, bool Collected);
public sealed record CourierMessageRecord(string Id, string Category, string Title, string Preview, string Body, DateTimeOffset CreatedAtUtc, bool Read, bool Favorite, List<CourierRewardRecord> Rewards);
public sealed record CourierMailboxState(List<CourierMessageRecord> Messages);
public sealed record CourierMailboxSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, IReadOnlyList<CourierMessageRecord> Messages);
public sealed record AppendCourierMessageRequest(string Id, string Category, string Title, string? Preview, string? Body, List<CourierRewardRecord>? Rewards);
public sealed record SetCourierFlagRequest(bool Value);
