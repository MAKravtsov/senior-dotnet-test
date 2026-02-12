namespace AIAgent.KnowledgeBase;

internal class Article
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public HashSet<string> Tokens { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
