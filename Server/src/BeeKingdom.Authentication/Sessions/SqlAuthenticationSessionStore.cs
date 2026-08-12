using System.Data;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Sessions;

public sealed class SqlAuthenticationSessionStore : IAuthenticationSessionStore
{
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAuthenticationSessionStore(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public void Save(AuthenticationSession session)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AuthenticationSessions AS target
            USING (SELECT @SessionId AS SessionId) AS source
            ON target.SessionId = source.SessionId
            WHEN MATCHED THEN
                UPDATE SET AccountId = @AccountId,
                           PlayerId = @PlayerId,
                           AuthenticationProvider = @AuthenticationProvider,
                           LoginUtc = @LoginUtc,
                           LastActivityUtc = @LastActivityUtc,
                           ExpirationUtc = @ExpirationUtc,
                           ClientVersion = @ClientVersion,
                           IpAddress = @IpAddress,
                           DeviceIdentifier = @DeviceIdentifier,
                           Region = @Region,
                           IsRevoked = @IsRevoked
            WHEN NOT MATCHED THEN
                INSERT (SessionId, AccountId, PlayerId, AuthenticationProvider, LoginUtc, LastActivityUtc, ExpirationUtc,
                        ClientVersion, IpAddress, DeviceIdentifier, Region, IsRevoked)
                VALUES (@SessionId, @AccountId, @PlayerId, @AuthenticationProvider, @LoginUtc, @LastActivityUtc, @ExpirationUtc,
                        @ClientVersion, @IpAddress, @DeviceIdentifier, @Region, @IsRevoked);
            """;
        AddSessionParameters(command, session);
        command.ExecuteNonQuery();
    }

    public bool TryGet(string sessionId, out AuthenticationSession session)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectSql("WHERE SessionId = @SessionId");
        Add(command, "@SessionId", sessionId);
        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            session = null!;
            return false;
        }

        session = ReadSession(reader);
        return true;
    }

    public IReadOnlyList<AuthenticationSession> GetAccountSessions(Guid accountId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectSql("WHERE AccountId = @AccountId");
        Add(command, "@AccountId", accountId);
        using IDataReader reader = command.ExecuteReader();

        List<AuthenticationSession> sessions = new();
        while (reader.Read())
        {
            sessions.Add(ReadSession(reader));
        }

        return sessions;
    }

    private static string SelectSql(string whereClause)
    {
        return $"""
            SELECT SessionId, PlayerId, AccountId, AuthenticationProvider, LoginUtc, LastActivityUtc, ExpirationUtc,
                   ClientVersion, IpAddress, DeviceIdentifier, Region, IsRevoked
            FROM dbo.AuthenticationSessions
            {whereClause};
            """;
    }

    private static AuthenticationSession ReadSession(IDataReader reader)
    {
        return new AuthenticationSession(
            reader.GetString(0),
            new PlayerId(reader.GetGuid(1)),
            reader.GetGuid(2),
            (AuthenticationProviderKind)reader.GetInt32(3),
            AsUtc(reader.GetDateTime(4)),
            AsUtc(reader.GetDateTime(5)),
            AsUtc(reader.GetDateTime(6)),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetBoolean(11));
    }

    private static void AddSessionParameters(IDbCommand command, AuthenticationSession session)
    {
        Add(command, "@SessionId", session.SessionId);
        Add(command, "@AccountId", session.AccountId);
        Add(command, "@PlayerId", session.PlayerId.Value);
        Add(command, "@AuthenticationProvider", (int)session.AuthenticationProvider);
        Add(command, "@LoginUtc", session.LoginUtc.UtcDateTime);
        Add(command, "@LastActivityUtc", session.LastActivityUtc.UtcDateTime);
        Add(command, "@ExpirationUtc", session.ExpirationUtc.UtcDateTime);
        Add(command, "@ClientVersion", session.ClientVersion);
        Add(command, "@IpAddress", session.IpAddress);
        Add(command, "@DeviceIdentifier", session.DeviceIdentifier);
        Add(command, "@Region", session.Region);
        Add(command, "@IsRevoked", session.IsRevoked);
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
