using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

// M056-CL: coverage for the Admin-gated account lookup + irreversible deletion cascade behind
// /bk-admin/accounts. Every account used here is synthetic and created by the test itself.
public sealed class AccountDeletionTests
{
    [Test]
    public async Task Lookup_and_delete_require_admin_role_on_the_caller()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        string victimEmail = Email("victim");
        AuthenticationAccount victim = SeedAccount(factory, victimEmail);

        string token = await LoginAsPlayer(factory, client, Email("plain-player"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var lookup = await client.GetAsync($"/accounts/v1/admin/lookup?email={Uri.EscapeDataString(victimEmail)}");
        var delete = await SendDeleteAsync(client, victim.AccountId, victimEmail);

        Assert.Multiple(() =>
        {
            Assert.That(lookup.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        });

        // And crucially: the refused call changed nothing.
        Assert.That(factory.Services.GetRequiredService<IAccountCredentialStore>().TryGetByEmail(victimEmail, out _), Is.True);
    }

    [Test]
    public async Task Lookup_without_a_session_is_rejected()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/accounts/v1/admin/lookup?email=nobody@bee.test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Lookup_returns_404_for_an_unknown_email()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);

        var response = await client.GetAsync($"/accounts/v1/admin/lookup?email={Uri.EscapeDataString(Email("ghost"))}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_returns_404_for_a_nonexistent_account()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);

        var response = await SendDeleteAsync(client, Guid.NewGuid(), Email("nobody"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Lookup_reports_the_hives_that_would_be_deleted()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);

        string email = Email("with-hive");
        AuthenticationAccount account = SeedAccount(factory, email, displayName: "Pollen Scout");
        Guid hiveId = await SeedHiveAsync(factory, account.PlayerId);

        var detail = await client.GetFromJsonAsync<JsonElement>($"/accounts/v1/admin/lookup?email={Uri.EscapeDataString(email)}");

        Assert.Multiple(() =>
        {
            Assert.That(detail.GetProperty("accountId").GetGuid(), Is.EqualTo(account.AccountId));
            Assert.That(detail.GetProperty("playerId").GetGuid(), Is.EqualTo(account.PlayerId.Value));
            Assert.That(detail.GetProperty("email").GetString(), Is.EqualTo(email));
            Assert.That(detail.GetProperty("displayName").GetString(), Is.EqualTo("Pollen Scout"));
            Assert.That(detail.GetProperty("role").GetString(), Is.EqualTo("Player"));
            Assert.That(detail.GetProperty("securityState").GetString(), Is.EqualTo("Active"));
            Assert.That(detail.GetProperty("hiveCount").GetInt32(), Is.EqualTo(1));
            Assert.That(detail.GetProperty("hiveIds").EnumerateArray().Single().GetGuid(), Is.EqualTo(hiveId));
        });
    }

    // The headline requirement: after deletion the email must be free to register again.
    [Test]
    public async Task Delete_removes_the_credential_row_and_frees_the_email_for_reuse()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();

        string email = Email("reusable");
        AuthenticationAccount account = SeedAccount(factory, email);
        Guid originalPlayerId = account.PlayerId.Value;

        var response = await SendDeleteAsync(client, account.AccountId, email);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var summary = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.That(summary.GetProperty("credentialRowDeleted").GetBoolean(), Is.True);

        // Gone from the store, and gone from the admin lookup.
        Assert.That(credentials.TryGetByEmail(email, out _), Is.False);
        Assert.That(credentials.TryGetByAccountId(account.AccountId, out _), Is.False);
        var lookupAfter = await client.GetAsync($"/accounts/v1/admin/lookup?email={Uri.EscapeDataString(email)}");
        Assert.That(lookupAfter.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        // The whole point: the same address registers again, as a genuinely NEW player.
        AuthenticationAccount recreated = credentials.CreateEmailAccount(email, "secret");
        Assert.Multiple(() =>
        {
            Assert.That(recreated.AccountId, Is.Not.EqualTo(account.AccountId));
            Assert.That(recreated.PlayerId.Value, Is.Not.EqualTo(originalPlayerId));
            Assert.That(credentials.TryGetByEmail(email, out _), Is.True);
        });

        // And the fresh account can actually log in with the reused address.
        var login = await factory.CreateClient().PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0" });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Delete_removes_every_hive_state_owned_by_the_player()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var hives = factory.Services.GetRequiredService<IHiveStateRepository>();

        string email = Email("hive-owner");
        AuthenticationAccount account = SeedAccount(factory, email);
        Guid hiveA = await SeedHiveAsync(factory, account.PlayerId);
        Guid hiveB = await SeedHiveAsync(factory, account.PlayerId);

        // A second, untouched player - proof the cascade is scoped to one player.
        AuthenticationAccount bystander = SeedAccount(factory, Email("hive-bystander"));
        Guid bystanderHive = await SeedHiveAsync(factory, bystander.PlayerId);

        var response = await SendDeleteAsync(client, account.AccountId, email);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var summary = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.That(summary.GetProperty("hivesDeleted").GetInt32(), Is.EqualTo(2));

        Assert.Multiple(async () =>
        {
            Assert.That(await hives.ReadAsync(account.PlayerId.Value, hiveA), Is.Null);
            Assert.That(await hives.ReadAsync(account.PlayerId.Value, hiveB), Is.Null);
            Assert.That((await hives.ListHiveIdsAsync(account.PlayerId.Value)), Is.Empty);
            Assert.That(await hives.ReadAsync(bystander.PlayerId.Value, bystanderHive), Is.Not.Null);
        });
    }

    [Test]
    public async Task Delete_removes_chat_participation_without_touching_other_participants_or_message_history()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var chat = factory.Services.GetRequiredService<IChatRepository>();

        string email = Email("chatter");
        AuthenticationAccount account = SeedAccount(factory, email);
        AuthenticationAccount survivor = SeedAccount(factory, Email("chat-survivor"));

        Guid conversationId = Guid.NewGuid();
        chat.SaveConversation(
            new ChatConversation(conversationId, Guid.NewGuid(), Guid.NewGuid(), ChatChannelType.Alliance, $"aud-{conversationId:N}", "Test room", survivor.PlayerId, DateTimeOffset.UtcNow, null, null, "standard", 1),
            new[]
            {
                Participant(conversationId, survivor.PlayerId),
                Participant(conversationId, account.PlayerId)
            });

        ChatMessage authored = SaveMessage(chat, conversationId, account.PlayerId, "message from the doomed account");
        ChatMessage othersMessage = SaveMessage(chat, conversationId, survivor.PlayerId, "message from a real player");

        var response = await SendDeleteAsync(client, account.AccountId, email);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var summary = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.That(summary.GetProperty("chatConversationsLeft").GetInt32(), Is.EqualTo(1));

        ChatConversationParticipant? removed = chat.GetParticipant(conversationId, account.PlayerId);
        ChatConversationParticipant? kept = chat.GetParticipant(conversationId, survivor.PlayerId);

        Assert.Multiple(() =>
        {
            // Removed as a participant...
            Assert.That(removed?.RemovedAtUtc, Is.Not.Null);
            // ...the other participant is completely untouched...
            Assert.That(kept, Is.Not.Null);
            Assert.That(kept!.RemovedAtUtc, Is.Null);
            // ...the conversation itself still exists...
            Assert.That(chat.GetConversation(conversationId), Is.Not.Null);
            // ...and BOTH messages are still readable, including the deleted account's own.
            // Deliberate: punching holes in a real player's history because a test account was
            // deleted would be a far worse side effect than a stale author id.
            Assert.That(chat.GetMessage(authored.MessageId), Is.Not.Null);
            Assert.That(chat.GetMessage(othersMessage.MessageId), Is.Not.Null);
            Assert.That(chat.ListMessages(conversationId, 0, 50), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task Delete_removes_the_players_personal_chat_records()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var chat = factory.Services.GetRequiredService<IChatRepository>();

        string email = Email("inbox-owner");
        AuthenticationAccount account = SeedAccount(factory, email);
        AuthenticationAccount survivor = SeedAccount(factory, Email("inbox-survivor"));

        Guid conversationId = Guid.NewGuid();
        chat.SaveConversation(
            new ChatConversation(conversationId, Guid.NewGuid(), Guid.NewGuid(), ChatChannelType.Alliance, $"aud-{conversationId:N}", "Room", survivor.PlayerId, DateTimeOffset.UtcNow, null, null, "standard", 1),
            new[] { Participant(conversationId, survivor.PlayerId), Participant(conversationId, account.PlayerId) });

        chat.SaveInbox(new ChatInboxEntry(account.PlayerId, conversationId, null, null, 0, 3, 0, false, false, DateTimeOffset.UtcNow));
        chat.SaveInbox(new ChatInboxEntry(survivor.PlayerId, conversationId, null, null, 0, 1, 0, false, false, DateTimeOffset.UtcNow));

        var response = await SendDeleteAsync(client, account.AccountId, email);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.Multiple(() =>
        {
            Assert.That(chat.GetInbox(account.PlayerId, conversationId), Is.Null);
            Assert.That(chat.GetInbox(survivor.PlayerId, conversationId), Is.Not.Null);
        });
    }

    [Test]
    public async Task Delete_removes_alliance_membership_for_an_ordinary_member()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var alliances = factory.Services.GetRequiredService<AllianceService>();

        AuthenticationAccount leader = SeedAccount(factory, Email("alliance-leader"));
        string email = Email("alliance-member");
        AuthenticationAccount member = SeedAccount(factory, email);

        AllianceEntity alliance = alliances.CreateAlliance(leader.PlayerId,
            new CreateAllianceRequest($"Hive {Guid.NewGuid():N}"[..20], "TST", "desc", "fr-CA", "", AllianceJoinMode.Open, $"create-{Guid.NewGuid():N}")).Alliance;
        alliances.JoinOpen(member.PlayerId, alliance.AllianceId);
        Assert.That(alliances.FindActiveMembership(member.PlayerId), Is.Not.Null);

        var response = await SendDeleteAsync(client, member.AccountId, email);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var summary = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(summary.GetProperty("allianceIdLeft").GetGuid(), Is.EqualTo(alliance.AllianceId.Value));
            Assert.That(alliances.FindActiveMembership(member.PlayerId), Is.Null);
            // The alliance and its leader survive untouched.
            Assert.That(alliances.FindAlliance(alliance.AllianceId), Is.Not.Null);
            Assert.That(alliances.FindActiveMembership(leader.PlayerId), Is.Not.Null);
        });
    }

    // Refusing here is deliberate: silently decapitating an alliance that real players belong to is
    // too large a side effect for deleting one account.
    [Test]
    public async Task Delete_refuses_an_alliance_leader_and_changes_nothing()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var alliances = factory.Services.GetRequiredService<AllianceService>();
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();

        string email = Email("doomed-leader");
        AuthenticationAccount leader = SeedAccount(factory, email);
        alliances.CreateAlliance(leader.PlayerId,
            new CreateAllianceRequest($"Lead {Guid.NewGuid():N}"[..20], "LED", "desc", "fr-CA", "", AllianceJoinMode.Open, $"create-{Guid.NewGuid():N}"));

        var response = await SendDeleteAsync(client, leader.AccountId, email);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(credentials.TryGetByEmail(email, out _), Is.True);
            Assert.That(alliances.FindActiveMembership(leader.PlayerId), Is.Not.Null);
        });
    }

    [Test]
    public async Task Delete_requires_the_typed_email_to_match_the_account_id()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();

        string email = Email("mismatch");
        AuthenticationAccount account = SeedAccount(factory, email);

        var wrongEmail = await SendDeleteAsync(client, account.AccountId, Email("someone-else"));
        var missingEmail = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/accounts/v1/admin/accounts/{account.AccountId:D}")
        {
            Content = JsonContent.Create(new { confirmEmail = "" })
        });

        Assert.Multiple(() =>
        {
            Assert.That(wrongEmail.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(missingEmail.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(credentials.TryGetByEmail(email, out _), Is.True);
        });
    }

    [Test]
    public async Task Delete_refuses_to_remove_an_admin_account()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsAdmin(factory, client);
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();

        string email = Email("other-admin");
        AuthenticationAccount other = SeedAccount(factory, email);
        credentials.Save(other with { Role = AccountRole.Admin });

        var response = await SendDeleteAsync(client, other.AccountId, email);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(credentials.TryGetByEmail(email, out _), Is.True);
        });
    }

    // ---------------- helpers ----------------

    private static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@bee.test";

    private static ChatConversationParticipant Participant(Guid conversationId, PlayerId playerId)
        => new(conversationId, playerId, ChatPermissionRole.Member, DateTimeOffset.UtcNow, null, true, true);

    private static ChatMessage SaveMessage(IChatRepository chat, Guid conversationId, PlayerId author, string body)
    {
        ChatConversation conversation = chat.GetConversation(conversationId)!;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return chat.SaveMessage(new ChatMessage(
            Guid.NewGuid(), conversationId, conversation.GameServerId, conversation.WorldId, conversation.ChannelType,
            author, "author", body,
            Array.Empty<ChatContentPart>(), Array.Empty<ChatMention>(), Array.Empty<ChatEmoji>(),
            null, now, now, chat.NextSequence(conversationId), $"req-{Guid.NewGuid():N}",
            ChatMessageState.Delivered, ChatModerationStatus.Clear, null, null, null, 1));
    }

    private static AuthenticationAccount SeedAccount(WebApplicationFactory<Program> factory, string email, string? displayName = null)
    {
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();
        AuthenticationAccount account = credentials.CreateEmailAccount(email, "secret");
        if (displayName is null) return account;
        AuthenticationAccount named = account with { DisplayName = displayName };
        credentials.Save(named);
        return named;
    }

    private static async Task<Guid> SeedHiveAsync(WebApplicationFactory<Program> factory, PlayerId playerId)
    {
        Guid hiveId = Guid.NewGuid();
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(playerId.Value, hiveId, state => state with { Revision = state.Revision + 1 });
        return hiveId;
    }

    private static async Task<HttpResponseMessage> SendDeleteAsync(HttpClient client, Guid accountId, string confirmEmail)
        => await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/accounts/v1/admin/accounts/{accountId:D}")
        {
            Content = JsonContent.Create(new { confirmEmail })
        });

    private static async Task AuthenticateAsAdmin(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var credentials = factory.Services.GetRequiredService<IAccountCredentialStore>();
        string adminEmail = Email("admin");
        string token = await LoginAsPlayer(factory, client, adminEmail);
        credentials.TryGetByEmail(adminEmail, out AuthenticationAccount admin);
        credentials.Save(admin with { Role = AccountRole.Admin });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
    });
}
