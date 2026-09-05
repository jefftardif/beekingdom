namespace BeeKingdom.News;

public interface INewsRepository
{
    // Insert-only - CreateAsync must fail (return false) if the slug already exists so the
    // service can surface news.slug_taken instead of silently overwriting another article.
    Task<bool> CreateAsync(NewsArticle article, CancellationToken cancellationToken = default);

    Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    // Full replace of the mutable columns of an existing row. Returns false if the slug does not exist.
    Task<bool> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken = default);

    // Published only, newest-first by PublishedAtUtc. offset/limit back the opaque string cursor
    // the HTTP layer exposes (see NewsServiceCollectionExtensions/Program.cs wiring).
    Task<IReadOnlyList<NewsArticle>> ListPublishedAsync(int offset, int limit, CancellationToken cancellationToken = default);

    // Draft + Published, newest-first by UpdatedAtUtc - admin only.
    Task<IReadOnlyList<NewsArticle>> ListAllAsync(int offset, int limit, CancellationToken cancellationToken = default);
}
