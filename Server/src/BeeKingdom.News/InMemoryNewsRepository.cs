using System.Collections.Concurrent;

namespace BeeKingdom.News;

// Non-SQL local/dev environments only - production runs Persistence:Provider=SqlServer (see
// SqlNewsRepository's class comment). Not durable across a process restart.
public sealed class InMemoryNewsRepository : INewsRepository
{
    private readonly ConcurrentDictionary<string, NewsArticle> articles = new(StringComparer.Ordinal);

    public Task<bool> CreateAsync(NewsArticle article, CancellationToken cancellationToken = default)
        => Task.FromResult(articles.TryAdd(article.Slug, article));

    public Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(articles.GetValueOrDefault(slug));

    public Task<bool> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        if (!articles.ContainsKey(article.Slug))
        {
            return Task.FromResult(false);
        }

        articles[article.Slug] = article;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(articles.TryRemove(slug, out _));

    public Task<IReadOnlyList<NewsArticle>> ListPublishedAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NewsArticle> page = articles.Values
            .Where(a => a.Status == NewsArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAtUtc)
            .ThenByDescending(a => a.Slug, StringComparer.Ordinal)
            .Skip(Math.Max(0, offset))
            .Take(Math.Clamp(limit, 1, 100))
            .ToList();
        return Task.FromResult(page);
    }

    public Task<IReadOnlyList<NewsArticle>> ListAllAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NewsArticle> page = articles.Values
            .OrderByDescending(a => a.UpdatedAtUtc)
            .ThenByDescending(a => a.Slug, StringComparer.Ordinal)
            .Skip(Math.Max(0, offset))
            .Take(Math.Clamp(limit, 1, 100))
            .ToList();
        return Task.FromResult(page);
    }
}
