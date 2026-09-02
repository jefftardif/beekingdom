using BeeKingdom.Chat.Audience;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts;

// M043Q-CL: the real, server-authoritative implementation of BeeKingdom.Chat's
// IChatSenderDisplayNameResolver seam - wraps IPlayerDirectoryService.GetByPlayerId directly, the
// exact same authoritative lookup (real onboarded AuthenticationAccounts.DisplayName first, Account
// record fallback) M043P already wired Alliance member/journal names to. Before this, every chat
// message's SenderDisplayNameSnapshot was hardcoded to "player:{id}" regardless of who sent it.
public sealed class PlayerDirectoryChatSenderDisplayNameResolver : IChatSenderDisplayNameResolver
{
    private readonly IPlayerDirectoryService directory;

    public PlayerDirectoryChatSenderDisplayNameResolver(IPlayerDirectoryService directory)
    {
        this.directory = directory;
    }

    public string? ResolveDisplayName(Guid playerId) => directory.GetByPlayerId(new PlayerId(playerId))?.DisplayName;
}
