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

public sealed class AdminAccountPromotionTests
{
    private static WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("environment", "Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["AdminSupport:Enabled"] = "false" }));
    });

    [Test]
    public async Task Anonymous_caller_cannot_promote()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/accounts/v1/admin/accounts/{Guid.NewGuid():D}/promote", new { confirmEmail = "target@bee.test" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(AccountRole.Player)]
    [TestCase(AccountRole.Moderator)]
    public async Task Non_admin_cannot_promote(AccountRole role)
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await Login(factory, client, role);
        var response = await client.PostAsJsonAsync($"/accounts/v1/admin/accounts/{Guid.NewGuid():D}/promote", new { confirmEmail = "target@bee.test" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Admin_promotes_exact_target_and_retry_is_idempotent()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await Login(factory, client, AccountRole.Admin);
        var store = factory.Services.GetRequiredService<IAccountCredentialStore>();
        var target = store.CreateEmailAccount($"target-{Guid.NewGuid():N}@bee.test", "secret");
        var url = $"/accounts/v1/admin/accounts/{target.AccountId:D}/promote";
        var response = await client.PostAsJsonAsync(url, new { confirmEmail = target.Email.ToUpperInvariant() });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("role").GetString(), Is.EqualTo("Admin"));
        Assert.That(json.GetProperty("changed").GetBoolean(), Is.True);
        Assert.That(json.EnumerateObject().Count(), Is.EqualTo(3));
        store.TryGetByAccountId(target.AccountId, out AuthenticationAccount promoted);
        Assert.That(promoted.Role, Is.EqualTo(AccountRole.Admin));
        Assert.That(promoted.Email, Is.EqualTo(target.Email));
        response = await client.PostAsJsonAsync(url, new { confirmEmail = target.Email });
        json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("changed").GetBoolean(), Is.False);
    }

    [Test]
    public async Task Wrong_confirmation_never_changes_role()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        await Login(factory, client, AccountRole.Admin);
        var store = factory.Services.GetRequiredService<IAccountCredentialStore>();
        var target = store.CreateEmailAccount($"target-{Guid.NewGuid():N}@bee.test", "secret");
        var response = await client.PostAsJsonAsync($"/accounts/v1/admin/accounts/{target.AccountId:D}/promote", new { confirmEmail = "wrong@bee.test" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        store.TryGetByAccountId(target.AccountId, out AuthenticationAccount unchanged);
        Assert.That(unchanged.Role, Is.EqualTo(AccountRole.Player));
    }

    [Test]
    public async Task Revoked_admin_role_is_checked_again_despite_existing_session()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var caller = await Login(factory, client, AccountRole.Admin);
        var store = factory.Services.GetRequiredService<IAccountCredentialStore>();
        store.Save(caller with { Role = AccountRole.Player });
        var response = await client.PostAsJsonAsync($"/accounts/v1/admin/accounts/{Guid.NewGuid():D}/promote", new { confirmEmail = "target@bee.test" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task<AuthenticationAccount> Login(WebApplicationFactory<Program> factory, HttpClient client, AccountRole role)
    {
        var store = factory.Services.GetRequiredService<IAccountCredentialStore>();
        var email = $"caller-{Guid.NewGuid():N}@bee.test";
        var account = store.CreateEmailAccount(email, "secret");
        account = account with { Role = role };
        store.Save(account);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0" });
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("tokens").GetProperty("accessToken").GetString());
        return account;
    }
}
