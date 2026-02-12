using System.Text.Json.Serialization;

namespace AIAgent.KnowledgeBase;

internal class KnowledgeRoot
{
    [JsonPropertyName("articles")]
    public List<KnowledgeArticle>? Articles { get; set; }
}
