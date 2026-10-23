using System.Text.Json;
using BeeKingdom.HiveOperations;
using Xunit;
namespace BeeKingdom.HiveOperations.Tests;
public sealed class PatrolContractPersistenceTests
{
    [Fact]
    public void OlderHiveKeepsPatrolWoundedBankAndNurseryTogether()
    {
        var reserved = new Dictionary<string,long> { ["guardians"]=10, ["wingrunners"]=0, ["darters"]=0 };
        var troops = new Dictionary<string,long> { ["guardians"]=8, ["wingrunners"]=0, ["darters"]=0 };
        var state = new PlayerHiveState(Guid.NewGuid(),Guid.NewGuid(),12,3,
            new Dictionary<string,ResourceBalance>(),new Dictionary<string,int>{{"guard_post",6}},
            new List<HiveOperation>(),new Dictionary<string,IdempotencyReceipt>(),
            DoctrineRoster:new DoctrineRosterState(1,troops,null,new()),
            SquadReservation:new SquadReservationState(1,1000,reserved,"prepared",new(),new(){"zephyra"}),
            CombatPatrol:new CombatPatrolState(1,new(),new(),new(),
                Recovering:new(){new("guardians",2,DateTimeOffset.UtcNow.AddMinutes(5))}),
            RoyalReserve:new RoyalReserveState(1,new(){{"honey",111},{"pollen",13},{"wax",7}},new()),
            NurseryCohorts:new NurseryCohortState(1,3,null,new()));
        var copy=JsonSerializer.Deserialize<PlayerHiveState>(JsonSerializer.Serialize(state))!;
        var migrated=HiveStateMigrator.ToCurrent(copy);
        Assert.Equal(13,migrated.ModelVersion);
        Assert.Equal("prepared",migrated.SquadReservation!.ReservationId);
        Assert.Equal(new[]{"zephyra"},migrated.SquadReservation.ChampionBeeIds);
        Assert.Equal(2,migrated.CombatPatrol!.Recovering!.Single().Count);
        Assert.Equal(111,migrated.RoyalReserve!.Amounts["honey"]);
        Assert.Equal(3,migrated.NurseryCohorts!.RecruitsInReserve);
        var invalid=state with { SquadReservation=state.SquadReservation! with
            { Reserved=new(){{"guardians",11},{"wingrunners",0},{"darters",0}} } };
        Assert.Throws<InvalidDataException>(()=>HiveStateMigrator.ToCurrent(invalid));
    }
}