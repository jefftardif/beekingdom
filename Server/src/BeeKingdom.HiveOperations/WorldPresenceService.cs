namespace BeeKingdom.HiveOperations;

public sealed record WorldPresenceSighting(Guid HiveId, string ColonyLabel, string NodeId, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc);
public sealed record WorldPresenceSnapshot(DateTimeOffset ServerTimeUtc, IReadOnlyList<WorldPresenceSighting> Sightings);

// Monde vivant (demande de Jeff, 2026-08-01) : uniquement de la presence ambiante - aucune
// interaction, aucun combat, jamais de mutation d'un etat qui ne nous appartient pas. Reutilise
// directement l'architecture d'escouades persistantes deja construite (WorldResourceCollectionState.Active,
// avec ses troupes reellement engagees) : l'occupation d'un noeud de ressource par un autre joueur
// EST deja un deploiement reel qui existe sur le terrain - ce service se contente de le rendre
// visible aux autres joueurs, sans y ajouter de mecanique.
//
// L'echantillon (hives recemment modifiees, tous joueurs confondus) est un compromis honnete :
// aucun registre/index dedie n'existe pour "qui est actif en ce moment", et en creer un serait un
// nouveau gros systeme. Se limiter aux 3 noeuds officiels (identifiants stables et connus de tous
// les clients, voir WorldResourceCollectionOptions) garantit que chaque sighting peut toujours
// etre resolu en position sur la carte, meme chez un joueur qui n'a jamais explore ce chunk.
public sealed class WorldPresenceService(IHiveStateRepository repository, IServerClock clock)
{
    public const string ContractVersion = "living-world-presence-v1";
    private const int SampleSize = 60;
    private const int MaxSightings = 20;

    public async Task<WorldPresenceSnapshot> ReadAsync(Guid excludeHiveId, CancellationToken ct = default)
    {
        DateTimeOffset now = clock.UtcNow;
        IReadOnlyList<PlayerHiveState> sample = await repository.ListRecentlyActiveAsync(SampleSize, ct);
        var sightings = new List<WorldPresenceSighting>();
        foreach (PlayerHiveState state in sample)
        {
            if (state.HiveId == excludeHiveId) continue;
            WorldResourceActiveFlight? active = state.WorldResourceCollection?.Active;
            if (active is null || active.EndsAtUtc <= now) continue;
            sightings.Add(new WorldPresenceSighting(state.HiveId, ColonyLabel(state.HiveId), active.NodeId, active.StartedAtUtc, active.EndsAtUtc));
            if (sightings.Count >= MaxSightings) break;
        }
        return new WorldPresenceSnapshot(now, sightings);
    }

    // Aucun nom de compte/joueur reel n'est expose (presence ambiante seulement, pas d'identite) -
    // une etiquette courte, stable et deterministe derivee du hiveId suffit a donner
    // l'impression d'une colonie distincte sans coupler ce service au systeme de comptes.
    private static string ColonyLabel(Guid hiveId) => "Colonie #" + hiveId.ToString("N")[..4].ToUpperInvariant();
}
