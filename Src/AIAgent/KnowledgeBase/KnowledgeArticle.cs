using System.Text.Json.Serialization;

namespace AIAgent.KnowledgeBase;

internal class KnowledgeArticle
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
