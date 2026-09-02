using System.Text.Json;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Tests;

// M043E-CL: exact wire-shape regression for GET /alliance/v1/membership/mine
// (MyAllianceOverviewResponse), triggered by a CEO Play Mode certification failure
// where the Alliance Center rendered a Ready/IN_ALLIANCE shell with empty/default
// data for an account that has NO active Alliance membership. This proves (or
// disproves) the JSON layer specifically - the server serialization side of the
// contract Unity's RemoteMyAllianceOverview must deserialize correctly.
public sealed class MyAllianceOverviewWireContractTests
{
    [Test]
    public void NoneResponse_SerializesWithHasAllianceFalseAndOmitsAllianceAndMembershipKeysEntirely()
    {
        string json = JsonSerializer.Serialize(MyAllianceOverviewResponse.None, BeeJson.CreateDefaultOptions());

        // DefaultIgnoreCondition=WhenWritingNull means a null Alliance/Membership must
        // never appear as an explicit "alliance":null / "membership":null key - Unity's
        // codec (System.Text.Json) leaves an ABSENT property at its C# default (null for
        // a reference type), which is exactly what the controller's NoAlliance check
        // relies on. If this ever regresses to writing null keys explicitly, or - far
        // worse - to writing a real (even if empty/default) nested object, this test
        // must fail.
        Assert.That(json, Is.EqualTo("{\"hasAlliance\":false}"));
    }

    [Test]
    public void PresentResponse_SerializesRealNestedAllianceAndMembershipWithCamelCaseKeys()
    {
        var alliance = new AllianceEntity
        {
            AllianceId = new AllianceId(System.Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            Name = "Golden Hive",
            Tag = "GLD",
            JoinMode = AllianceJoinMode.Open,
            Status = AllianceStatus.Active,
            CreatedAtUtc = System.DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            CreatedByPlayerId = new PlayerId(System.Guid.Parse("11111111-1111-1111-1111-111111111111")),
            LeaderPlayerId = new PlayerId(System.Guid.Parse("11111111-1111-1111-1111-111111111111")),
            MemberCount = 1,
            MaxMembers = 100,
            Revision = 1
        };
        var membership = new AllianceMembership
        {
            AllianceId = alliance.AllianceId,
            PlayerId = alliance.LeaderPlayerId,
            Role = AllianceRole.Leader,
            JoinedAtUtc = alliance.CreatedAtUtc,
            Revision = 1
        };
        var response = new MyAllianceOverviewResponse(true, alliance, membership);

        string json = JsonSerializer.Serialize(response, BeeJson.CreateDefaultOptions());

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("hasAlliance").GetBoolean(), Is.True);
            Assert.That(document.RootElement.TryGetProperty("alliance", out JsonElement allianceElement), Is.True);
            Assert.That(allianceElement.GetProperty("name").GetString(), Is.EqualTo("Golden Hive"));
            Assert.That(document.RootElement.TryGetProperty("membership", out JsonElement membershipElement), Is.True);
            Assert.That(membershipElement.GetProperty("role").ToString(), Does.Contain("Leader").Or.EqualTo("2"));
        });
    }

    [Test]
    public void RoundTrip_DeserializingNoneResponseBackIntoTheServerRecordType_YieldsFalseAndNullsNeverDefaultObjects()
    {
        // Sanity check on the server's own record type (not Unity's DTO, which lives in a
        // different assembly this test project cannot reference - see AllianceClientTests.cs
        // for the Unity-side equivalent) - proves a round trip never silently produces a
        // non-null-but-empty Alliance/Membership, which is the exact failure mode a
        // System.Text.Json wrapper-shape bug (the M043 class of bug) would produce.
        string json = JsonSerializer.Serialize(MyAllianceOverviewResponse.None, BeeJson.CreateDefaultOptions());
        MyAllianceOverviewResponse? roundTripped = JsonSerializer.Deserialize<MyAllianceOverviewResponse>(json, BeeJson.CreateDefaultOptions());

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.HasAlliance, Is.False);
        Assert.That(roundTripped.Alliance, Is.Null);
        Assert.That(roundTripped.Membership, Is.Null);
    }
}
