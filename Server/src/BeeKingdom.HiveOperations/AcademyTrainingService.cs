using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

// This is a playable academic curriculum, not the Research Center or Barracks.
// Completing a course awards a persistent academic certificate (and a course
// credit) used solely for later course prerequisites. It does NOT currently
// change combat, champion levels, research effects, or resource production.
public sealed record AcademyCourseDefinition(
    string CourseId, int RequiredAcademyLevel, TimeSpan Duration,
    IReadOnlyDictionary<string, long> Costs, IReadOnlyList<string> Prerequisites,
    int Credits);

public sealed record AcademyCourseCompletion(
    string CourseId, DateTimeOffset CompletedAtUtc, int Credits);

public sealed record AcademyTrainingOperation(
    Guid OperationId, string CourseId, DateTimeOffset StartedAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record AcademyTrainingStoredReceipt(
    string PayloadHash, string CourseId, string Action, Guid OperationId,
    long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc,
    string Code);

public sealed record AcademyTrainingState(
    long Revision, List<AcademyCourseCompletion> Completed,
    AcademyTrainingOperation? ActiveOperation,
    Dictionary<string, AcademyTrainingStoredReceipt> Receipts);

public sealed record AcademyCourseOffer(
    string CourseId, int RequiredAcademyLevel, TimeSpan Duration,
    IReadOnlyDictionary<string, long> Costs, IReadOnlyList<string> Prerequisites,
    int Credits, string State, bool CanStart);

public sealed record AcademyTrainingActive(
    Guid OperationId, string CourseId, DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletesAtUtc, string Status);

public sealed record AcademyTrainingSnapshot(
    Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion,
    long Revision, long AcademyRevision, DateTimeOffset ServerTimeUtc,
    int AcademyBuildingLevel, IReadOnlyDictionary<string, ResourceBalance> Balances,
    IReadOnlyList<AcademyCourseOffer> Courses,
    IReadOnlyList<AcademyCourseCompletion> Completed,
    AcademyTrainingActive? ActiveOperation, int EarnedCredits);

public sealed record AcademyTrainingMutationRequest(
    long ExpectedRevision, string IdempotencyKey);

public sealed record AcademyTrainingCommandReceipt(
    string IdempotencyKey, string CourseId, Guid OperationId, string Code,
    long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc);

public sealed record AcademyTrainingCommandResult(
    bool Succeeded, string Code, AcademyTrainingSnapshot Snapshot,
    AcademyTrainingCommandReceipt? Receipt = null);

// Default OFF in production. This keeps newly deployed server binaries
// backwards compatible with older Unity builds until release verification.
public sealed class AcademyTrainingOptions
{
    public const string SectionName = "AcademyTraining";
    public bool Enabled { get; set; }
}

public sealed class AcademyTrainingService
{
    public const string ContractVersion = "bk-academy-training-v1";
    public const string CatalogVersion = "academy-curriculum-2026-10-v1";
    public const string AcademyBuildingKey = "academy_canopy";
    public const int MaxReceipts = 128;
    public const int MaxCredits = 4;

    public static readonly IReadOnlyList<AcademyCourseDefinition> Curriculum =
    [
        new("tactics", 1, TimeSpan.FromMinutes(3),
            new Dictionary<string, long>(StringComparer.Ordinal)
            { ["honey"] = 240, ["pollen"] = 75 }, [], 1),
        new("logistics", 2, TimeSpan.FromMinutes(5),
            new Dictionary<string, long>(StringComparer.Ordinal)
            { ["honey"] = 410, ["wax"] = 90 }, ["tactics"], 1),
        new("exploration", 3, TimeSpan.FromMinutes(7),
            new Dictionary<string, long>(StringComparer.Ordinal)
            { ["honey"] = 540, ["pollen"] = 160 }, ["tactics"], 1),
        new("mentorship", 4, TimeSpan.FromMinutes(9),
            new Dictionary<string, long>(StringComparer.Ordinal)
            { ["honey"] = 750, ["wax"] = 220, ["pollen"] = 180 },
            ["logistics", "exploration"], 1)
    ];

    private readonly IHiveStateRepository repository;
    private readonly IServerClock clock;
    private readonly AcademyTrainingOptions options;

    public AcademyTrainingService(IHiveStateRepository repository,
        IServerClock clock, AcademyTrainingOptions options)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private static AcademyTrainingState Empty() =>
        new(0, [], null,
            new Dictionary<string, AcademyTrainingStoredReceipt>(StringComparer.Ordinal));

    private DateTimeOffset Now() => clock.UtcNow.ToUniversalTime();

    private void RequireEnabled()
    {
        if (!options.Enabled)
            throw new InvalidOperationException("game.academy_disabled");
    }

    private static bool ValidIdempotencyKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length is >= 8 and <= 128 &&
        key.Trim() == key &&
        key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static bool ValidateIdentity(Guid player, Guid hive) =>
        player != Guid.Empty && hive != Guid.Empty;

    private static string Hash(string payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

    private static AcademyCourseDefinition? Find(string courseId) =>
        Curriculum.FirstOrDefault(x =>
            string.Equals(x.CourseId, courseId, StringComparison.Ordinal));

    private static bool Completed(AcademyTrainingState data, string courseId) =>
        data.Completed.Any(x => string.Equals(x.CourseId, courseId, StringComparison.Ordinal));

    private static void EnsureScope(PlayerHiveState state, Guid player, Guid hive)
    {
        if (state.PlayerId != player || state.HiveId != hive)
            throw new InvalidDataException("game.academy_identity_mismatch");
    }

    public async Task<AcademyTrainingSnapshot> ReadAsync(Guid player, Guid hive,
        CancellationToken ct = default)
    {
        RequireEnabled();
        if (!ValidateIdentity(player,hive)) throw new ArgumentException("game.invalid_request");
        PlayerHiveState? state = await repository.ReadAsync(player, hive, ct);
        if (state is null) throw new KeyNotFoundException("game.not_found");
        EnsureScope(state,player,hive);
        return Snapshot(state,Now());
    }

    public async Task<AcademyTrainingCommandResult> StartAsync(
        Guid player, Guid hive, string courseId,
        AcademyTrainingMutationRequest request, CancellationToken ct = default)
    {
        RequireEnabled();
        if (!ValidateIdentity(player,hive) || request is null ||
            request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue ||
            !ValidIdempotencyKey(request.IdempotencyKey) || Find(courseId) is null)
            throw new ArgumentException("game.invalid_request");
        if (await repository.ReadAsync(player,hive,ct) is null)
            throw new KeyNotFoundException("game.not_found");
        AcademyTrainingCommandResult? response = null;
        DateTimeOffset now = Now();
        await repository.ExecuteAtomicallyAsync(player,hive, original =>
        {
            EnsureScope(original,player,hive);
            AcademyTrainingState data = original.Academy ?? Empty();
            string hash = Hash("start|" + courseId + "|" +
                request.ExpectedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (data.Receipts.TryGetValue(request.IdempotencyKey, out var existing))
            {
                response = Replay(original,existing,hash,now,request.IdempotencyKey);
                return original;
            }
            if (original.Revision != request.ExpectedRevision)
            {
                response = Failure(original,now,"game.revision_conflict");
                return original;
            }
            AcademyCourseDefinition course = Find(courseId)!;
            if (data.ActiveOperation is not null)
            {
                response = Failure(original,now,"game.academy_busy");
                return original;
            }
            if (Completed(data,courseId))
            {
                response = Failure(original,now,"game.academy_already_completed");
                return original;
            }
            if (original.BuildingLevels.GetValueOrDefault(AcademyBuildingKey,1)
                    < course.RequiredAcademyLevel ||
                course.Prerequisites.Any(x => !Completed(data,x)))
            {
                response = Failure(original,now,"game.academy_prerequisite_missing");
                return original;
            }
            if (course.Costs.Any(pair => !original.Resources.TryGetValue(pair.Key,
                    out ResourceBalance? balance) || balance.Amount < pair.Value))
            {
                response = Failure(original,now,"game.insufficient_resources");
                return original;
            }
            var resources = new Dictionary<string, ResourceBalance>(original.Resources);
            foreach ((string key,long value) in course.Costs)
                resources[key] = resources[key] with { Amount = resources[key].Amount - value };
            Guid operationId = Guid.NewGuid();
            var operation = new AcademyTrainingOperation(
                operationId,courseId,now,now.Add(course.Duration));
            AcademyTrainingStoredReceipt receipt = new(hash,courseId,"start",operationId,
                original.Revision,original.Revision+1,now,"game.academy_started");
            var receipts = AddReceipt(data.Receipts,request.IdempotencyKey,receipt);
            AcademyTrainingState next = data with
            {
                Revision = data.Revision+1,
                ActiveOperation = operation,
                Receipts = receipts
            };
            PlayerHiveState changed = original with
            {
                Revision = original.Revision+1,
                Resources = resources,
                Academy = next
            };
            response = Success(changed,receipt,now,request.IdempotencyKey);
            return changed;
        },ct);
        return response ?? throw new InvalidOperationException("Academy mutation lost");
    }

    public async Task<AcademyTrainingCommandResult> CompleteAsync(
        Guid player, Guid hive, Guid operationId,
        AcademyTrainingMutationRequest request, CancellationToken ct = default)
    {
        RequireEnabled();
        if (!ValidateIdentity(player,hive) || operationId == Guid.Empty ||
            request is null || request.ExpectedRevision < 0 ||
            request.ExpectedRevision == long.MaxValue ||
            !ValidIdempotencyKey(request.IdempotencyKey))
            throw new ArgumentException("game.invalid_request");
        if (await repository.ReadAsync(player,hive,ct) is null)
            throw new KeyNotFoundException("game.not_found");
        AcademyTrainingCommandResult? response = null;
        DateTimeOffset now = Now();
        await repository.ExecuteAtomicallyAsync(player,hive, original =>
        {
            EnsureScope(original,player,hive);
            AcademyTrainingState data = original.Academy ?? Empty();
            string hash = Hash("complete|" + operationId.ToString("D") + "|" +
                request.ExpectedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (data.Receipts.TryGetValue(request.IdempotencyKey, out var existing))
            {
                response = Replay(original,existing,hash,now,request.IdempotencyKey);
                return original;
            }
            if (original.Revision != request.ExpectedRevision)
            {
                response = Failure(original,now,"game.revision_conflict");
                return original;
            }
            AcademyTrainingOperation? active = data.ActiveOperation;
            if (active is null || active.OperationId != operationId ||
                Find(active.CourseId) is not { } course)
            {
                response = Failure(original,now,"game.academy_operation_missing");
                return original;
            }
            if (now < active.EndsAtUtc)
            {
                response = Failure(original,now,"game.academy_not_ready");
                return original;
            }
            if (Completed(data,active.CourseId))
            {
                response = Failure(original,now,"game.academy_already_completed");
                return original;
            }
            var learned = new List<AcademyCourseCompletion>(data.Completed)
                { new(active.CourseId,now,course.Credits) };
            var receipt = new AcademyTrainingStoredReceipt(
                hash,active.CourseId,"complete",operationId,
                original.Revision,original.Revision+1,now,"game.academy_completed");
            AcademyTrainingState next = data with
            {
                Revision = data.Revision+1,
                Completed = learned,
                ActiveOperation = null,
                Receipts = AddReceipt(data.Receipts,request.IdempotencyKey,receipt)
            };
            PlayerHiveState changed = original with
            {
                Revision = original.Revision+1, Academy = next
            };
            response = Success(changed,receipt,now,request.IdempotencyKey);
            return changed;
        },ct);
        return response ?? throw new InvalidOperationException("Academy completion lost");
    }

    private static Dictionary<string,AcademyTrainingStoredReceipt> AddReceipt(
        Dictionary<string,AcademyTrainingStoredReceipt> source,
        string key,AcademyTrainingStoredReceipt newReceipt)
    {
        var result = new Dictionary<string,AcademyTrainingStoredReceipt>(source,
            StringComparer.Ordinal);
        if (result.Count >= MaxReceipts)
        {
            string oldest = result.OrderBy(x => x.Value.AcceptedAtUtc)
                .ThenBy(x => x.Key,StringComparer.Ordinal).First().Key;
            result.Remove(oldest);
        }
        result[key] = newReceipt;
        return result;
    }

    private AcademyTrainingCommandResult Replay(PlayerHiveState hive,
        AcademyTrainingStoredReceipt receipt,string hash,DateTimeOffset now,string key)
    {
        if (!string.Equals(receipt.PayloadHash,hash,StringComparison.Ordinal))
            return Failure(hive,now,"game.idempotency_conflict");
        return Success(hive,receipt,now,key);
    }

    private AcademyTrainingCommandResult Success(PlayerHiveState hive,
        AcademyTrainingStoredReceipt r,DateTimeOffset now,string key) =>
        new(true,r.Code,Snapshot(hive,now),
            new(key,r.CourseId,r.OperationId,r.Code,r.RevisionBefore,
                r.RevisionAfter,r.AcceptedAtUtc));

    private AcademyTrainingCommandResult Failure(
        PlayerHiveState hive,DateTimeOffset now,string code) =>
        new(false,code,Snapshot(hive,now));

    private AcademyTrainingSnapshot Snapshot(PlayerHiveState hive,DateTimeOffset now)
    {
        AcademyTrainingState data = hive.Academy ?? Empty();
        int level = hive.BuildingLevels.GetValueOrDefault(AcademyBuildingKey,1);
        var completedKeys = data.Completed.Select(x => x.CourseId)
            .ToHashSet(StringComparer.Ordinal);
        var courses = Curriculum.Select(course =>
        {
            bool learned = completedKeys.Contains(course.CourseId);
            bool prerequisitesMet =
                course.Prerequisites.All(completedKeys.Contains);
            bool levelOk = level >= course.RequiredAcademyLevel;
            bool affordable = course.Costs.All(cost =>
                hive.Resources.TryGetValue(cost.Key,out ResourceBalance? balance) &&
                balance.Amount >= cost.Value);
            bool ready = !learned && prerequisitesMet && levelOk;
            string status = learned ? "completed"
                : !levelOk || !prerequisitesMet ? "locked"
                : "available";
            return new AcademyCourseOffer(
                course.CourseId,course.RequiredAcademyLevel,
                course.Duration,course.Costs,course.Prerequisites,course.Credits,
                status,ready && data.ActiveOperation is null && affordable);
        }).ToArray();
        AcademyTrainingActive? operation = data.ActiveOperation is { } active
            ? new(active.OperationId,active.CourseId,active.StartedAtUtc,
                active.EndsAtUtc,now >= active.EndsAtUtc
                    ? "awaiting_completion" : "running")
            : null;
        return new(hive.PlayerId,hive.HiveId,ContractVersion,CatalogVersion,
            hive.Revision,data.Revision,now,level,
            new Dictionary<string,ResourceBalance>(hive.Resources),
            courses,data.Completed.OrderBy(x => x.CompletedAtUtc).ToArray(),
            operation,data.Completed.Sum(x => x.Credits));
    }

    public static void ValidateStored(AcademyTrainingState data,long hiveRevision)
    {
        if (data.Revision < 0 || data.Revision > hiveRevision ||
            data.Completed is null || data.Receipts is null ||
            data.Completed.Count > Curriculum.Count ||
            data.Receipts.Count > MaxReceipts ||
            data.Completed.Any(x => Find(x.CourseId) is not { } course ||
                x.Credits != course.Credits || x.CompletedAtUtc.Offset != TimeSpan.Zero) ||
            data.Completed.Select(x=>x.CourseId).Distinct(StringComparer.Ordinal).Count()
                != data.Completed.Count)
            throw new InvalidDataException("Invalid academy state.");
        foreach (var pair in data.Receipts)
        {
            AcademyTrainingStoredReceipt r = pair.Value;
            if (!ValidIdempotencyKey(pair.Key) || r is null ||
                r.PayloadHash is null || r.PayloadHash.Length != 64 ||
                r.PayloadHash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')) ||
                Find(r.CourseId) is null || r.Action is not ("start" or "complete") ||
                r.OperationId == Guid.Empty ||
                r.Code != (r.Action == "start" ? "game.academy_started" : "game.academy_completed") ||
                r.RevisionBefore < 0 || r.RevisionAfter != r.RevisionBefore+1 ||
                r.RevisionAfter > hiveRevision || r.AcceptedAtUtc.Offset != TimeSpan.Zero)
                throw new InvalidDataException("Invalid academy receipt.");
        }
        if (data.ActiveOperation is { } active)
        {
            if (active.OperationId == Guid.Empty || Find(active.CourseId) is not { } course ||
                data.Completed.Any(c => c.CourseId == active.CourseId) ||
                active.StartedAtUtc.Offset != TimeSpan.Zero ||
                active.EndsAtUtc.Offset != TimeSpan.Zero ||
                active.EndsAtUtc-active.StartedAtUtc != course.Duration)
                throw new InvalidDataException("Invalid academy operation.");
        }
    }
}
