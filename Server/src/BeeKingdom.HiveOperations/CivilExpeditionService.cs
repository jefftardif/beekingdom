namespace BeeKingdom.HiveOperations;

/// <summary>
/// Alpha civilian flight into one public floral site and return to hive.
/// All operations run against a single ACID hive+site+contribution unit of work.
/// This service never uses military reservations or local Unity flowers.
/// </summary>
public sealed class CivilExpeditionService(
    IHiveStateRepository hiveRepository,
    ICivilExpeditionUnitOfWork unitOfWork,
    IServerClock clock,
    CivilExpeditionOptions options,
    FloralSiteOptions floralOptions)
{
    private readonly CivilExpeditionOptions rules =
        options ?? throw new ArgumentNullException(nameof(options));
    private readonly FloralSiteOptions catalogue =
        floralOptions ?? throw new ArgumentNullException(nameof(floralOptions));

    public async Task<CivilExpeditionSnapshot> ReadAsync(
        Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        EnsureEnabled();
        EnsureIdentity(playerId, hiveId);
        PlayerHiveState hive = await hiveRepository.ReadAsync(playerId, hiveId, ct)
            ?? throw new KeyNotFoundException("game.not_found");
        CivilExpeditionMission? active = hive.CivilExpeditions?.Active;
        DateTimeOffset now = Now();
        if (active is null || now < active.ReturnsAtUtc)
            return CivilExpeditionRules.Snapshot(hive, now);

        FloralSiteDefinition site = RequireSite(active.SiteId);
        CivilExpeditionMutation result = await unitOfWork.ExecuteAsync(
            playerId, hiveId, site, catalogue.CatalogVersion,
            (state, flora) => Settle(state, flora, now), ct);
        return CivilExpeditionRules.Snapshot(result.Hive, Now());
    }

    public async Task<CivilExpeditionResult> StartAsync(
        Guid playerId, Guid hiveId, StartCivilExpeditionRequest request,
        CancellationToken ct = default)
    {
        EnsureEnabled();
        EnsureIdentity(playerId, hiveId);
        if (request is null || request.Workers <= 0
            || request.Workers > rules.MaxWorkers
            || request.ExpectedHiveRevision < 0 || request.ExpectedSiteRevision < 0
            || request.ExpectedHiveRevision == long.MaxValue
            || !CivilExpeditionRules.ValidKey(request.IdempotencyKey)
            || !string.Equals(request.Mode, "balanced", StringComparison.Ordinal))
            throw new ArgumentException("game.invalid_request");

        FloralSiteDefinition definition = RequireSite(request.SiteId);
        // No phantom hive from a crafted URL. The SQL UoW repeats this check
        // under lock, closing the delete/start race.
        if (await hiveRepository.ReadAsync(playerId, hiveId, ct) is null)
            throw new KeyNotFoundException("game.not_found");
        CivilExpeditionResult? result = null;
        CivilExpeditionMutation committed = await unitOfWork.ExecuteAsync(playerId, hiveId, definition,
            catalogue.CatalogVersion, (original, site) =>
        {
            DateTimeOffset now = Now();
            PlayerHiveState hive = CivilPopulationService
                .SettleMatureCohort(original, now);
            var exp = hive.CivilExpeditions ?? CivilExpeditionRules.Empty();
            string payload = CivilExpeditionRules.Hash(
                "start|"+definition.SiteId+"|"+request.Workers+"|"
                +request.Mode+"|"+request.ExpectedHiveRevision+"|"
                +request.ExpectedSiteRevision);
            if (exp.Receipts.TryGetValue(request.IdempotencyKey,
                out CivilExpeditionReceipt? old))
            {
                result = old.PayloadHash == payload
                    ? new(true, old.Code, CivilExpeditionRules.Snapshot(hive, now), old)
                    : Deny(hive, now, "game.idempotency_conflict");
                return new(hive, site);
            }
            if (hive.Revision != request.ExpectedHiveRevision
                || site.Revision != request.ExpectedSiteRevision)
            {
                result = Deny(hive, now, "game.revision_conflict");
                return new(hive, site);
            }
            if (exp.Active is not null)
            {
                result = Deny(hive, now, "game.civil_expedition_busy");
                return new(hive, site);
            }
            if (site.PollenRemaining <= 0)
            {
                result = Deny(hive, now, "game.floral_site_depleted");
                return new(hive, site);
            }
            CivilPopulationState? civil = hive.CivilPopulation;
            if (civil is null || civil.AvailableWorkers < request.Workers)
            {
                result = Deny(hive, now, "game.civil_workers_insufficient");
                return new(hive, site);
            }
            if (exp.Receipts.Count >= CivilExpeditionRules.MaxReceipts - 1)
            {
                result = Deny(hive, now, "game.civil_receipt_limit");
                return new(hive, site);
            }

            long minutes = rules.DurationMinutes;
            CivilExpeditionMission flight = new(
                Guid.NewGuid(), definition.WorldId, definition.RegionId,
                definition.SiteId, request.Workers, "balanced", now,
                now.AddMinutes(minutes / 3),
                now.AddMinutes(2 * minutes / 3),
                now.AddMinutes(minutes), rules.PollenPerWorker,
                rules.FertilityGainBp, rules.BiodiversityGainBp);
            long before = hive.Revision;
            long after = checked(before + 1);
            var receipt = new CivilExpeditionReceipt("start",
                request.IdempotencyKey, payload, flight.MissionId,
                before, after, site.Revision, site.Revision, now,
                "game.civil_expedition_started");
            var receipts = new Dictionary<string, CivilExpeditionReceipt>(
                exp.Receipts, StringComparer.Ordinal)
            { [request.IdempotencyKey] = receipt };
            PlayerHiveState next = hive with
            {
                Revision = after,
                CivilPopulation = civil with
                {
                    Revision = checked(civil.Revision + 1),
                    AvailableWorkers = civil.AvailableWorkers - request.Workers,
                    AssignedWorkers = checked(civil.AssignedWorkers + request.Workers)
                },
                CivilExpeditions = exp with
                {
                    Revision = checked(exp.Revision + 1),
                    Active = flight,
                    Receipts = receipts
                }
            };
            result = new(true, receipt.Code, CivilExpeditionRules.Snapshot(next, now), receipt);
            return new(next, site);
        }, ct);
        return result is { } outcome
            ? outcome with { Snapshot = CivilExpeditionRules.Snapshot(committed.Hive, Now()) }
            : throw new InvalidOperationException("Missing expedition result.");
    }

    /// <summary>
    /// Finish on an authenticated GET after server deadline. A future due-work
    /// runner can call the SAME method without a separate settlement path.
    /// </summary>
    internal static CivilExpeditionMutation Settle(
        PlayerHiveState hive, FloralSiteState site, DateTimeOffset now)
    {
        CivilExpeditionMission? flight = hive.CivilExpeditions?.Active;
        if (flight is null || now < flight.ReturnsAtUtc)
            return new(hive, site);
        if (flight.WorldId != site.WorldId || flight.SiteId != site.SiteId)
            throw new InvalidDataException("Expedition/site mismatch.");

        CivilPopulationState civil = hive.CivilPopulation
            ?? throw new InvalidDataException("Missing reserved civilian roster.");
        if (civil.AssignedWorkers < flight.Workers)
            throw new InvalidDataException("Civil expedition worker reservation lost.");
        CivilExpeditionState exp = hive.CivilExpeditions!;
        string completionKey = "finish:" + flight.MissionId.ToString("N");
        if (exp.Receipts.ContainsKey(completionKey))
            throw new InvalidDataException("Active mission was already settled.");

        ResourceBalance pollen = hive.Resources.GetValueOrDefault("pollen")
            ?? throw new InvalidDataException("Missing pollen resource balance.");
        if (pollen.Amount < 0 || pollen.Capacity < pollen.Amount)
            throw new InvalidDataException("Corrupt pollen resource balance.");

        long collectable = Math.Min(checked(flight.Workers * flight.PollenPerWorker),
            Math.Min(site.PollenRemaining, pollen.Capacity - pollen.Amount));
        long afterHiveRevision = checked(hive.Revision + 1);
        long afterSiteRevision = collectable == 0
            ? site.Revision : checked(site.Revision + 1);
        Dictionary<string, ResourceBalance> resources =
            new(hive.Resources, StringComparer.Ordinal)
            {
                ["pollen"] = pollen with { Amount = checked(pollen.Amount + collectable) }
            };

        FloralSiteState nextSite = site;
        CivilExpeditionContribution? contribution = null;
        if (collectable > 0)
        {
            Dictionary<string, long> byPlayer =
                new(site.ContributionPointsByPlayer ?? new(), StringComparer.Ordinal);
            string playerKey = hive.PlayerId.ToString("N");
            byPlayer[playerKey] = checked(byPlayer.GetValueOrDefault(playerKey) + collectable);
            nextSite = site with
            {
                Revision = afterSiteRevision,
                PollenRemaining = site.PollenRemaining - collectable,
                FertilityBp = Math.Min(10000, site.FertilityBp + flight.FertilityGainBp),
                BiodiversityBp = Math.Min(10000,
                    site.BiodiversityBp + flight.BiodiversityGainBp),
                TotalPollinationPoints = checked(site.TotalPollinationPoints + collectable),
                ContributionPointsByPlayer = byPlayer,
                LastSettledAtUtc = now
            };
            contribution = new(flight.MissionId, hive.PlayerId, hive.HiveId,
                flight.WorldId, flight.SiteId, collectable, collectable, collectable, now);
        }

        CivilExpeditionCompletion completion = new(
            flight.MissionId, flight.SiteId, collectable, collectable,
            now, afterSiteRevision, resources["pollen"].Amount);
        var receipt = new CivilExpeditionReceipt(
            "finish", completionKey, CivilExpeditionRules.Hash("finish|"+flight.MissionId),
            flight.MissionId, hive.Revision, afterHiveRevision,
            site.Revision, afterSiteRevision, now, "game.civil_expedition_completed",
            collectable);
        var receipts = new Dictionary<string, CivilExpeditionReceipt>(
            exp.Receipts, StringComparer.Ordinal) { [completionKey] = receipt };
        PlayerHiveState nextHive = hive with
        {
            Revision = afterHiveRevision,
            Resources = resources,
            CivilPopulation = civil with
            {
                Revision = checked(civil.Revision + 1),
                AvailableWorkers = checked(civil.AvailableWorkers + flight.Workers),
                AssignedWorkers = civil.AssignedWorkers - flight.Workers
            },
            CivilExpeditions = exp with
            {
                Revision = checked(exp.Revision + 1),
                Active = null,
                LastCompletion = completion,
                Receipts = receipts
            }
        };
        return new(nextHive, nextSite, contribution);
    }

    private CivilExpeditionResult Deny(PlayerHiveState hive, DateTimeOffset now, string code) =>
        new(false, code, CivilExpeditionRules.Snapshot(hive, now));

    private FloralSiteDefinition RequireSite(string? siteId)
    {
        if (!FloralSiteOptions.ValidId(siteId))
            throw new ArgumentException("game.invalid_request");
        return catalogue.Sites.SingleOrDefault(s => s.SiteId == siteId)
            ?? throw new KeyNotFoundException("game.floral_site_not_found");
    }

    private void EnsureEnabled()
    {
        rules.Validate();
        catalogue.Validate();
        if (!rules.Enabled || !catalogue.Enabled)
            throw new InvalidOperationException("game.civil_expeditions_disabled");
    }

    private static void EnsureIdentity(Guid playerId, Guid hiveId)
    {
        if (playerId == Guid.Empty || hiveId == Guid.Empty)
            throw new ArgumentException("game.invalid_request");
    }
    private DateTimeOffset Now() => clock.UtcNow.ToUniversalTime();
}
