using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Research;

// Implements HiveOperations' own small port (IAllianceGameplayBonusResolver) so
// HiveOfflineProductionService/CombatPatrolService can resolve a real Alliance Research bonus
// without HiveOperations ever depending on Alliance - see AllianceGameplayBonus.cs's own comment.
public sealed class AllianceGameplayBonusResolverAdapter : IAllianceGameplayBonusResolver
{
    private readonly AllianceResearchBonusResolver inner;

    public AllianceGameplayBonusResolverAdapter(AllianceResearchBonusResolver inner)
    {
        this.inner = inner;
    }

    public async Task<AllianceGameplayBonus> ResolveAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        AllianceResearchBonus bonus = await inner.ResolveForPlayerAsync(new PlayerId(playerId), cancellationToken);
        return new AllianceGameplayBonus(bonus.ProductionBp, bonus.CapacityBp, bonus.CombatPowerBp);
    }
}
