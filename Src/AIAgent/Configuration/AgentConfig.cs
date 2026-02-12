namespace AIAgent.Configuration;

internal class AgentConfig
{
    public string OpenAIApiKey { get; set; } = string.Empty;

    public string GeminiApiKey { get; set; } = string.Empty;

    public ResponderType Type { get; set; } = ResponderType.Search;
}
