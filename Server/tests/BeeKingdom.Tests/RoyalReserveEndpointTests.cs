using System.Net;
using System.Net.Http.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class RoyalReserveEndpointTests
{
    [Test]
    public async Task RoyalReserveEndpointsRequireAuthentication()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Guid hive = Guid.NewGuid();

        using HttpResponseMessage response =
            await client.GetAsync($"/game/v1/hives/{hive:D}/bank/royal-reserve");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AuthenticatedReadDepositAndWithdrawRoundTrip()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Guid hive = Guid.NewGuid();

        string token = await Login(factory, client, $"bank-{Guid.NewGuid():N}@bee.test");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;

        using HttpResponseMessage ensure =
            await client.PostAsync($"/game/v1/hives/{hive:D}/ensure", null);
        Assert.That(ensure.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        IHiveStateRepository repository = factory.Services.GetRequiredService<IHiveStateRepository>();
        await repository.ExecuteAtomicallyAsync(player, hive, state =>
        {
            Dictionary<string, int> levels = new(state.BuildingLevels, StringComparer.Ordinal)
            {
                ["hive_bank"] = 2,
                ["honey_storage"] = 1,
                ["wax_workshop"] = 1,
                ["warehouse_cells"] = 1
            };
            Dictionary<string, ResourceBalance> resources = new(StringComparer.Ordinal)
            {
                ["honey"] = new(1_000, 1_000_000),
                ["wax"] = new(500, 1_000_000),
                ["pollen"] = new(500, 1_000_000)
            };
            return state with { BuildingLevels = levels, Resources = resources };
        });

        RoyalReserveReadSnapshot? initial =
            await client.GetFromJsonAsync<RoyalReserveReadSnapshot>(
                $"/game/v1/hives/{hive:D}/bank/royal-reserve");

        Assert.That(initial, Is.Not.Null);
        Assert.That(initial!.ContractVersion, Is.EqualTo(RoyalReserveService.ContractVersion));
        Assert.That(initial.BankLevel, Is.EqualTo(2));
        Assert.That(initial.Capacity, Is.EqualTo(20_000));
        Assert.That(initial.ReservedTotal, Is.Zero);
        Assert.That(initial.LiquidBalances["honey"].Amount, Is.EqualTo(1_000));

        using HttpResponseMessage depositResponse = await client.PostAsJsonAsync(
            $"/game/v1/hives/{hive:D}/bank/royal-reserve/deposit",
            new
            {
                resourceKey = "honey",
                amount = 300,
                expectedRevision = initial.Revision,
                idempotencyKey = "http-deposit"
            });
        Assert.That(depositResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await depositResponse.Content.ReadAsStringAsync());

        RoyalReserveReadSnapshot? deposited =
            await depositResponse.Content.ReadFromJsonAsync<RoyalReserveReadSnapshot>();
        Assert.That(deposited, Is.Not.Null);
        Assert.That(deposited!.Reserved["honey"], Is.EqualTo(300));
        Assert.That(deposited.LiquidBalances["honey"].Amount, Is.EqualTo(700));

        using HttpResponseMessage withdrawResponse = await client.PostAsJsonAsync(
            $"/game/v1/hives/{hive:D}/bank/royal-reserve/withdraw",
            new
            {
                resourceKey = "honey",
                amount = 125,
                expectedRevision = deposited.Revision,
                idempotencyKey = "http-withdraw"
            });
        Assert.That(withdrawResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await withdrawResponse.Content.ReadAsStringAsync());

        RoyalReserveReadSnapshot? withdrawn =
            await withdrawResponse.Content.ReadFromJsonAsync<RoyalReserveReadSnapshot>();
        Assert.That(withdrawn, Is.Not.Null);
        Assert.That(withdrawn!.Reserved["honey"], Is.EqualTo(175));
        Assert.That(withdrawn.LiquidBalances["honey"].Amount, Is.EqualTo(825));

        PlayerHiveState? persisted = await repository.ReadAsync(player, hive);
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.RoyalReserve!.Amounts["honey"], Is.EqualTo(175));
        Assert.That(persisted.Resources["honey"].Amount, Is.EqualTo(825));
    }

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory,
        HttpClient client,
        string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email, "secret");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                email,
                password = "secret",
                clientVersion = "1.0.0",
                ipAddress = "127.0.0.1",
                deviceIdentifier = "royal-reserve-tests",
                region = "local"
            });

        response.EnsureSuccessStatusCode();
        using System.Text.Json.JsonDocument json =
            System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement
            .GetProperty("tokens")
            .GetProperty("accessToken")
            .GetString()!;
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder => builder.UseSetting("environment", "Development"));
}
