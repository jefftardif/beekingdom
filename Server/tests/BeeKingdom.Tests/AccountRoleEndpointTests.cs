using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class AccountRoleEndpointTests
{
    private const string SupportKey = "test-support-key";

    [Test]
    public async Task Bootstrap_endpoint_requires_support_key()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var email = $"bootstrap-nokey-{Guid.NewGuid():N}@bee.test";
        var account = factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");

        var response = await client.PostAsJsonAsync($"/admin/v1/accounts/{account.AccountId:D}/role", new { role = "Admin", reason = "bootstrap" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Bootstrap_endpoint_grants_admin_role()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Support-Key", SupportKey);
        var email = $"bootstrap-{Guid.NewGuid():N}@bee.test";
        var account = factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");

        var response = await client.PostAsJsonAsync($"/admin/v1/accounts/{account.AccountId:D}/role", new { role = "Admin", reason = "First admin account" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        factory.Services.GetRequiredService<IAccountCredentialStore>().TryGetByAccountId(account.AccountId, out AuthenticationAccount updated);
        Assert.That(updated.Role, Is.EqualTo(AccountRole.Admin));
    }

    [Test]
    public async Task Lookup_and_assign_require_admin_role_on_the_caller()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsPlayer(factory, client, $"non-admin-{Guid.NewGuid():N}@bee.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var lookup = await client.GetAsync("/accounts/v1/role/lookup?query=any");
        var assign = await client.PostAsJsonAsync("/accounts/v1/role/assign", new { targetAccountId = Guid.NewGuid(), role = "Moderator" });

        Assert.Multiple(() =>
        {
            Assert.That(lookup.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(assign.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        });
    }

    [Test]
    public async Task Admin_can_look_up_and_promote_a_player_to_moderator()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();

        string adminEmail = $"admin-{Guid.NewGuid():N}@bee.test";
        string adminToken = await LoginAsPlayer(factory, client, adminEmail);
        credentials.TryGetByEmail(adminEmail, out AuthenticationAccount adminAccount);
        credentials.Save(adminAccount with { Role = AccountRole.Admin });

        string targetEmail = $"target-{Guid.NewGuid():N}@bee.test";
        await LoginAsPlayer(factory, client, targetEmail);
        credentials.TryGetByEmail(targetEmail, out AuthenticationAccount targetAccount);
        credentials.Save(targetAccount with { DisplayName = "Nectar Scout" });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var lookup = await client.GetFromJsonAsync<JsonElement>("/accounts/v1/role/lookup?query=Nectar");
        Assert.That(lookup.EnumerateArray().Any(x => x.GetProperty("accountId").GetGuid() == targetAccount.AccountId), Is.True);

        var assign = await client.PostAsJsonAsync("/accounts/v1/role/assign", new { targetAccountId = targetAccount.AccountId, role = "Moderator" });
        Assert.That(assign.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        credentials.TryGetByAccountId(targetAccount.AccountId, out AuthenticationAccount promoted);
        Assert.That(promoted.Role, Is.EqualTo(AccountRole.Moderator));
    }

    [Test]
    public async Task Assign_cannot_grant_admin_role()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();

        string adminEmail = $"admin-noadmin-{Guid.NewGuid():N}@bee.test";
        string adminToken = await LoginAsPlayer(factory, client, adminEmail);
        credentials.TryGetByEmail(adminEmail, out AuthenticationAccount adminAccount);
        credentials.Save(adminAccount with { Role = AccountRole.Admin });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.PostAsJsonAsync("/accounts/v1/role/assign", new { targetAccountId = Guid.NewGuid(), role = "Admin" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private static async Task<string> LoginAsPlayer(WebApplicationFactory<Program> factory, HttpClient client, string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0" });
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }

    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("environment", "Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminSupport:Enabled"] = "true",
            ["AdminSupport:Key"] = SupportKey
        }));
    });
}
