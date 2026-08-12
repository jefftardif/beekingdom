using System.Data;
using System.Text.Json;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts.Repositories;

public sealed class SqlAccountRepository : IAccountRepository
{
    private static readonly JsonSerializerOptions JsonOptions = BeeJson.CreateDefaultOptions();
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAccountRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public AccountRecord Create(AccountRecord account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Accounts
            (AccountId, PlayerId, DisplayName, Email, Language, TimeZone, Country, Currency, Status, AnalyticsEnabled, CrossPlayEnabled,
             PreferencesJson, ProgressionJson, CreatedAtUtc, LastLoginUtc)
            VALUES
            (@AccountId, @PlayerId, @DisplayName, @Email, @Language, @TimeZone, @Country, @Currency, @Status, @AnalyticsEnabled, @CrossPlayEnabled,
             @PreferencesJson, @ProgressionJson, @CreatedAtUtc, @LastLoginUtc);
            """;
        AddAccountParameters(command, account);
        command.ExecuteNonQuery();
        return account;
    }

    public AccountRecord? Get(Guid accountId)
    {
        return QuerySingle("WHERE AccountId = @AccountId", command => Add(command, "@AccountId", accountId));
    }

    public AccountRecord? GetByPlayerId(PlayerId playerId)
    {
        return QuerySingle("WHERE PlayerId = @PlayerId", command => Add(command, "@PlayerId", playerId.Value));
    }

    public AccountRecord? GetByEmail(string email)
    {
        return QuerySingle("WHERE Email = @Email", command => Add(command, "@Email", email.Trim()));
    }

    public AccountRecord Save(AccountRecord account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Accounts
            SET PlayerId = @PlayerId,
                DisplayName = @DisplayName,
                Email = @Email,
                Language = @Language,
                TimeZone = @TimeZone,
                Country = @Country,
                Currency = @Currency,
                Status = @Status,
                AnalyticsEnabled = @AnalyticsEnabled,
                CrossPlayEnabled = @CrossPlayEnabled,
                PreferencesJson = @PreferencesJson,
                ProgressionJson = @ProgressionJson,
                CreatedAtUtc = @CreatedAtUtc,
                LastLoginUtc = @LastLoginUtc,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE AccountId = @AccountId;
            """;
        AddAccountParameters(command, account);
        command.ExecuteNonQuery();
        return account;
    }

    public IReadOnlyList<AccountRecord> Query(AccountQuery query)
    {
        List<string> filters = new();
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            filters.Add("Email = @Email");
            Add(command, "@Email", query.Email.Trim());
        }

        if (query.Status.HasValue)
        {
            filters.Add("Status = @Status");
            Add(command, "@Status", (int)query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.DisplayNameContains))
        {
            filters.Add("DisplayName LIKE @DisplayNameContains");
            Add(command, "@DisplayNameContains", $"%{query.DisplayNameContains.Trim()}%");
        }

        string where = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        command.CommandText = SelectSql(where);
        using IDataReader reader = command.ExecuteReader();
        return ReadAccounts(reader).OrderBy(account => account.Profile.CreationDate).ToArray();
    }

    private AccountRecord? QuerySingle(string whereClause, Action<IDbCommand> configure)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectSql(whereClause);
        configure(command);
        using IDataReader reader = command.ExecuteReader();
        return ReadAccounts(reader).FirstOrDefault();
    }

    private static string SelectSql(string whereClause)
    {
        return $"""
            SELECT AccountId, PlayerId, DisplayName, Email, Language, TimeZone, Country, Currency, Status, AnalyticsEnabled, CrossPlayEnabled,
                   PreferencesJson, ProgressionJson, CreatedAtUtc, LastLoginUtc
            FROM dbo.Accounts
            {whereClause};
            """;
    }

    private static IReadOnlyList<AccountRecord> ReadAccounts(IDataReader reader)
    {
        List<AccountRecord> accounts = new();
        while (reader.Read())
        {
            AccountPreferences preferences = JsonSerializer.Deserialize<AccountPreferences>(reader.GetString(11), JsonOptions)
                ?? throw new InvalidOperationException("Invalid account preferences payload.");
            AccountProgression progression = DeserializeProgression(reader.GetString(12));

            AccountProfile profile = new(
                reader.GetGuid(0),
                new PlayerId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                AsUtc(reader.GetDateTime(13)),
                reader.IsDBNull(14) ? null : AsUtc(reader.GetDateTime(14)),
                (AccountStatus)reader.GetInt32(8));
            AccountSettings settings = new(reader.GetString(7), reader.GetBoolean(9), reader.GetBoolean(10));
            accounts.Add(new AccountRecord(profile, settings, preferences, progression));
        }

        return accounts;
    }

    private static void AddAccountParameters(IDbCommand command, AccountRecord account)
    {
        Add(command, "@AccountId", account.Profile.AccountId);
        Add(command, "@PlayerId", account.Profile.PlayerId.Value);
        Add(command, "@DisplayName", account.Profile.DisplayName);
        Add(command, "@Email", account.Profile.Email.Trim());
        Add(command, "@Language", account.Profile.Language);
        Add(command, "@TimeZone", account.Profile.TimeZone);
        Add(command, "@Country", account.Profile.Country);
        Add(command, "@Currency", account.Settings.Currency);
        Add(command, "@Status", (int)account.Profile.Status);
        Add(command, "@AnalyticsEnabled", account.Settings.AnalyticsEnabled);
        Add(command, "@CrossPlayEnabled", account.Settings.CrossPlayEnabled);
        Add(command, "@PreferencesJson", JsonSerializer.Serialize(account.Preferences, JsonOptions));
        Add(command, "@ProgressionJson", SerializeProgression(account.Progression));
        Add(command, "@CreatedAtUtc", account.Profile.CreationDate.UtcDateTime);
        Add(command, "@LastLoginUtc", account.Profile.LastLogin.HasValue ? account.Profile.LastLogin.Value.UtcDateTime : DBNull.Value);
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string SerializeProgression(AccountProgression progression)
    {
        ArgumentNullException.ThrowIfNull(progression);

        AccountProgressionSqlDto dto = new(
            NormalizeSetForStorage(progression.GlobalAchievements),
            progression.GlobalStatistics
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
            NormalizeSetForStorage(progression.PermanentRewards),
            progression.SeasonHistory.ToList(),
            progression.PurchaseHistory.ToList());

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static AccountProgression DeserializeProgression(string payload)
    {
        try
        {
            AccountProgressionSqlDto dto = JsonSerializer.Deserialize<AccountProgressionSqlDto>(payload, JsonOptions)
                ?? throw new InvalidDataException("Account progression payload cannot be null.");

            return new AccountProgression(
                NormalizeSetFromStorage(RequireField(dto.GlobalAchievements, nameof(dto.GlobalAchievements)), nameof(dto.GlobalAchievements)),
                new Dictionary<string, double>(RequireField(dto.GlobalStatistics, nameof(dto.GlobalStatistics)), StringComparer.Ordinal),
                NormalizeSetFromStorage(RequireField(dto.PermanentRewards, nameof(dto.PermanentRewards)), nameof(dto.PermanentRewards)),
                NormalizeListFromStorage(RequireField(dto.SeasonHistory, nameof(dto.SeasonHistory)), nameof(dto.SeasonHistory)),
                NormalizeListFromStorage(RequireField(dto.PurchaseHistory, nameof(dto.PurchaseHistory)), nameof(dto.PurchaseHistory)));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Account progression payload is malformed.", exception);
        }
    }

    private static List<string> NormalizeSetForStorage(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
    }

    private static HashSet<string> NormalizeSetFromStorage(IEnumerable<string> values, string fieldName)
    {
        HashSet<string> normalized = new(StringComparer.Ordinal);
        foreach (string? value in values)
        {
            if (value is null)
            {
                throw new InvalidDataException($"Account progression field '{fieldName}' contains a null value.");
            }

            normalized.Add(value);
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeListFromStorage(IEnumerable<string> values, string fieldName)
    {
        List<string> normalized = new();
        foreach (string? value in values)
        {
            if (value is null)
            {
                throw new InvalidDataException($"Account progression field '{fieldName}' contains a null value.");
            }

            normalized.Add(value);
        }

        return normalized;
    }

    private static T RequireField<T>(T? value, string fieldName) where T : class
    {
        return value ?? throw new InvalidDataException($"Account progression field '{fieldName}' cannot be null.");
    }

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed class AccountProgressionSqlDto
    {
        public List<string>? GlobalAchievements { get; init; } = [];
        public Dictionary<string, double>? GlobalStatistics { get; init; } = new(StringComparer.Ordinal);
        public List<string>? PermanentRewards { get; init; } = [];
        public List<string>? SeasonHistory { get; init; } = [];
        public List<string>? PurchaseHistory { get; init; } = [];

        public AccountProgressionSqlDto()
        {
        }

        public AccountProgressionSqlDto(
            List<string> globalAchievements,
            Dictionary<string, double> globalStatistics,
            List<string> permanentRewards,
            List<string> seasonHistory,
            List<string> purchaseHistory)
        {
            GlobalAchievements = globalAchievements;
            GlobalStatistics = globalStatistics;
            PermanentRewards = permanentRewards;
            SeasonHistory = seasonHistory;
            PurchaseHistory = purchaseHistory;
        }
    }
}
