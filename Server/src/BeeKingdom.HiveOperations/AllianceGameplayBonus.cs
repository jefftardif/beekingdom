namespace BeeKingdom.HiveOperations;

// M051-CL: BeeKingdom.HiveOperations must never depend on BeeKingdom.Alliance (Alliance already
// depends on HiveOperations - PlayerHiveState, IHiveStateRepository, IServerClock - so the reverse
// would be circular). This tiny port lives here instead: HiveOperations only knows "some resolver
// can hand me a player's currently-active bonus bps", never that it comes from an Alliance's
// completed research. BeeKingdom.Alliance.Research implements the adapter and is the only thing
// that ever knows both sides exist.
public readonly record struct AllianceGameplayBonus(long ProductionBp, long CapacityBp, long CombatPowerBp)
{
    public static readonly AllianceGameplayBonus None = new(0, 0, 0);
}

public interface IAllianceGameplayBonusResolver
{
    Task<AllianceGameplayBonus> ResolveAsync(Guid playerId, CancellationToken cancellationToken = default);
}
