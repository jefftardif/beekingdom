using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Infrastructure.Events;
using BeeKingdom.Infrastructure.Hosting;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Persistence.Abstractions;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Protocol.Requests;
using BeeKingdom.Protocol.Versioning;
using BeeKingdom.Shared.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class ArchitectureTests
{
    [Test]
    public void InfrastructureRegistersReusableServerServices()
    {
        ServiceProvider provider = CreateServices().BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IServerClock>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IEventBus>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IUnitOfWorkFactory>(), Is.Not.Null);
        });
    }

    [Test]
    public void ServerProfileKeepsBeeKingdomIndependent()
    {
        ServiceProvider provider = CreateServices().BuildServiceProvider();
        BeeKingdomServerHostProfile profile = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<BeeKingdomServerHostProfile>>()
            .Value;

        Assert.Multiple(() =>
        {
            Assert.That(profile.HostingModel, Is.EqualTo("IIS"));
            Assert.That(profile.TargetOperatingSystem, Is.EqualTo("Windows Server 2025"));
            Assert.That(profile.SqlServerRole, Does.Contain("Dedicated Bee Kingdom"));
        });
    }

    [Test]
    public void EventBusPublishesTypedEvents()
    {
        ServiceProvider provider = CreateServices().BuildServiceProvider();
        IEventBus bus = provider.GetRequiredService<IEventBus>();
        PingRequest? received = null;

        using IDisposable subscription = bus.Subscribe<PingRequest>(message => received = message);
        PingRequest request = new("tests", DateTimeOffset.UtcNow);
        bus.Publish(request);

        Assert.That(received, Is.EqualTo(request));
        Assert.That(bus.HasSubscribers<PingRequest>(), Is.True);
    }

    [Test]
    public void SharedSerializationUsesWebConventions()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(new PingRequest("tests", DateTimeOffset.UnixEpoch), BeeJson.CreateDefaultOptions());

        Assert.That(json, Does.Contain("clientBuild"));
    }

    private static IServiceCollection CreateServices()
    {
        Dictionary<string, string?> values = new()
        {
            ["BeeKingdom:ServerName"] = "BeeKingdom.Tests",
            ["ServerHost:HostingModel"] = "IIS",
            ["ServerHost:TargetOperatingSystem"] = "Windows Server 2025",
            ["ServerHost:SqlServerRole"] = "Dedicated Bee Kingdom SQL Server database",
            ["SqlServer:DatabaseName"] = "BeeKingdom",
            ["SqlServer:ConnectionStringName"] = "BeeKingdomDb"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomPersistence(configuration);
    }
}
