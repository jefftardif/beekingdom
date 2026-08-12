namespace BeeKingdom.HiveOperations;
public enum HiveDailyRoundFact { CollectionReceived, OperationLaunched, SnapshotRead }
public static class HiveDailyRoundFacts
{
 public static PlayerHiveState ApplyFreshFact(PlayerHiveState state, DateTimeOffset now, HiveDailyRoundFact fact, bool incrementRevisionWhenStandalone)
 {
  if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("UTC required");
  var day=new DateTimeOffset(now.UtcDateTime.Date,TimeSpan.Zero); var current=state.DailyRound;
  var round=current is { } r && r.DayUtc==day ? r : new(day,false,false,false,null);
  bool already=fact switch { HiveDailyRoundFact.CollectionReceived=>round.CollectionReceived,HiveDailyRoundFact.OperationLaunched=>round.OperationLaunched,_=>round.SnapshotRead };
  if(already) return state;
  round=fact switch { HiveDailyRoundFact.CollectionReceived=>round with{CollectionReceived=true},HiveDailyRoundFact.OperationLaunched=>round with{OperationLaunched=true},_=>round with{SnapshotRead=true} };
  return state with { DailyRound=round, Revision=incrementRevisionWhenStandalone?state.Revision+1:state.Revision };
 }
}
