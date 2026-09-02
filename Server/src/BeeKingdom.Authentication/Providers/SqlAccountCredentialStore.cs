using System.Data;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Providers;

public sealed class SqlAccountCredentialStore : IAccountCredentialStore
{
    private readonly SqlConnectionFactory connectionFactory;
    private readonly IPasswordHasher passwordHasher;

    public SqlAccountCredentialStore(SqlConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
    {
        this.connectionFactory = connectionFactory;
        this.passwordHasher = passwordHasher;
    }

    public AuthenticationAccount CreateEmailAccount(string email, string password)
    {
        AuthenticationAccount account = new(
            Guid.NewGuid(),
            PlayerId.New(),
            email.Trim(),
            passwordHasher.HashPassword(password),
            AccountSecurityState.Active,
            0,
            null);
        Insert(account);
        return account;
    }

    public AuthenticationAccount CreateGoogleAccount(string googleSubjectId, string email)
    {
        AuthenticationAccount account = new(
            Guid.NewGuid(),
            PlayerId.New(),
            email.Trim(),
            null,
            AccountSecurityState.Active,
            0,
            null,
            googleSubjectId);
        Insert(account);
        return account;
    }

    private void Insert(AuthenticationAccount account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.AuthenticationAccounts
            (AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role)
            VALUES
            (@AccountId, @PlayerId, @Email, @PasswordHash, @SecurityState, @FailedAttempts, @LockedUntilUtc, @GoogleSubjectId, @DisplayName, @IsOnboarded, @Role);
            """;
        AddAccountParameters(command, account);
        command.ExecuteNonQuery();
    }

    public bool TryGetByEmail(string email, out AuthenticationAccount account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role
            FROM dbo.AuthenticationAccounts
            WHERE Email = @Email;
            """;
        Add(command, "@Email", email.Trim());
        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            account = null!;
            return false;
        }

        account = ReadAccount(reader);
        return true;
    }

    public bool TryGetByGoogleSubjectId(string googleSubjectId, out AuthenticationAccount account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role
            FROM dbo.AuthenticationAccounts
            WHERE GoogleSubjectId = @GoogleSubjectId;
            """;
        Add(command, "@GoogleSubjectId", googleSubjectId);
        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            account = null!;
            return false;
        }

        account = ReadAccount(reader);
        return true;
    }

    public bool TryGetByAccountId(Guid accountId, out AuthenticationAccount account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role
            FROM dbo.AuthenticationAccounts
            WHERE AccountId = @AccountId;
            """;
        Add(command, "@AccountId", accountId);
        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            account = null!;
            return false;
        }

        account = ReadAccount(reader);
        return true;
    }

    public bool TryGetByPlayerId(PlayerId playerId, out AuthenticationAccount account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role
            FROM dbo.AuthenticationAccounts
            WHERE PlayerId = @PlayerId;
            """;
        Add(command, "@PlayerId", playerId.Value);
        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            account = null!;
            return false;
        }

        account = ReadAccount(reader);
        return true;
    }

    public bool IsDisplayNameTaken(Guid worldId, string displayName, Guid excludingAccountId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.AuthenticationAccounts
            WHERE WorldId = @WorldId AND DisplayName = @DisplayName AND AccountId <> @AccountId;
            """;
        Add(command, "@WorldId", worldId);
        Add(command, "@DisplayName", displayName);
        Add(command, "@AccountId", excludingAccountId);
        object? result = command.ExecuteScalar();
        return result != null && Convert.ToInt32(result) > 0;
    }

    public IReadOnlyList<AuthenticationAccount> SearchByDisplayName(string displayNameContains)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role
            FROM dbo.AuthenticationAccounts
            WHERE DisplayName LIKE @DisplayNameContains;
            """;
        Add(command, "@DisplayNameContains", $"%{displayNameContains.Trim()}%");
        using IDataReader reader = command.ExecuteReader();
        List<AuthenticationAccount> results = new();
        while (reader.Read()) results.Add(ReadAccount(reader));
        return results;
    }

    public void Save(AuthenticationAccount account)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.AuthenticationAccounts
            SET PlayerId = @PlayerId,
                Email = @Email,
                PasswordHash = @PasswordHash,
                SecurityState = @SecurityState,
                FailedAttempts = @FailedAttempts,
                LockedUntilUtc = @LockedUntilUtc,
                GoogleSubjectId = @GoogleSubjectId,
                DisplayName = @DisplayName,
                IsOnboarded = @IsOnboarded,
                Role = @Role,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE AccountId = @AccountId;
            """;
        AddAccountParameters(command, account);
        command.ExecuteNonQuery();
    }

    private static AuthenticationAccount ReadAccount(IDataReader reader)
    {
        return new AuthenticationAccount(
            reader.GetGuid(0),
            new PlayerId(reader.GetGuid(1)),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            (AccountSecurityState)reader.GetInt32(4),
            reader.GetInt32(5),
            reader.IsDBNull(6) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            !reader.IsDBNull(9) && reader.GetBoolean(9),
            (AccountRole)reader.GetInt32(10));
    }

    private static void AddAccountParameters(IDbCommand command, AuthenticationAccount account)
    {
        Add(command, "@AccountId", account.AccountId);
        Add(command, "@PlayerId", account.PlayerId.Value);
        Add(command, "@Email", account.Email.Trim());
        Add(command, "@PasswordHash", (object?)account.PasswordHash ?? DBNull.Value);
        Add(command, "@SecurityState", (int)account.State);
        Add(command, "@FailedAttempts", account.FailedAttempts);
        Add(command, "@LockedUntilUtc", account.LockedUntilUtc.HasValue ? account.LockedUntilUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@GoogleSubjectId", (object?)account.GoogleSubjectId ?? DBNull.Value);
        Add(command, "@DisplayName", (object?)account.DisplayName ?? DBNull.Value);
        Add(command, "@IsOnboarded", account.IsOnboarded);
        Add(command, "@Role", (int)account.Role);
    }

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
