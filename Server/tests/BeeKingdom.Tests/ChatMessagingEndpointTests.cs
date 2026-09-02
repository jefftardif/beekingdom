using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class ChatMessagingEndpointTests
{
    private static readonly string[] InvalidOpaquePathSegments =
    [
        "%2F",
        "%3F",
        "%23",
        "%252F",
        "%E8%9C%82",
        new string('a', 256),
        new string('a', 257),
        "%20abc",
        "abc%20"
    ];

    [Test]
    public async Task ChatReadinessAndCapabilitiesStayNonLiveByDefault()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage readinessResponse = await client.GetAsync("/runtime/chat-readiness");
        HttpResponseMessage capabilitiesResponse = await client.GetAsync("/chat/v1/capabilities");

        Assert.Multiple(() =>
        {
            Assert.That(readinessResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(capabilitiesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(capabilitiesResponse.Headers.CacheControl?.NoStore, Is.True);
            Assert.That(capabilitiesResponse.Headers.CacheControl?.NoCache, Is.True);
            Assert.That(capabilitiesResponse.Headers.CacheControl?.MaxAge, Is.EqualTo(TimeSpan.Zero));
            Assert.That(capabilitiesResponse.Headers.CacheControl?.MustRevalidate, Is.True);
            Assert.That(capabilitiesResponse.Headers.Pragma.Any(value => value.Name == "no-cache"), Is.True);
            Assert.That(capabilitiesResponse.Headers.Vary, Does.Contain("Accept-Encoding"));
            Assert.That(capabilitiesResponse.Headers.Age, Is.Null);
        });

        using JsonDocument readiness = JsonDocument.Parse(await readinessResponse.Content.ReadAsStringAsync());
        using JsonDocument capabilities = JsonDocument.Parse(await capabilitiesResponse.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(readiness.RootElement.GetProperty("status").GetString(), Is.EqualTo("PreparationOnly"));
            Assert.That(readiness.RootElement.GetProperty("enabled").GetBoolean(), Is.False);
            Assert.That(readiness.RootElement.GetProperty("liveDeploymentAllowed").GetBoolean(), Is.False);
            Assert.That(capabilities.RootElement.GetProperty("server").GetBoolean(), Is.False);
            Assert.That(capabilities.RootElement.GetProperty("officialGain").GetBoolean(), Is.False);
            Assert.That(capabilities.RootElement.GetProperty("channels").GetArrayLength(), Is.EqualTo(4));
            Assert.That(capabilities.RootElement.GetProperty("idempotencyReceiptRetentionDays").GetInt32(), Is.EqualTo(30));
            Assert.That(capabilities.RootElement.GetProperty("translationAvailable").GetBoolean(), Is.False);
            Assert.That(capabilities.RootElement.GetProperty("translationModelVersion").GetString(), Is.EqualTo("translation-disabled-v1"));
        });
    }

    [Test]
    public async Task ChatMutationEndpointIsGatedWhenDisabled()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: false);
        using HttpClient client = factory.CreateClient();
        string accessToken = await LoginTestAccount(factory, client, "disabled-chat@bee.test");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/chat/v1/conversations",
            new
            {
                channelType = ChatChannelType.Server,
                gameServerId = Guid.NewGuid(),
                worldId = Guid.NewGuid(),
                audienceKey = (string?)null,
                title = "Global",
                participantIds = Array.Empty<Guid>(),
                clientRequestId = "create_disabled_global"
            },
            BeeJson.CreateDefaultOptions());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [TestCaseSource(nameof(InvalidOpaquePathSegments))]
    public async Task OpaqueChatPathSegmentIsDecodedOnceBoundedAndRejectedSafely(string segment)
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        string accessToken = await LoginTestAccount(factory, client, $"opaque-{Guid.NewGuid():N}@bee.test");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync($"/chat/v1/conversations/{segment}/messages?afterSequence=0&limit=1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using JsonDocument error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(error.RootElement.GetProperty("code").GetString(), Is.EqualTo("chat.invalid_request"));
        Assert.That(response.Headers.Location, Is.Null);
    }

    [Test]
    public async Task EveryChatRestRouteRespondsDirectlyWithoutRedirect()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid conversationId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        JsonSerializerOptions json = BeeJson.CreateDefaultOptions();

        HttpResponseMessage[] responses =
        [
            await client.GetAsync("/chat/v1/capabilities"),
            await client.GetAsync("/chat/v1/conversations?limit=1"),
            await client.GetAsync($"/chat/v1/conversations/{conversationId}/messages?afterSequence=0&limit=1"),
            await client.PostAsJsonAsync($"/chat/v1/conversations/{conversationId}/messages", new { clientRequestId = "redirect_test", body = "x", clientCreatedAt = DateTimeOffset.UtcNow }, json),
            await client.PostAsJsonAsync($"/chat/v1/conversations/{conversationId}/read", new { sequence = 0 }, json),
            await client.PostAsJsonAsync($"/chat/v1/messages/{messageId}/report", new { clientRequestId = "redirect_report", category = "spam" }, json),
            await client.PostAsJsonAsync($"/chat/v1/messages/{messageId}/translations", new { targetLocale = "en-CA" }, json)
        ];

        Assert.That(responses[0].StatusCode, Is.EqualTo(HttpStatusCode.OK));
        foreach (HttpResponseMessage response in responses.Skip(1))
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        Assert.Multiple(() =>
        {
            foreach (HttpResponseMessage response in responses)
            {
                Assert.That((int)response.StatusCode, Is.Not.InRange(300, 399));
                Assert.That(response.Headers.Location, Is.Null);
            }
        });
    }

    [Test]
    public async Task InvalidBearerSyntaxAlwaysReturnsStructuredSessionRequired()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        string[] invalidTokens = ["abc*", "ab=c", " abc", "abc ", new string('A', 8193)];

        foreach (string token in invalidTokens)
        {
            using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);
            HttpResponseMessage response = await client.GetAsync("/chat/v1/conversations?limit=1");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), token.Length > 32 ? "oversized" : token);
            using JsonDocument error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.That(error.RootElement.GetProperty("code").GetString(), Is.EqualTo("chat.session_required"));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain(token.Length > 32 ? token[..32] : token));
        }
    }

    [Test]
    public async Task ChatResponsesRemainJsonBoundedAndNeverExposeHtmlIntermediaryPage()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: false);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        HttpResponseMessage capabilities = await client.GetAsync("/chat/v1/capabilities");
        HttpResponseMessage unauthorized = await client.GetAsync("/chat/v1/conversations?limit=100");

        foreach (HttpResponseMessage response in new[] { capabilities, unauthorized })
        {
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(BeeKingdom.Chat.Diagnostics.ChatResponseBudget.DefaultBytes));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            Assert.That(Encoding.UTF8.GetString(bytes).ToLowerInvariant(), Does.Not.Contain("<html"));
            Assert.That(response.Headers.Location, Is.Null);
        }
    }

    [Test]
    public async Task ChatRequestBodyLimitAcceptsExactBytesAndRejectsTheNextByte()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");

        string exactJson = JsonSerializer.Serialize(new { channelType = "Server", gameServerId = Guid.Empty, worldId = Guid.Empty, audienceKey = (string?)null, title = "x", participantIds = Array.Empty<Guid>(), clientRequestId = "body-limit" });
        byte[] exactPayload = Encoding.UTF8.GetBytes(exactJson.PadRight(65_536, ' '));
        using ByteArrayContent exactContent = new(exactPayload);
        exactContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        HttpResponseMessage exact = await client.PostAsync("/chat/v1/conversations", exactContent);

        using ByteArrayContent overContent = new(new byte[65_537]);
        overContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        HttpResponseMessage over = await client.PostAsync("/chat/v1/conversations", overContent);

        using StringContent unicode = new(JsonSerializer.Serialize(new string('蜂', 1000)), Encoding.UTF8, "application/json");
        HttpResponseMessage unicodeResponse = await client.PostAsync("/chat/v1/conversations", unicode);

        Assert.Multiple(() =>
        {
            Assert.That(exact.StatusCode, Is.Not.EqualTo(HttpStatusCode.RequestEntityTooLarge));
            Assert.That(over.StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge));
            Assert.That(unicodeResponse.StatusCode, Is.Not.EqualTo(HttpStatusCode.RequestEntityTooLarge));
        });
    }

    [Test]
    public async Task ChatRequestTargetIsUtf8BoundedBeforeRouteOrAuthentication()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: false);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        HttpResponseMessage within = await client.GetAsync("/chat/v1/capabilities?x=" + new string('a', 7_500));
        HttpResponseMessage over = await client.GetAsync("/chat/v1/capabilities?x=" + new string('a', 8_200));

        Assert.That((int)within.StatusCode, Is.Not.EqualTo(414));
        Assert.That(over.StatusCode, Is.EqualTo(HttpStatusCode.RequestUriTooLong));
        Assert.That(over.Headers.Location, Is.Null);
    }

    [Test]
    public async Task ChatPrivateConversationSendReadAndIdempotenceWorkWhenEnabled()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient client = factory.CreateClient();
        string accessToken = await LoginTestAccount(factory, client, "chat-queen@bee.test");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        Guid otherPlayerId = Guid.NewGuid();
        Guid gameServerId = Guid.NewGuid();
        Guid worldId = Guid.NewGuid();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/chat/v1/conversations",
            new
            {
                channelType = ChatChannelType.Private,
                gameServerId,
                worldId,
                audienceKey = (string?)null,
                title = "Queen, Scout",
                participantIds = new[] { otherPlayerId },
                clientRequestId = "create_private_001"
            },
            BeeJson.CreateDefaultOptions());

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Guid conversationId = created.RootElement.GetProperty("conversation").GetProperty("conversationId").GetGuid();
        Assert.That(created.RootElement.GetProperty("clientRequestId").GetString(), Is.EqualTo("create_private_001"));
        Assert.That(created.RootElement.GetProperty("inbox").GetProperty("conversationId").GetGuid(), Is.EqualTo(conversationId));

        object sendBody = new
        {
            clientRequestId = "send_private_001",
            body = "Rendez-vous a la porte nord !",
            contentParts = new[] { new { kind = "text", text = "Rendez-vous a la porte nord !" } },
            mentions = Array.Empty<object>(),
            emoji = Array.Empty<object>(),
            replyToMessageId = (Guid?)null,
            clientCreatedAt = DateTimeOffset.UtcNow
        };

        HttpResponseMessage sendResponse = await client.PostAsJsonAsync($"/chat/v1/conversations/{conversationId}/messages", sendBody, BeeJson.CreateDefaultOptions());
        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync($"/chat/v1/conversations/{conversationId}/messages", sendBody, BeeJson.CreateDefaultOptions());
        HttpResponseMessage messagesResponse = await client.GetAsync($"/chat/v1/conversations/{conversationId}/messages?afterSequence=0&limit=10");
        HttpResponseMessage readResponse = await client.PostAsJsonAsync($"/chat/v1/conversations/{conversationId}/read", new { sequence = 1 }, BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sendResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(duplicateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(messagesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(readResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        using JsonDocument sent = JsonDocument.Parse(await sendResponse.Content.ReadAsStringAsync());
        using JsonDocument duplicate = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());
        using JsonDocument messages = JsonDocument.Parse(await messagesResponse.Content.ReadAsStringAsync());
        using JsonDocument readReceipt = JsonDocument.Parse(await readResponse.Content.ReadAsStringAsync());
        Guid messageId = sent.RootElement.GetProperty("message").GetProperty("messageId").GetGuid();
        HttpResponseMessage translationUnavailable = await client.PostAsJsonAsync($"/chat/v1/messages/{messageId}/translations", new { messageId, targetLocale = "en-CA", modelVersion = "translation-disabled-v1" }, BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sent.RootElement.GetProperty("deduplicated").GetBoolean(), Is.False);
            Assert.That(sent.RootElement.GetProperty("serverSequence").GetInt64(), Is.EqualTo(1));
            Assert.That(duplicate.RootElement.GetProperty("deduplicated").GetBoolean(), Is.True);
            Assert.That(messages.RootElement.GetProperty("items").GetArrayLength(), Is.EqualTo(1));
            Assert.That(messages.RootElement.GetProperty("items")[0].GetProperty("body").GetString(), Is.EqualTo("Rendez-vous a la porte nord !"));
            Assert.That(readReceipt.RootElement.GetProperty("conversationId").GetGuid(), Is.EqualTo(conversationId));
            Assert.That(readReceipt.RootElement.GetProperty("readCursorSequence").GetInt64(), Is.GreaterThanOrEqualTo(1));
            Assert.That(readReceipt.RootElement.GetProperty("unreadCount").GetInt32(), Is.GreaterThanOrEqualTo(0));
            Assert.That(readReceipt.RootElement.GetProperty("mentionCount").GetInt32(), Is.GreaterThanOrEqualTo(0));
            Assert.That(translationUnavailable.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(translationUnavailable.Headers.RetryAfter?.Delta, Is.EqualTo(TimeSpan.FromSeconds(30)));
        });

        HttpResponseMessage reportResponse = await client.PostAsJsonAsync($"/chat/v1/messages/{messageId}/report", new { clientRequestId = "report-correlated", category = "spam" }, BeeJson.CreateDefaultOptions());
        using JsonDocument report = JsonDocument.Parse(await reportResponse.Content.ReadAsStringAsync());
        Assert.Multiple(() =>
        {
            Assert.That(reportResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(report.RootElement.GetProperty("reportId").GetGuid(), Is.Not.EqualTo(Guid.Empty));
            Assert.That(report.RootElement.GetProperty("messageId").GetGuid(), Is.EqualTo(messageId));
            Assert.That(report.RootElement.GetProperty("clientRequestId").GetString(), Is.EqualTo("report-correlated"));
            Assert.That(report.RootElement.GetProperty("status").GetString(), Is.Not.Empty);
        });
    }

    [Test]
    public async Task ServerChannelIsSharedAcrossPlayersOnTheSameGameServerAndWorld()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient firstClient = factory.CreateClient();
        using HttpClient secondClient = factory.CreateClient();
        string firstToken = await LoginTestAccount(factory, firstClient, "chat-server-first@bee.test");
        string secondToken = await LoginTestAccount(factory, secondClient, "chat-server-second@bee.test");
        firstClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firstToken);
        secondClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secondToken);

        Guid gameServerId = Guid.NewGuid();
        Guid worldId = Guid.NewGuid();
        object CreateBody(string clientRequestId) => new
        {
            channelType = ChatChannelType.Server,
            gameServerId,
            worldId,
            audienceKey = (string?)null,
            title = "Global",
            participantIds = Array.Empty<Guid>(),
            clientRequestId
        };

        HttpResponseMessage firstCreate = await firstClient.PostAsJsonAsync("/chat/v1/conversations", CreateBody("server_shared_first"), BeeJson.CreateDefaultOptions());
        HttpResponseMessage secondCreate = await secondClient.PostAsJsonAsync("/chat/v1/conversations", CreateBody("server_shared_second"), BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(firstCreate.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(secondCreate.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        using JsonDocument firstCreated = JsonDocument.Parse(await firstCreate.Content.ReadAsStringAsync());
        using JsonDocument secondCreated = JsonDocument.Parse(await secondCreate.Content.ReadAsStringAsync());
        Guid firstConversationId = firstCreated.RootElement.GetProperty("conversation").GetProperty("conversationId").GetGuid();
        Guid secondConversationId = secondCreated.RootElement.GetProperty("conversation").GetProperty("conversationId").GetGuid();
        Assert.That(secondConversationId, Is.EqualTo(firstConversationId), "Two players scoped to the same game server/world must share one Server conversation.");

        object SendBody(string clientRequestId, string body) => new
        {
            clientRequestId,
            body,
            contentParts = new[] { new { kind = "text", text = body } },
            mentions = Array.Empty<object>(),
            emoji = Array.Empty<object>(),
            replyToMessageId = (Guid?)null,
            clientCreatedAt = DateTimeOffset.UtcNow
        };

        HttpResponseMessage sendFromFirst = await firstClient.PostAsJsonAsync($"/chat/v1/conversations/{firstConversationId}/messages", SendBody("server_shared_message_1", "Salutations a la ruche !"), BeeJson.CreateDefaultOptions());
        HttpResponseMessage sendFromSecond = await secondClient.PostAsJsonAsync($"/chat/v1/conversations/{secondConversationId}/messages", SendBody("server_shared_message_2", "Bonjour a toutes !"), BeeJson.CreateDefaultOptions());
        HttpResponseMessage messagesForSecond = await secondClient.GetAsync($"/chat/v1/conversations/{secondConversationId}/messages?afterSequence=0&limit=10");

        Assert.Multiple(() =>
        {
            Assert.That(sendFromFirst.StatusCode, Is.EqualTo(HttpStatusCode.OK), "First (creating) player must be able to write.");
            Assert.That(sendFromSecond.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Second (joining) player must also be able to write, not just read.");
            Assert.That(messagesForSecond.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        using JsonDocument messages = JsonDocument.Parse(await messagesForSecond.Content.ReadAsStringAsync());
        Assert.That(messages.RootElement.GetProperty("items").GetArrayLength(), Is.EqualTo(2));
    }

    // M042-CL: alliance/leaders chat access is now server-authoritative (see
    // LocalChatAudienceResolver/IAllianceMembershipResolver) - a client-declared
    // "requesterAllianceRole" no longer has any effect on Alliance/Leaders channels. This test
    // now creates a REAL alliance through the real /alliance/v1 endpoints (Alliance:Enabled is
    // true in the base config CreateFactory inherits) and uses real Leader/Member accounts
    // instead of a role string, proving the actual end-to-end wiring rather than a since-removed
    // trust mechanism.
    [Test]
    public async Task AllianceAndLeadersChannelsRequireAllianceRoles()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient leaderClient = factory.CreateClient();
        string leaderToken = await LoginTestAccount(factory, leaderClient, "chat-leader-gates@bee.test");
        leaderClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", leaderToken);

        HttpResponseMessage createAlliance = await leaderClient.PostAsJsonAsync(
            "/alliance/v1/alliances",
            new { name = "Golden Hive Gate Test", tag = "GHG", description = "", language = "fr-CA", emblemKey = "", joinMode = 0, clientRequestId = "chat-gate-alliance" },
            BeeJson.CreateDefaultOptions());
        Assert.That(createAlliance.StatusCode, Is.EqualTo(HttpStatusCode.OK), await createAlliance.Content.ReadAsStringAsync());
        using JsonDocument allianceDoc = JsonDocument.Parse(await createAlliance.Content.ReadAsStringAsync());
        Guid allianceId = allianceDoc.RootElement.GetProperty("alliance").GetProperty("allianceId").GetProperty("value").GetGuid();

        using HttpClient memberClient = factory.CreateClient();
        string memberToken = await LoginTestAccount(factory, memberClient, "chat-member-gates@bee.test");
        memberClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", memberToken);
        HttpResponseMessage joinResponse = await memberClient.PostAsync($"/alliance/v1/alliances/{allianceId:D}/join", null);
        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await joinResponse.Content.ReadAsStringAsync());

        Guid gameServerId = Guid.NewGuid();
        Guid worldId = Guid.NewGuid();
        Guid unrelatedAllianceId = Guid.NewGuid(); // a real Guid, but no alliance/membership exists for it

        object AllianceBody(string clientRequestId, Guid forAllianceId) => new
        {
            channelType = ChatChannelType.Alliance,
            gameServerId,
            worldId,
            audienceKey = $"alliance:{forAllianceId:N}",
            title = "Alliance",
            participantIds = Array.Empty<Guid>(),
            clientRequestId
        };
        object LeadersBody(string clientRequestId) => new
        {
            channelType = ChatChannelType.Leaders,
            gameServerId,
            worldId,
            audienceKey = $"leaders:{allianceId:N}",
            title = "Leaders",
            participantIds = Array.Empty<Guid>(),
            clientRequestId
        };

        HttpResponseMessage allianceWithoutMembership = await leaderClient.PostAsJsonAsync("/chat/v1/conversations", AllianceBody("alliance_no_role", unrelatedAllianceId), BeeJson.CreateDefaultOptions());
        HttpResponseMessage allianceWithRealMember = await memberClient.PostAsJsonAsync("/chat/v1/conversations", AllianceBody("alliance_member", allianceId), BeeJson.CreateDefaultOptions());
        HttpResponseMessage leadersWithRealMember = await memberClient.PostAsJsonAsync("/chat/v1/conversations", LeadersBody("leaders_member"), BeeJson.CreateDefaultOptions());
        HttpResponseMessage leadersWithRealLeader = await leaderClient.PostAsJsonAsync("/chat/v1/conversations", LeadersBody("leaders_leader"), BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(allianceWithoutMembership.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(allianceWithRealMember.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(leadersWithRealMember.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(leadersWithRealLeader.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    // M042-CL: announcement access is now gated by real server-side Alliance membership
    // (IAllianceMembershipResolver), not the client-declared "requesterAllianceRole" the endpoint
    // used to trust - seeds a REAL alliance/leader/member through the live /alliance/v1 endpoints
    // instead, same approach as AllianceAndLeadersChannelsRequireAllianceRoles above.
    [Test]
    public async Task AllianceAnnouncementRequiresLeaderRoleAndFanOutParticipants()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient leaderClient = factory.CreateClient();
        string leaderToken = await LoginTestAccount(factory, leaderClient, "chat-announcement-leader@bee.test");
        leaderClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", leaderToken);

        HttpResponseMessage createAlliance = await leaderClient.PostAsJsonAsync(
            "/alliance/v1/alliances",
            new { name = "Golden Hive Announce Test", tag = "GHA", description = "", language = "fr-CA", emblemKey = "", joinMode = 0, clientRequestId = "chat-gate-announce-alliance" },
            BeeJson.CreateDefaultOptions());
        Assert.That(createAlliance.StatusCode, Is.EqualTo(HttpStatusCode.OK), await createAlliance.Content.ReadAsStringAsync());
        using JsonDocument allianceDoc = JsonDocument.Parse(await createAlliance.Content.ReadAsStringAsync());
        Guid allianceId = allianceDoc.RootElement.GetProperty("alliance").GetProperty("allianceId").GetProperty("value").GetGuid();

        using HttpClient memberClient = factory.CreateClient();
        string memberToken = await LoginTestAccount(factory, memberClient, "chat-announcement-member@bee.test");
        memberClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", memberToken);
        HttpResponseMessage joinResponse = await memberClient.PostAsync($"/alliance/v1/alliances/{allianceId:D}/join", null);
        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await joinResponse.Content.ReadAsStringAsync());

        object body = new
        {
            gameServerId = Guid.NewGuid(),
            worldId = Guid.NewGuid(),
            body = "Defense au centre ce soir.",
            memberPlayerIds = new[] { Guid.NewGuid(), Guid.NewGuid() },
            clientRequestId = "announcement_001"
        };
        object memberBody = new
        {
            gameServerId = Guid.NewGuid(),
            worldId = Guid.NewGuid(),
            body = "Defense au centre ce soir.",
            memberPlayerIds = new[] { Guid.NewGuid() },
            clientRequestId = "announcement_forbidden"
        };

        HttpResponseMessage forbidden = await memberClient.PostAsJsonAsync($"/chat/v1/alliances/{allianceId}/announcements", memberBody, BeeJson.CreateDefaultOptions());
        HttpResponseMessage accepted = await leaderClient.PostAsJsonAsync($"/chat/v1/alliances/{allianceId}/announcements", body, BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(forbidden.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        using JsonDocument payload = JsonDocument.Parse(await accepted.Content.ReadAsStringAsync());
        Assert.Multiple(() =>
        {
            Assert.That(payload.RootElement.GetProperty("conversation").GetProperty("channelType").GetString(), Is.EqualTo("Alliance"));
            Assert.That(payload.RootElement.GetProperty("sendResult").GetProperty("message").GetProperty("body").GetString(), Is.EqualTo("Defense au centre ce soir."));
        });
    }

    [Test]
    public async Task DIAGNOSTIC_ListConversationsAfterOtherParticipantNeverExisted()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(chatEnabled: true);
        using HttpClient client = factory.CreateClient();
        string accessToken = await LoginTestAccount(factory, client, "diag-queen@bee.test");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        Guid otherPlayerId = Guid.NewGuid();
        Guid gameServerId = Guid.NewGuid();
        Guid worldId = Guid.NewGuid();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/chat/v1/conversations",
            new
            {
                channelType = ChatChannelType.Private,
                gameServerId,
                worldId,
                audienceKey = (string?)null,
                title = "Queen, Ghost",
                participantIds = new[] { otherPlayerId },
                clientRequestId = "diag_create_001"
            },
            BeeJson.CreateDefaultOptions());
        Console.WriteLine("CREATE status: " + createResponse.StatusCode);
        Console.WriteLine("CREATE body: " + await createResponse.Content.ReadAsStringAsync());

        HttpResponseMessage listResponse = await client.GetAsync("/chat/v1/conversations");
        Console.WriteLine("LIST status: " + listResponse.StatusCode);
        Console.WriteLine("LIST body: " + await listResponse.Content.ReadAsStringAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(bool chatEnabled)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Chat:Enabled"] = chatEnabled.ToString(),
                        ["Chat:RealtimeEnabled"] = "false"
                    });
                });
            });
    }

    private static async Task<string> LoginTestAccount(WebApplicationFactory<Program> factory, HttpClient client, string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                email,
                password = "secret",
                clientVersion = "1.0.0",
                ipAddress = "127.0.0.1",
                deviceIdentifier = "chat-tests",
                region = "local"
            },
            BeeJson.CreateDefaultOptions());

        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return login.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login did not return an access token.");
    }
}
