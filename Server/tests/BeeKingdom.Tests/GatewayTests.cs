using BeeKingdom.Accounts.DependencyInjection;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.DependencyInjection;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Gateway;
using BeeKingdom.Gateway.DependencyInjection;
using BeeKingdom.Gateway.Models;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Protocol;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Protocol.Requests;
using BeeKingdom.Protocol.Versioning;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class GatewayTests
{
    [Test]
    public void AcceptConnectionCreatesConnectingConnection()
    {
        GatewayManager gateway = CreateProvider().GetRequiredService<GatewayManager>();

        GatewayConnection connection = gateway.AcceptConnection(CreateConnectionRequest());

        Assert.Multiple(() =>
        {
            Assert.That(connection.ConnectionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(connection.ConnectionState, Is.EqualTo(GatewayConnectionState.Connecting));
            Assert.That(gateway.GetGatewayStatistics().ActiveConnections, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task AuthenticateSessionConnectsValidatedPlayer()
    {
        ServiceProvider provider = CreateProvider();
        AuthenticationResult auth = await CreateAuthenticatedSession(provider);
        GatewayManager gateway = provider.GetRequiredService<GatewayManager>();
        GatewayConnection connection = gateway.AcceptConnection(CreateConnectionRequest());

        GatewayConnection authenticated = gateway.AuthenticateSession(connection.ConnectionId, auth.Tokens!.AccessToken);

        Assert.Multiple(() =>
        {
            Assert.That(authenticated.ConnectionState, Is.EqualTo(GatewayConnectionState.Connected));
            Assert.That(authenticated.PlayerId, Is.EqualTo(auth.PlayerId));
            Assert.That(authenticated.SessionId, Is.EqualTo(auth.Session!.SessionId));
        });
    }

    [Test]
    public async Task RouteMessageUsesConfiguredRoute()
    {
        ServiceProvider provider = CreateProvider();
        AuthenticationResult auth = await CreateAuthenticatedSession(provider);
        GatewayManager gateway = provider.GetRequiredService<GatewayManager>();
        GatewayConnection connection = gateway.AuthenticateSession(gateway.AcceptConnection(CreateConnectionRequest()).ConnectionId, auth.Tokens!.AccessToken);
        ProtocolMessage<PingRequest> message = CreateMessage(auth, ProtocolMessageType.Request);
        byte[] bytes = provider.GetRequiredService<ProtocolManager>().Serialize(message);

        GatewayRouteResult result = gateway.RouteMessage(connection.ConnectionId, message, bytes.Length);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Target, Is.EqualTo(GatewayServiceTarget.Account));
            Assert.That(gateway.GetGatewayStatistics().MessagesRouted, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DisconnectClosesConnection()
    {
        ServiceProvider provider = CreateProvider();
        AuthenticationResult auth = await CreateAuthenticatedSession(provider);
        GatewayManager gateway = provider.GetRequiredService<GatewayManager>();
        GatewayConnection connection = gateway.AuthenticateSession(gateway.AcceptConnection(CreateConnectionRequest()).ConnectionId, auth.Tokens!.AccessToken);

        bool disconnected = gateway.Disconnect(connection.ConnectionId);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected, Is.True);
            Assert.That(gateway.QueryConnections()[0].ConnectionState, Is.EqualTo(GatewayConnectionState.Disconnected));
            Assert.That(gateway.GetGatewayStatistics().ActiveConnections, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task RateLimitRejectsExcessMessages()
    {
        ServiceProvider provider = CreateProvider(playerMessagesPerMinute: 1);
        AuthenticationResult auth = await CreateAuthenticatedSession(provider);
        GatewayManager gateway = provider.GetRequiredService<GatewayManager>();
        GatewayConnection connection = gateway.AuthenticateSession(gateway.AcceptConnection(CreateConnectionRequest()).ConnectionId, auth.Tokens!.AccessToken);
        ProtocolMessage<PingRequest> message = CreateMessage(auth, ProtocolMessageType.Request);
        byte[] bytes = provider.GetRequiredService<ProtocolManager>().Serialize(message);

        GatewayRouteResult first = gateway.RouteMessage(connection.ConnectionId, message, bytes.Length);
        GatewayRouteResult second = gateway.RouteMessage(connection.ConnectionId, message with { MessageId = Guid.NewGuid() }, bytes.Length);

        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.ErrorCode, Is.EqualTo("rate_limited"));
        });
    }

    private static GatewayConnectionRequest CreateConnectionRequest()
    {
        return new GatewayConnectionRequest("1.0.0", ProtocolVersion.Current, "local", "127.0.0.1");
    }

    private static ProtocolMessage<PingRequest> CreateMessage(AuthenticationResult auth, ProtocolMessageType type)
    {
        return ProtocolMessage<PingRequest>.Create(
            type,
            auth.Session!.SessionId,
            auth.PlayerId,
            ColonyId.New(),
            new PingRequest("tests", DateTimeOffset.UnixEpoch));
    }

    private static async Task<AuthenticationResult> CreateAuthenticatedSession(ServiceProvider provider)
    {
        provider.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(Guid.NewGuid().ToString("N") + "@bee.test", "secret");
        IAccountCredentialStore accounts = provider.GetRequiredService<IAccountCredentialStore>();
        string email = "gateway-" + Guid.NewGuid().ToString("N") + "@bee.test";
        accounts.CreateEmailAccount(email, "secret");
        return await provider.GetRequiredService<AuthenticationManager>().Authenticate(new AuthenticationRequest(email, "secret", "1.0.0", "127.0.0.1", "device", "local"));
    }

    private static ServiceProvider CreateProvider(int playerMessagesPerMinute = 120)
    {
        Dictionary<string, string?> values = new()
        {
            ["Authentication:AccessTokenLifetime"] = "00:15:00",
            ["Authentication:RefreshTokenLifetime"] = "14.00:00:00",
            ["Authentication:MaxSessionsPerAccount"] = "5",
            ["Authentication:MaxFailedAttempts"] = "5",
            ["Authentication:LockoutDuration"] = "00:10:00",
            ["Gateway:MaxConnections"] = "100",
            ["Gateway:MaxMessageBytes"] = "65536",
            ["Gateway:PlayerMessagesPerMinute"] = playerMessagesPerMinute.ToString(),
            ["Gateway:SessionMessagesPerMinute"] = "120",
            ["Gateway:IpMessagesPerMinute"] = "300",
            ["Gateway:MessageTypePerMinute"] = "240"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomAuthentication(configuration)
            .AddBeeKingdomAccounts(configuration)
            .AddBeeKingdomGateway(configuration)
            .BuildServiceProvider();
    }
}
