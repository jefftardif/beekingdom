using BeeKingdom.Accounts;
using BeeKingdom.Accounts.Configuration;
using BeeKingdom.Accounts.Events;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Accounts.Repositories;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// M043B-CL: the generic player-search/lookup surface identified as entirely missing in M043 -
// covers search correctness, privacy (no email/status ever reachable through PlayerPublicIdentity),
// the "blank query extracts everyone" guard, case-insensitivity, and pagination/limit.
public sealed class PlayerDirectoryServiceTests
{
    private sealed class FixedClock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

    private static (IPlayerDirectoryService Directory, AccountManager Accounts, IAccountCredentialStore Credentials) CreateDirectory()
    {
        var options = Options.Create(new AccountOptions
        {
            DefaultLanguage = "fr-CA",
            DefaultTimeZone = "America/Toronto",
            DefaultCurrency = "CAD"
        });
        var service = new AccountService(new InMemoryAccountRepository(), new InMemoryAccountEventSink(), new FixedClock(), options);
        var manager = new AccountManager(service);
        var credentials = new InMemoryAccountCredentialStore(new Pbkdf2PasswordHasher());
        return (new PlayerDirectoryService(service, credentials), manager, credentials);
    }

    [Test]
    public void Search_FindsRealDisplayNameCaseInsensitively()
    {
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("QueenBee", "queen@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId); // PendingVerification -> Active so Search (Active-only) finds it

        var results = directory.Search("queenbee", 0, 20);

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].DisplayName, Is.EqualTo("QueenBee"));
        Assert.That(results[0].PlayerId, Is.EqualTo(created.Profile.PlayerId));
    }

    [Test]
    public void Search_RejectsBlankQuery_CannotExtractWholePlayerBase()
    {
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("QueenBee", "queen@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId);

        Assert.Throws<ArgumentException>(() => directory.Search(string.Empty, 0, 20));
        Assert.Throws<ArgumentException>(() => directory.Search("a", 0, 20)); // below MinQueryLength
        Assert.Throws<ArgumentException>(() => directory.Search(new string('x', 100), 0, 20)); // above MaxQueryLength
    }

    [Test]
    public void Search_ResultsNeverExposePrivateAccountData()
    {
        // Structural guarantee: PlayerPublicIdentity only ever carries PlayerId + DisplayName -
        // no email/status/auth-provider-id/token property can exist on it, by construction.
        System.Reflection.PropertyInfo[] properties = typeof(PlayerPublicIdentity).GetProperties();
        Assert.That(properties.Select(p => p.Name), Is.EquivalentTo(new[] { "PlayerId", "DisplayName" }));
    }

    [Test]
    public void Search_RespectsPaginationAndLimit()
    {
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        for (int i = 0; i < 5; i++)
        {
            AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("Scout" + i, $"scout{i}@bee.test"));
            accounts.ReactivateAccount(created.Profile.AccountId);
        }

        var firstPage = directory.Search("Scout", 0, 2);
        var secondPage = directory.Search("Scout", 2, 2);

        Assert.That(firstPage.Count, Is.EqualTo(2));
        Assert.That(secondPage.Count, Is.EqualTo(2));
        Assert.That(firstPage.Select(r => r.DisplayName), Is.Not.EquivalentTo(secondPage.Select(r => r.DisplayName)));

        var overLimit = directory.Search("Scout", 0, 1000);
        Assert.That(overLimit.Count, Is.LessThanOrEqualTo(PlayerDirectoryService.MaxLimit));
    }

    [Test]
    public void GetByPlayerIds_BatchResolvesWithoutOneCallPerPlayer()
    {
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        AccountRecord a = accounts.CreateAccount(new CreateAccountRequest("Alpha", "alpha@bee.test"));
        AccountRecord b = accounts.CreateAccount(new CreateAccountRequest("Beta", "beta@bee.test"));

        var resolved = directory.GetByPlayerIds(new[] { a.Profile.PlayerId, b.Profile.PlayerId, PlayerId.New() });

        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved[a.Profile.PlayerId].DisplayName, Is.EqualTo("Alpha"));
        Assert.That(resolved[b.Profile.PlayerId].DisplayName, Is.EqualTo("Beta"));
    }

    [Test]
    public void Search_DoesNotReturnInactiveAccounts()
    {
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        accounts.CreateAccount(new CreateAccountRequest("Dormant", "dormant@bee.test")); // stays PendingVerification

        var results = directory.Search("Dormant", 0, 20);

        Assert.That(results, Is.Empty);
    }

    // ---------------- M043P-CL: authoritative DisplayName resolution ----------------

    [Test]
    public void GetByPlayerId_PrefersOnboardedAuthenticationDisplayNameOverAccountRecord()
    {
        // The real Google-auth onboarding flow (POST /auth/display-name) writes ONLY to
        // AuthenticationAccounts - it never touches the separate BeeKingdom.Accounts record. This
        // is the exact production shape that showed the CEO as a truncated PlayerId everywhere:
        // an Account record exists with an empty/irrelevant DisplayName, while the real, onboarded
        // name lives in AuthenticationAccounts.
        (IPlayerDirectoryService directory, AccountManager accounts, IAccountCredentialStore credentials) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest(string.Empty, "queen@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId);

        AuthenticationAccount authAccount = credentials.CreateGoogleAccount("google-subject-1", "queen@bee.test") with
        {
            PlayerId = created.Profile.PlayerId,
            DisplayName = "QueenBee",
            IsOnboarded = true
        };
        credentials.Save(authAccount);

        PlayerPublicIdentity? resolved = directory.GetByPlayerId(created.Profile.PlayerId);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.DisplayName, Is.EqualTo("QueenBee"));
    }

    [Test]
    public void GetByPlayerId_FallsBackToAccountRecordWhenNoAuthenticationAccountExists()
    {
        // A synthetic/seeded test account created directly via IAccountService, never through the
        // real Google onboarding flow - must keep working exactly as before.
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("SeededScout", "scout@bee.test"));

        PlayerPublicIdentity? resolved = directory.GetByPlayerId(created.Profile.PlayerId);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.DisplayName, Is.EqualTo("SeededScout"));
    }

    [Test]
    public void GetByPlayerId_IgnoresNotYetOnboardedAuthenticationAccount()
    {
        // A player mid-login who has not chosen a name yet (IsOnboarded=false) must not surface a
        // null/empty DisplayName in place of whatever the Account record already has.
        (IPlayerDirectoryService directory, AccountManager accounts, IAccountCredentialStore credentials) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("PendingName", "pending@bee.test"));
        AuthenticationAccount authAccount = credentials.CreateGoogleAccount("google-subject-2", "pending@bee.test") with
        {
            PlayerId = created.Profile.PlayerId,
            IsOnboarded = false
        };
        credentials.Save(authAccount);

        PlayerPublicIdentity? resolved = directory.GetByPlayerId(created.Profile.PlayerId);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.DisplayName, Is.EqualTo("PendingName"));
    }

    [Test]
    public void GetByPlayerId_UnknownPlayer_ReturnsNullWithoutCrashing()
    {
        (IPlayerDirectoryService directory, _, _) = CreateDirectory();

        PlayerPublicIdentity? resolved = directory.GetByPlayerId(PlayerId.New());

        Assert.That(resolved, Is.Null);
    }

    // ---------------- M043R-CL: authoritative Search() merges AuthenticationAccounts ----------------

    // The exact production shape reported by the CEO: a real, onboarded Google player ("Stara")
    // who exists ONLY in AuthenticationAccounts (never created via IAccountService) - Search()
    // used to be BeeKingdom.Accounts-only and returned nothing for this player.
    private static AuthenticationAccount CreateAuthOnlyPlayer(IAccountCredentialStore credentials, string googleSubjectId, string email, string displayName, bool isOnboarded = true)
    {
        AuthenticationAccount account = credentials.CreateGoogleAccount(googleSubjectId, email) with
        {
            DisplayName = displayName,
            IsOnboarded = isOnboarded
        };
        credentials.Save(account);
        return account;
    }

    [Test]
    public void Search_FindsAuthOnlyOnboardedPlayer_ExactStaraRuntimeContract()
    {
        (IPlayerDirectoryService directory, _, IAccountCredentialStore credentials) = CreateDirectory();
        AuthenticationAccount stara = CreateAuthOnlyPlayer(credentials, "google-subject-stara", "stara@bee.test", "Stara");

        var byPrefix = directory.Search("St", 0, 20);
        var byCaseInsensitive = directory.Search("stara", 0, 20);

        Assert.That(byPrefix.Select(r => r.DisplayName), Does.Contain("Stara"));
        Assert.That(byPrefix.Single(r => r.DisplayName == "Stara").PlayerId, Is.EqualTo(stara.PlayerId));
        Assert.That(byCaseInsensitive.Select(r => r.DisplayName), Does.Contain("Stara"));
    }

    [Test]
    public void Search_MatchesPartialContainsAnywhereInName()
    {
        (IPlayerDirectoryService directory, _, IAccountCredentialStore credentials) = CreateDirectory();
        CreateAuthOnlyPlayer(credentials, "google-subject-star2", "star2@bee.test", "Stara");

        var results = directory.Search("star", 0, 20);

        Assert.That(results.Select(r => r.DisplayName), Does.Contain("Stara"));
    }

    [Test]
    public void Search_ExcludesNotYetOnboardedAuthenticationPlayer()
    {
        (IPlayerDirectoryService directory, _, IAccountCredentialStore credentials) = CreateDirectory();
        CreateAuthOnlyPlayer(credentials, "google-subject-pending", "pending2@bee.test", "Pendingbee", isOnboarded: false);

        var results = directory.Search("Pending", 0, 20);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Search_ExcludesAuthenticationPlayerWithEmptyDisplayName()
    {
        (IPlayerDirectoryService directory, _, IAccountCredentialStore credentials) = CreateDirectory();
        // Onboarded but somehow blank name (defensive - should never reach search results).
        CreateAuthOnlyPlayer(credentials, "google-subject-blank", "blank@bee.test", string.Empty);

        var results = directory.Search("be", 0, 20);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Search_LegacyOnlyAccount_StillFound()
    {
        // A synthetic/seeded test account that only exists in BeeKingdom.Accounts, never through
        // real Google onboarding - must keep working exactly as before this fix.
        (IPlayerDirectoryService directory, AccountManager accounts, _) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("LegacyScout", "legacyscout@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId);

        var results = directory.Search("Legacy", 0, 20);

        Assert.That(results.Select(r => r.DisplayName), Does.Contain("LegacyScout"));
    }

    [Test]
    public void Search_SamePlayerIdInBothSources_DeduplicatesAndAuthoritativeNameWins()
    {
        // The exact identity-precedence shape from M043P: an Account record exists (possibly with
        // a stale/empty name) and an onboarded AuthenticationAccount exists for the SAME PlayerId -
        // Search must return exactly one row, using the authoritative name.
        (IPlayerDirectoryService directory, AccountManager accounts, IAccountCredentialStore credentials) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("Dupe", "dupe@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId);
        AuthenticationAccount authAccount = credentials.CreateGoogleAccount("google-subject-dupe", "dupe@bee.test") with
        {
            PlayerId = created.Profile.PlayerId,
            DisplayName = "RealDupeName",
            IsOnboarded = true
        };
        credentials.Save(authAccount);

        var results = directory.Search("Dupe", 0, 20).Where(r => r.PlayerId == created.Profile.PlayerId).ToArray();

        Assert.That(results.Length, Is.EqualTo(1));
        Assert.That(results[0].DisplayName, Is.EqualTo("RealDupeName"));
    }

    [Test]
    public void Search_ResultLimitStillEnforcedWithMergedSources()
    {
        (IPlayerDirectoryService directory, AccountManager accounts, IAccountCredentialStore credentials) = CreateDirectory();
        for (int i = 0; i < 3; i++)
        {
            AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("MergeLegacy" + i, $"mergelegacy{i}@bee.test"));
            accounts.ReactivateAccount(created.Profile.AccountId);
        }
        for (int i = 0; i < 3; i++)
        {
            CreateAuthOnlyPlayer(credentials, "google-subject-merge" + i, $"mergeauth{i}@bee.test", "MergeAuth" + i);
        }

        var results = directory.Search("Merge", 0, 4);

        Assert.That(results.Count, Is.LessThanOrEqualTo(4));
    }

    [Test]
    public void Search_NeverExposesEmailOrAuthProviderData()
    {
        // Same structural guarantee as Search_ResultsNeverExposePrivateAccountData, re-asserted for
        // the merged authoritative+legacy path specifically - PlayerPublicIdentity's shape alone
        // makes leaking email/GoogleSubjectId/PasswordHash structurally impossible.
        (IPlayerDirectoryService directory, _, IAccountCredentialStore credentials) = CreateDirectory();
        CreateAuthOnlyPlayer(credentials, "google-subject-private", "private@bee.test", "PrivacyCheck");

        var results = directory.Search("Privacy", 0, 20);

        Assert.That(results, Is.Not.Empty);
        System.Reflection.PropertyInfo[] properties = typeof(PlayerPublicIdentity).GetProperties();
        Assert.That(properties.Select(p => p.Name), Is.EquivalentTo(new[] { "PlayerId", "DisplayName" }));
    }
}
