using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record ChooseStrategicPathCommand(Guid PlayerId, Guid HiveId, string PathId, long ExpectedRevision, string IdempotencyKey);
public sealed record StrategicPathCommandResult(bool Succeeded, string Code, StrategicPathSnapshot Snapshot);

// Premier effet reel du chemin strategique (auparavant un choix permanent sans aucune
// consequence de jeu). Valeurs volontairement modestes et non definitivement equilibrees
// (voir DB-16/ARCHITECTURE_METHODOLOGY §10 - l'equilibrage fin viendra avec l'arbre de
// competences officiel, cf. Docs/Architecture/PlayerSkillTree_Progression_Spec.md) : chaque
// classe recoit UN bonus reel, reutilisant les mecanismes bp additifs deja construits cette
// session (puissance de combat par famille, taux de production, capacite) plutot que
// d'inventer un nouveau systeme par classe.
public static class StrategicPathBonusCatalog
{
    private static readonly string[] CombatFamilies = ["guardians", "wingrunners", "darters"];
    public const long CombatPowerBonusBp = 600;      // royal_guard, striker (garde/assaut)
    public const long ProductionRateBonusBp = 600;   // nurturer, alchemist (soutien/transformation)
    public const long CapacityBonusBp = 600;         // scout (logistique)

    public static IReadOnlyDictionary<string, long> CombatPowerBonusBpByFamily(string? selectedPath)
    {
        long bonus = selectedPath is "royal_guard" or "striker" ? CombatPowerBonusBp : 0;
        return CombatFamilies.ToDictionary(f => f, _ => bonus, StringComparer.Ordinal);
    }

    public static long ProductionRateBonusBpFor(string? selectedPath) =>
        selectedPath is "nurturer" or "alchemist" ? ProductionRateBonusBp : 0;

    public static long CapacityBonusBpFor(string? selectedPath) =>
        selectedPath == "scout" ? CapacityBonusBp : 0;
}

public sealed class StrategicPathService(IHiveStateRepository repository, IServerClock clock)
{
    public static readonly IReadOnlyList<string> CanonicalPaths = ["royal_guard", "striker", "nurturer", "scout", "alchemist"];
    public const string CatalogVersion = "phase4-v1";
    public async Task<StrategicPathSnapshot> ReadSnapshotAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    { PlayerHiveState state = (await repository.ReadAsync(playerId, hiveId, ct)) ?? throw new KeyNotFoundException(); return Snapshot(state); }
    public Task<StrategicPathCommandResult> ChooseAsync(ChooseStrategicPathCommand command, CancellationToken ct = default)
    {
        string path = command.PathId?.Trim() ?? string.Empty; string key = command.IdempotencyKey ?? string.Empty; string hash = Hash($"strategic|{command.PlayerId}|{command.HiveId}|{path}"); StrategicPathCommandResult? result = null;
        return Execute();
        async Task<StrategicPathCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = HiveStateMigrator.ToCurrent(state); StrategicPathState strategic = state.StrategicPath ?? new(CatalogVersion, null, 0, clock.UtcNow, new());
                if (strategic.Receipts.TryGetValue(key, out IdempotencyReceipt? receipt)) { result = receipt.PayloadHash == hash ? new(receipt.Succeeded, receipt.Code, Snapshot(state)) : new(false, "idempotency_conflict", Snapshot(state)); return state; }
                if (!CanonicalPaths.Contains(path) || string.IsNullOrWhiteSpace(key) || key.Length > 256) return Record(state, strategic, key, hash, false, "game.invalid_request", out result);
                if (state.BuildingLevels.Values.DefaultIfEmpty().Max() < 10) return Record(state, strategic, key, hash, false, "game.strategic_path_ineligible", out result);
                // Comparer uniquement a strategic.Revision (meme convention que CombatPatrolService
                // pour un sous-systeme a compteur imbrique) : state.Revision global avance des que
                // n'importe quel autre systeme ecrit (ex. lecture de production hors ligne), donc le
                // comparer ici rendrait ce choix definitivement inatteignable des qu'un joueur reel a
                // fait quoi que ce soit d'autre avant d'atteindre le niveau 10.
                if (strategic.Revision != command.ExpectedRevision) return Record(state, strategic, key, hash, false, "game.revision_conflict", out result);
                if (strategic.SelectedPath is not null) return Record(state, strategic, key, hash, false, "game.strategic_path_locked", out result);
                DateTimeOffset now = clock.UtcNow; StrategicPathState updatedStrategic = strategic with { SelectedPath = path, Revision = strategic.Revision + 1, UpdatedAtUtc = now };
                return Record(state with { Revision = state.Revision + 1, StrategicPath = updatedStrategic }, updatedStrategic, key, hash, true, "game.strategic_path_selected", out result);
            }, ct); return result!;
        }
    }
    PlayerHiveState Record(PlayerHiveState state, StrategicPathState strategic, string key, string hash, bool ok, string code, out StrategicPathCommandResult? result)
    { DateTimeOffset now = clock.UtcNow; var receipts = new Dictionary<string, IdempotencyReceipt>(strategic.Receipts) { [key] = new(hash, ok, code, null, now, state.Revision, state.Revision, null, strategic.SelectedPath, null, now) }; StrategicPathState updated = strategic with { Receipts = receipts }; PlayerHiveState recorded = state with { StrategicPath = updated }; result = new(ok, code, Snapshot(recorded)); return recorded; }
    StrategicPathSnapshot Snapshot(PlayerHiveState state) { StrategicPathState strategic = state.StrategicPath ?? new(CatalogVersion, null, 0, DateTimeOffset.UnixEpoch, new()); return new(state.PlayerId, state.HiveId, strategic.CatalogVersion, CanonicalPaths, strategic.SelectedPath, strategic.Revision, strategic.UpdatedAtUtc); }
    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
