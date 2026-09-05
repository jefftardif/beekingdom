using BeeKingdom.Infrastructure.Time;
using BeeKingdom.News;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// Exercises NewsService's validation/lifecycle rules against InMemoryNewsRepository - the same
// "against the real in-memory repository, not a mock" convention AllianceResearchServiceTests uses.
public sealed class NewsServiceTests
{
    private sealed class TestClock : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; }

        public TestClock(DateTimeOffset now) => UtcNow = now;
    }

    private sealed record Fixture(NewsService Service, InMemoryNewsRepository Repository, TestClock Clock);

    private static Fixture CreateFixture()
    {
        InMemoryNewsRepository repository = new();
        TestClock clock = new(DateTimeOffset.UtcNow);
        IOptions<NewsOptions> options = Options.Create(new NewsOptions { Enabled = true });
        return new Fixture(new NewsService(repository, options, clock), repository, clock);
    }

    private static NewsArticleCreateRequest ValidCreateRequest(string slug = "alpha-launch-announcement") =>
        new(slug, "Title EN", "Titre FR", "Excerpt EN", "Extrait FR", "Body EN", "Corps FR");

    [TestCase("alpha-launch-announcement")]
    [TestCase("a")]
    [TestCase("a1-b2-c3")]
    public void IsValidSlug_AcceptsLowercaseHyphenatedSlugs(string slug)
    {
        Assert.That(NewsService.IsValidSlug(slug), Is.True);
    }

    [TestCase("Alpha-Launch")]
    [TestCase("alpha_launch")]
    [TestCase("-alpha-launch")]
    [TestCase("alpha-launch-")]
    [TestCase("alpha--launch")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("alpha launch")]
    public void IsValidSlug_RejectsInvalidSlugs(string? slug)
    {
        Assert.That(NewsService.IsValidSlug(slug), Is.False);
    }

    [Test]
    public async Task CreateAsync_RejectsInvalidSlug()
    {
        Fixture fx = CreateFixture();
        NewsArticleCommandResult result = await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest("Not A Slug"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("invalid_request"));
    }

    [Test]
    public async Task CreateAsync_AllowsPartialLocaleDraft()
    {
        Fixture fx = CreateFixture();
        NewsArticleCreateRequest request = new("draft-only-english", "Title EN", null, "Excerpt EN", null, "Body EN", null);
        NewsArticleCommandResult result = await fx.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Article!.Status, Is.EqualTo(NewsArticleStatus.Draft));
        Assert.That(result.Article.TitleFr, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task CreateAsync_RejectsDuplicateSlug()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());
        NewsArticleCommandResult second = await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());

        Assert.That(second.Succeeded, Is.False);
        Assert.That(second.Code, Is.EqualTo("slug_taken"));
    }

    [Test]
    public async Task PublishAsync_RejectsWhenALocaleIsIncomplete()
    {
        Fixture fx = CreateFixture();
        NewsArticleCreateRequest request = new("half-baked", "Title EN", null, "Excerpt EN", null, "Body EN", null);
        await fx.Service.CreateAsync(Guid.NewGuid(), request);

        NewsArticleCommandResult publish = await fx.Service.PublishAsync("half-baked");

        Assert.That(publish.Succeeded, Is.False);
        Assert.That(publish.Code, Is.EqualTo("locale_incomplete"));
        NewsArticle? stored = await fx.Repository.GetBySlugAsync("half-baked");
        Assert.That(stored!.Status, Is.EqualTo(NewsArticleStatus.Draft));
    }

    [Test]
    public async Task PublishAsync_SucceedsWhenBothLocalesComplete_AndSetsPublishedAtUtc()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());

        NewsArticleCommandResult publish = await fx.Service.PublishAsync("alpha-launch-announcement");

        Assert.That(publish.Succeeded, Is.True);
        Assert.That(publish.Article!.Status, Is.EqualTo(NewsArticleStatus.Published));
        Assert.That(publish.Article.PublishedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task PublishAsync_NeverResetsPublishedAtUtc_OnRepublishAfterEdit()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());
        NewsArticleCommandResult firstPublish = await fx.Service.PublishAsync("alpha-launch-announcement");
        DateTimeOffset originalPublishedAt = firstPublish.Article!.PublishedAtUtc!.Value;

        // Unpublish, edit, advance the clock, then republish - PublishedAtUtc must stay the ORIGINAL date.
        await fx.Service.UnpublishAsync("alpha-launch-announcement");
        await fx.Service.UpdateAsync("alpha-launch-announcement", new NewsArticleUpdateRequest("Updated Title EN", null, null, null, null, null));
        fx.Clock.UtcNow = fx.Clock.UtcNow.AddDays(3);
        NewsArticleCommandResult republish = await fx.Service.PublishAsync("alpha-launch-announcement");

        Assert.That(republish.Succeeded, Is.True);
        Assert.That(republish.Article!.PublishedAtUtc, Is.EqualTo(originalPublishedAt));
        Assert.That(republish.Article.TitleEn, Is.EqualTo("Updated Title EN"));
    }

    [Test]
    public async Task UnpublishAsync_KeepsOriginalPublishedAtUtc_AndSetsStatusToDraft()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());
        NewsArticleCommandResult publish = await fx.Service.PublishAsync("alpha-launch-announcement");
        DateTimeOffset publishedAt = publish.Article!.PublishedAtUtc!.Value;

        NewsArticleCommandResult unpublish = await fx.Service.UnpublishAsync("alpha-launch-announcement");

        Assert.That(unpublish.Succeeded, Is.True);
        Assert.That(unpublish.Article!.Status, Is.EqualTo(NewsArticleStatus.Draft));
        Assert.That(unpublish.Article.PublishedAtUtc, Is.EqualTo(publishedAt));
    }

    [Test]
    public async Task UpdateAsync_NeverChangesStatusOrPublishedAtUtc()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());
        await fx.Service.PublishAsync("alpha-launch-announcement");

        NewsArticleCommandResult update = await fx.Service.UpdateAsync("alpha-launch-announcement", new NewsArticleUpdateRequest("New EN Title", null, null, null, null, null));

        Assert.That(update.Succeeded, Is.True);
        Assert.That(update.Article!.Status, Is.EqualTo(NewsArticleStatus.Published));
        Assert.That(update.Article.TitleEn, Is.EqualTo("New EN Title"));
        Assert.That(update.Article.TitleFr, Is.EqualTo("Titre FR"));
    }

    [Test]
    public async Task UpdateAsync_ReturnsNotFound_ForUnknownSlug()
    {
        Fixture fx = CreateFixture();
        NewsArticleCommandResult result = await fx.Service.UpdateAsync("does-not-exist", new NewsArticleUpdateRequest("x", null, null, null, null, null));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_found"));
    }

    [Test]
    public async Task DeleteAsync_RemovesArticle()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());

        NewsArticleCommandResult delete = await fx.Service.DeleteAsync("alpha-launch-announcement");

        Assert.That(delete.Succeeded, Is.True);
        Assert.That(await fx.Repository.GetBySlugAsync("alpha-launch-announcement"), Is.Null);
    }

    [Test]
    public async Task GetPublishedBySlugAsync_ReturnsNull_ForDraftArticle_NeverLeaksDraftExistence()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());

        NewsArticle? result = await fx.Service.GetPublishedBySlugAsync("alpha-launch-announcement");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPublishedBySlugAsync_ReturnsArticle_OncePublished()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest());
        await fx.Service.PublishAsync("alpha-launch-announcement");

        NewsArticle? result = await fx.Service.GetPublishedBySlugAsync("alpha-launch-announcement");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo(NewsArticleStatus.Published));
    }

    [Test]
    public async Task ListPublishedAsync_OnlyReturnsPublishedArticles_NewestFirst()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest("draft-article"));
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest("older-published"));
        await fx.Service.PublishAsync("older-published");
        fx.Clock.UtcNow = fx.Clock.UtcNow.AddHours(1);
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest("newer-published"));
        await fx.Service.PublishAsync("newer-published");

        var published = await fx.Service.ListPublishedAsync(0, 10);

        Assert.That(published.Select(a => a.Slug), Is.EqualTo(new[] { "newer-published", "older-published" }));
    }

    [Test]
    public async Task ListAllAsync_IncludesDraftsAndPublished()
    {
        Fixture fx = CreateFixture();
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest("draft-article"));
        await fx.Service.CreateAsync(Guid.NewGuid(), ValidCreateRequest("published-article"));
        await fx.Service.PublishAsync("published-article");

        var all = await fx.Service.ListAllAsync(0, 10);

        Assert.That(all.Select(a => a.Slug), Is.EquivalentTo(new[] { "draft-article", "published-article" }));
    }
}
