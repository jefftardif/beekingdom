using System.Text.RegularExpressions;
using BeeKingdom.Infrastructure.Time;
using Microsoft.Extensions.Options;

namespace BeeKingdom.News;

// Bilingual (EN/FR) News/Actualites CMS service. Validation rules (see the mission spec this
// implements):
//   - Slug must be URL-safe: lowercase letters, digits and hyphens only, no leading/trailing/
//     doubled hyphens - it is the public /news/[slug] URL segment on the companion website.
//   - A Draft may be saved with only one locale filled in while being written.
//   - Publish requires BOTH TitleEn+TitleFr and BOTH BodyEn+BodyFr to be non-empty.
//   - PublishedAtUtc is set exactly once, the first time an article transitions to Published -
//     a later publish (after unpublish + edit) never resets it.
public sealed class NewsService
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private readonly INewsRepository repository;
    private readonly IOptions<NewsOptions> options;
    private readonly IServerClock clock;

    public NewsService(INewsRepository repository, IOptions<NewsOptions> options, IServerClock clock)
    {
        this.repository = repository;
        this.options = options;
        this.clock = clock;
    }

    private void RequireEnabled()
    {
        if (!options.Value.Enabled) throw new InvalidOperationException("news_disabled");
    }

    public static bool IsValidSlug(string? slug) => !string.IsNullOrWhiteSpace(slug) && slug.Length <= 200 && SlugPattern.IsMatch(slug);

    // ---------------- Commands ----------------

    public async Task<NewsArticleCommandResult> CreateAsync(Guid actorAccountId, NewsArticleCreateRequest request, CancellationToken ct = default)
    {
        RequireEnabled();
        if (request is null || !IsValidSlug(request.Slug))
        {
            return new NewsArticleCommandResult(false, "invalid_request", null);
        }

        DateTimeOffset now = clock.UtcNow;
        NewsArticle article = new(
            request.Slug,
            request.TitleEn ?? string.Empty,
            request.TitleFr ?? string.Empty,
            request.ExcerptEn ?? string.Empty,
            request.ExcerptFr ?? string.Empty,
            request.BodyEn ?? string.Empty,
            request.BodyFr ?? string.Empty,
            NewsArticleStatus.Draft, // client-supplied status is always ignored - new articles always start as Draft.
            null,
            now,
            now,
            actorAccountId);

        bool created = await repository.CreateAsync(article, ct);
        return created
            ? new NewsArticleCommandResult(true, "ok", article)
            : new NewsArticleCommandResult(false, "slug_taken", null);
    }

    public async Task<NewsArticleCommandResult> UpdateAsync(string slug, NewsArticleUpdateRequest request, CancellationToken ct = default)
    {
        RequireEnabled();
        NewsArticle? existing = await repository.GetBySlugAsync(slug, ct);
        if (existing is null || request is null)
        {
            return new NewsArticleCommandResult(false, "not_found", null);
        }

        NewsArticle updated = existing with
        {
            TitleEn = request.TitleEn ?? existing.TitleEn,
            TitleFr = request.TitleFr ?? existing.TitleFr,
            ExcerptEn = request.ExcerptEn ?? existing.ExcerptEn,
            ExcerptFr = request.ExcerptFr ?? existing.ExcerptFr,
            BodyEn = request.BodyEn ?? existing.BodyEn,
            BodyFr = request.BodyFr ?? existing.BodyFr,
            UpdatedAtUtc = clock.UtcNow
            // Status/PublishedAtUtc intentionally untouched - use PublishAsync/UnpublishAsync.
        };

        bool saved = await repository.UpdateAsync(updated, ct);
        return saved ? new NewsArticleCommandResult(true, "ok", updated) : new NewsArticleCommandResult(false, "not_found", null);
    }

    public async Task<NewsArticleCommandResult> PublishAsync(string slug, CancellationToken ct = default)
    {
        RequireEnabled();
        NewsArticle? existing = await repository.GetBySlugAsync(slug, ct);
        if (existing is null)
        {
            return new NewsArticleCommandResult(false, "not_found", null);
        }

        if (!HasBothLocalesFilled(existing))
        {
            return new NewsArticleCommandResult(false, "locale_incomplete", null);
        }

        DateTimeOffset now = clock.UtcNow;
        NewsArticle updated = existing with
        {
            Status = NewsArticleStatus.Published,
            // Set only if it was null - a republish after an edit must never reset the original date.
            PublishedAtUtc = existing.PublishedAtUtc ?? now,
            UpdatedAtUtc = now
        };

        bool saved = await repository.UpdateAsync(updated, ct);
        return saved ? new NewsArticleCommandResult(true, "ok", updated) : new NewsArticleCommandResult(false, "not_found", null);
    }

    public async Task<NewsArticleCommandResult> UnpublishAsync(string slug, CancellationToken ct = default)
    {
        RequireEnabled();
        NewsArticle? existing = await repository.GetBySlugAsync(slug, ct);
        if (existing is null)
        {
            return new NewsArticleCommandResult(false, "not_found", null);
        }

        // PublishedAtUtc is deliberately left untouched here - it is never cleared, only ever set
        // once by PublishAsync, so a later re-publish never needs to "remember" the old date.
        NewsArticle updated = existing with
        {
            Status = NewsArticleStatus.Draft,
            UpdatedAtUtc = clock.UtcNow
        };

        bool saved = await repository.UpdateAsync(updated, ct);
        return saved ? new NewsArticleCommandResult(true, "ok", updated) : new NewsArticleCommandResult(false, "not_found", null);
    }

    public async Task<NewsArticleCommandResult> DeleteAsync(string slug, CancellationToken ct = default)
    {
        RequireEnabled();
        bool deleted = await repository.DeleteAsync(slug, ct);
        return deleted ? new NewsArticleCommandResult(true, "ok", null) : new NewsArticleCommandResult(false, "not_found", null);
    }

    private static bool HasBothLocalesFilled(NewsArticle article) =>
        !string.IsNullOrWhiteSpace(article.TitleEn) && !string.IsNullOrWhiteSpace(article.TitleFr) &&
        !string.IsNullOrWhiteSpace(article.BodyEn) && !string.IsNullOrWhiteSpace(article.BodyFr);

    // ---------------- Reads ----------------

    // Public surface - 404s (returns null) both when the slug does not exist AND when it exists
    // but is not Published, so a draft's existence is never leaked to an unauthenticated caller.
    public async Task<NewsArticle?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default)
    {
        RequireEnabled();
        NewsArticle? article = await repository.GetBySlugAsync(slug, ct);
        return article is { Status: NewsArticleStatus.Published } ? article : null;
    }

    // Admin surface - any status.
    public async Task<NewsArticle?> GetAnyBySlugAsync(string slug, CancellationToken ct = default)
    {
        RequireEnabled();
        return await repository.GetBySlugAsync(slug, ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> ListPublishedAsync(int offset, int? limit, CancellationToken ct = default)
    {
        RequireEnabled();
        return await repository.ListPublishedAsync(offset, ResolveLimit(limit), ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> ListAllAsync(int offset, int? limit, CancellationToken ct = default)
    {
        RequireEnabled();
        return await repository.ListAllAsync(offset, ResolveLimit(limit), ct);
    }

    private int ResolveLimit(int? requested) => Math.Clamp(requested ?? options.Value.DefaultPageSize, 1, options.Value.MaxPageSize);
}
