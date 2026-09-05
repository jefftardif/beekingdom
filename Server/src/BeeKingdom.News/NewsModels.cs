namespace BeeKingdom.News;

// M0??-CL: bilingual (EN/FR) News/Actualites CMS backing the companion website
// (beekingdom-web, a separate Next.js repo - out of scope here, this is the server side only).
// Admin authoring gates on the EXISTING AuthenticationAccount.Role == AccountRole.Admin (see
// BeeKingdom.Authentication.Models.AccountRole) - there is no separate password system for this.
public enum NewsArticleStatus
{
    Draft = 0,
    Published = 1
}

// The whole aggregate, both locales always present as columns (never null strings - an
// unfilled locale is represented as "" while in Draft, see NewsService's validation rules).
public sealed record NewsArticle(
    string Slug,
    string TitleEn,
    string TitleFr,
    string ExcerptEn,
    string ExcerptFr,
    string BodyEn,
    string BodyFr,
    NewsArticleStatus Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid CreatedByAccountId);

// ---------------- Commands ----------------

public sealed record NewsArticleCreateRequest(
    string Slug,
    string? TitleEn,
    string? TitleFr,
    string? ExcerptEn,
    string? ExcerptFr,
    string? BodyEn,
    string? BodyFr);

// Status is intentionally absent - PUT never changes Status (use the dedicated publish/unpublish
// endpoints for that transition), matching the mission's own "update fields; does not change
// Status" instruction.
public sealed record NewsArticleUpdateRequest(
    string? TitleEn,
    string? TitleFr,
    string? ExcerptEn,
    string? ExcerptFr,
    string? BodyEn,
    string? BodyFr);

// ---------------- Results / read models ----------------

public sealed record NewsArticleCommandResult(bool Succeeded, string Code, NewsArticle? Article);

// Listing shape - no Body, for /news/v1/articles and the admin list endpoint.
public sealed record NewsArticleSummary(
    string Slug,
    string TitleEn,
    string TitleFr,
    string ExcerptEn,
    string ExcerptFr,
    NewsArticleStatus Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static NewsArticleSummary FromArticle(NewsArticle article) => new(
        article.Slug, article.TitleEn, article.TitleFr, article.ExcerptEn, article.ExcerptFr,
        article.Status, article.PublishedAtUtc, article.UpdatedAtUtc);
}

public sealed record NewsArticlePage<T>(IReadOnlyList<T> Items, string? NextCursor);

// Public detail projection - deliberately omits CreatedByAccountId (internal authorship, never
// surfaced to the public website).
public sealed record NewsArticleDetail(
    string Slug,
    string TitleEn,
    string TitleFr,
    string ExcerptEn,
    string ExcerptFr,
    string BodyEn,
    string BodyFr,
    NewsArticleStatus Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static NewsArticleDetail FromArticle(NewsArticle article) => new(
        article.Slug, article.TitleEn, article.TitleFr, article.ExcerptEn, article.ExcerptFr,
        article.BodyEn, article.BodyFr, article.Status, article.PublishedAtUtc, article.UpdatedAtUtc);
}
