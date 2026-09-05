using System.Data;
using BeeKingdom.Persistence.Sql;

namespace BeeKingdom.News;

// Schema: Server/src/BeeKingdom.Database/Scripts/093_news_articles.sql (NOT executed against
// production by this task - a human reviews and applies it separately, same workflow as every
// other migration in this codebase). One row per article, columns rather than a single JSON blob
// (unlike AllianceResearch/Chat's per-aggregate document rows) because News needs real relational
// listing/sorting (ListPublished by PublishedAtUtc, ListAll by UpdatedAtUtc) - mirrors
// SqlChatRepository's row-per-entity ADO.NET pattern.
public sealed class SqlNewsRepository : INewsRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public SqlNewsRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task<bool> CreateAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM dbo.NewsArticles WHERE Slug = @Slug)
            BEGIN
                INSERT INTO dbo.NewsArticles
                (Slug, TitleEn, TitleFr, ExcerptEn, ExcerptFr, BodyEn, BodyFr, Status, PublishedAtUtc, CreatedAtUtc, UpdatedAtUtc, CreatedByAccountId)
                VALUES
                (@Slug, @TitleEn, @TitleFr, @ExcerptEn, @ExcerptFr, @BodyEn, @BodyFr, @Status, @PublishedAtUtc, @CreatedAtUtc, @UpdatedAtUtc, @CreatedByAccountId);
                SELECT 1;
            END
            ELSE
            BEGIN
                SELECT 0;
            END
            """;
        AddParameters(command, article);
        object? result = command.ExecuteScalar();
        return Task.FromResult(Convert.ToInt32(result) == 1);
    }

    public Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns()}\nFROM dbo.NewsArticles\nWHERE Slug = @Slug;";
        Add(command, "@Slug", slug);
        using IDataReader reader = command.ExecuteReader();
        return Task.FromResult(reader.Read() ? ReadArticle(reader) : null);
    }

    public Task<bool> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.NewsArticles
            SET TitleEn = @TitleEn, TitleFr = @TitleFr, ExcerptEn = @ExcerptEn, ExcerptFr = @ExcerptFr,
                BodyEn = @BodyEn, BodyFr = @BodyFr, Status = @Status, PublishedAtUtc = @PublishedAtUtc,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE Slug = @Slug;
            """;
        AddParameters(command, article);
        int affected = command.ExecuteNonQuery();
        return Task.FromResult(affected > 0);
    }

    public Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.NewsArticles WHERE Slug = @Slug;";
        Add(command, "@Slug", slug);
        int affected = command.ExecuteNonQuery();
        return Task.FromResult(affected > 0);
    }

    public Task<IReadOnlyList<NewsArticle>> ListPublishedAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectColumns()}
            FROM dbo.NewsArticles
            WHERE Status = @Status
            ORDER BY PublishedAtUtc DESC, Slug DESC
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            """;
        Add(command, "@Status", NewsArticleStatus.Published.ToString());
        Add(command, "@Offset", Math.Max(0, offset));
        Add(command, "@Limit", Math.Clamp(limit, 1, 100));
        using IDataReader reader = command.ExecuteReader();
        return Task.FromResult(ReadArticles(reader));
    }

    public Task<IReadOnlyList<NewsArticle>> ListAllAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectColumns()}
            FROM dbo.NewsArticles
            ORDER BY UpdatedAtUtc DESC, Slug DESC
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            """;
        Add(command, "@Offset", Math.Max(0, offset));
        Add(command, "@Limit", Math.Clamp(limit, 1, 100));
        using IDataReader reader = command.ExecuteReader();
        return Task.FromResult(ReadArticles(reader));
    }

    private static string SelectColumns() => """
        SELECT Slug, TitleEn, TitleFr, ExcerptEn, ExcerptFr, BodyEn, BodyFr, Status, PublishedAtUtc, CreatedAtUtc, UpdatedAtUtc, CreatedByAccountId
        """;

    private static IReadOnlyList<NewsArticle> ReadArticles(IDataReader reader)
    {
        List<NewsArticle> articles = [];
        while (reader.Read())
        {
            articles.Add(ReadArticle(reader));
        }

        return articles;
    }

    private static NewsArticle ReadArticle(IDataReader reader)
    {
        return new NewsArticle(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            Enum.Parse<NewsArticleStatus>(reader.GetString(7), ignoreCase: true),
            reader.IsDBNull(8) ? null : AsUtc(reader.GetDateTime(8)),
            AsUtc(reader.GetDateTime(9)),
            AsUtc(reader.GetDateTime(10)),
            reader.GetGuid(11));
    }

    private static void AddParameters(IDbCommand command, NewsArticle article)
    {
        Add(command, "@Slug", article.Slug);
        Add(command, "@TitleEn", article.TitleEn);
        Add(command, "@TitleFr", article.TitleFr);
        Add(command, "@ExcerptEn", article.ExcerptEn);
        Add(command, "@ExcerptFr", article.ExcerptFr);
        Add(command, "@BodyEn", article.BodyEn);
        Add(command, "@BodyFr", article.BodyFr);
        Add(command, "@Status", article.Status.ToString());
        Add(command, "@PublishedAtUtc", article.PublishedAtUtc.HasValue ? article.PublishedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@CreatedAtUtc", article.CreatedAtUtc.UtcDateTime);
        Add(command, "@UpdatedAtUtc", article.UpdatedAtUtc.UtcDateTime);
        Add(command, "@CreatedByAccountId", article.CreatedByAccountId);
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
