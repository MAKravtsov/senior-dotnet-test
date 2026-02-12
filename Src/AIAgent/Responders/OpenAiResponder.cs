using AIAgent.Configuration;
using AIAgent.Store;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace AIAgent.Responders;

/// <summary>
/// Local OpenAiResponder (Markov / n-gram) for the AIAgent service.
/// Trained on knowledgebase.txt found in the app folder.
/// </summary>
internal class OpenAiResponder : IAiResponder
{
    private const string CollectionName = "knowledge_base";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";
    private const int TopChunks = 4;

    private const string PromptTemplate = @"
        Используй только этот контекст для ответа.
        Если ответа нет — скажи, что информации недостаточно.

        Контекст:
        {0}

        Вопрос:
        {1}
        ";

    private readonly IMemoryStore _memoryStore;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ChatClient _chatClient;

    public OpenAiResponder(
        IOptions<AgentConfig> config,
        IMemoryStore memoryStore)
    {
        var openApiClient = new OpenAIClient(config.Value.OpenAIApiKey);
        _embeddingClient = openApiClient.GetEmbeddingClient(EmbeddingModel);
        _chatClient = openApiClient.GetChatClient(ChatModel);
        _memoryStore = memoryStore ?? throw new ArgumentNullException(nameof(memoryStore));
    }

    public async Task Prepare(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            Constants.KnowledgeBaseFileName);
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        var chunks = SplitIntoChunks(text, 1000); // Структура базы знаний конкретная, возможно здесь стоило разбивать по элементам json

        foreach (var chunk in chunks)
        {
            var vector = await GetEmbeddingVector(chunk, cancellationToken);
            _memoryStore.SaveAsync(CollectionName, chunk, vector);
        }
    }

    public async Task<string> Respond(string message, CancellationToken cancellationToken)
    {
        var vector = await GetEmbeddingVector(message, cancellationToken);
        var prompt = GeneratePrompt(vector, message);
        var response = await GetResponse(prompt, cancellationToken);
        return response;
    }

    private async Task<ReadOnlyMemory<float>> GetEmbeddingVector(string text, CancellationToken cancellationToken)
    {
        var embeddingResp = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embeddingResp.Value.ToFloats();
    }

    private async Task<string> GetResponse(string prompt, CancellationToken cancellationToken)
    {
        var response = await _chatClient.CompleteChatAsync(prompt);
        return response.Value.Content[0].Text;
    }

    private string GeneratePrompt(ReadOnlyMemory<float> vector, string message)
    {
        var topChunks = _memoryStore.SearchAsync(CollectionName, vector, TopChunks);
        var context = string.Join("\n\n---\n\n", topChunks);
        var prompt = string.Format(PromptTemplate, context, message);
        return prompt;
    }

    private static List<string> SplitIntoChunks(string text, int size)
    {
        var result = new List<string>();

        for (int i = 0; i < text.Length; i += size)
        {
            result.Add(text.Substring(i, Math.Min(size, text.Length - i)));
        }

        return result;
    }
}
