namespace BeeKingdom.News;

public sealed class NewsOptions
{
    public const string SectionName = "News";

    public bool Enabled { get; set; } = true;

    public int DefaultPageSize { get; set; } = 20;

    public int MaxPageSize { get; set; } = 100;

    public void Validate()
    {
        if (DefaultPageSize <= 0) throw new InvalidDataException("News:DefaultPageSize must be positive.");
        if (MaxPageSize <= 0 || MaxPageSize < DefaultPageSize) throw new InvalidDataException("News:MaxPageSize must be positive and >= DefaultPageSize.");
    }
}
