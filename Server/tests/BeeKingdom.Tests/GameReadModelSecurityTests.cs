using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BeeKingdom.Tests;

public sealed class GameReadModelSecurityTests
{
    [Test]
    public async Task Authenticated_game_reads_are_private_and_not_cached_publicly()
    {
        await using var factory = Factory(new Dictionary<string, string?>
        {
            ["CombatFormationReadiness:Enabled"] = "true",
            ["CombatSquadReservation:Enabled"] = "true",
            ["HivePerimeterSortie:Enabled"] = "true"
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/game/v1/hives/{Guid.NewGuid()}/combat/formation-readiness");
        Assert.That(response.Headers.CacheControl?.Private, Is.True);
        Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
        Assert.That(response.Headers.Pragma.Any(value => value.ToString().Contains("no-cache", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task Enabled_game_routes_return_safe_401_for_missing_bearer()
    {
        await using var factory = Factory(new Dictionary<string, string?>
        {
            ["CombatFormationReadiness:Enabled"] = "true",
            ["CombatSquadReservation:Enabled"] = "true",
            ["HivePerimeterSortie:Enabled"] = "true"
        });
        using var client = factory.CreateClient();
        string hive = Guid.NewGuid().ToString();
        string[] paths =
        {
            $"/game/v1/hives/{hive}/combat/formation-readiness",
            $"/game/v1/hives/{hive}/combat/squad-reservation",
            $"/game/v1/hives/{hive}/perimeter-sortie"
        };
        foreach (string path in paths)
        {
            using HttpResponseMessage response = await client.GetAsync(path);
            string body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), path);
            Assert.That(body, Does.Contain("game.session_required"), path);
            Assert.That(body, Does.Not.Contain("playerId"), path);
        }
    }

    private static WebApplicationFactory<Program> Factory(IReadOnlyDictionary<string, string?> settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        });
}
