using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class GameWorkshopBatchQualificationEndpointTests
{
    [Test]
    public async Task Closed_flag_is_503_and_authentication_is_required()
    {
        await using WebApplicationFactory<Program> factory = Factory(false);
        using HttpClient client = factory.CreateClient();
        Assert.That((await client.PostAsJsonAsync($"/game/v1/hives/{Guid.NewGuid():D}/workshop/batch-qualification", new { expectedRevision = 0, answer = "heat", idempotencyKey = "a" })).StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        await using WebApplicationFactory<Program> open = Factory(true);
        using HttpClient unauth = open.CreateClient();
        Assert.That((await unauth.PostAsJsonAsync($"/game/v1/hives/{Guid.NewGuid():D}/workshop/batch-qualification", new { expectedRevision = 0, answer = "heat", idempotencyKey = "a" })).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Qualification_returns_incorrect_then_advances_and_replays()
    {
        await using WebApplicationFactory<Program> factory = Factory(true);
        using HttpClient client = factory.CreateClient();
        string email = $"workshop-{Guid.NewGuid():N}@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        using JsonDocument login = JsonDocument.Parse(await (await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1", ipAddress = "127.0.0.1", deviceIdentifier = "workshop", region = "local" })).Content.ReadAsStringAsync());
        string token = login.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, state => state with { WorkshopBatchQualification = new("production", 120, "chapter4.upgrade_batch_qualification", 0) });
        HttpResponseMessage wrong = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/workshop/batch-qualification", new { expectedRevision = 0, answer = "load", idempotencyKey = "wrong" });
        Assert.That(wrong.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument wrongJson = JsonDocument.Parse(await wrong.Content.ReadAsStringAsync()); Assert.That(wrongJson.RootElement.GetProperty("code").GetString(), Is.EqualTo("tutorial_answer_incorrect"));
        HttpResponseMessage right = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/workshop/batch-qualification", new { expectedRevision = 0, answer = "heat", idempotencyKey = "right" });
        Assert.That(right.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        string body = await right.Content.ReadAsStringAsync();
        HttpResponseMessage replay = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/workshop/batch-qualification", new { expectedRevision = 0, answer = "heat", idempotencyKey = "right" });
        Assert.That(await replay.Content.ReadAsStringAsync(), Is.EqualTo(body));
    }

    private static WebApplicationFactory<Program> Factory(bool enabled) => new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseSetting("environment", "Development"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["WorkshopBatchQualification:Enabled"] = enabled.ToString() })); });
}
