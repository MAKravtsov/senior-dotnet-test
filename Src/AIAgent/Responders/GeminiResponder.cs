using AIAgent.Configuration;
using AIAgent.Store;
using Google.GenAI;
using Microsoft.Extensions.Options;

namespace AIAgent.Responders;

internal class GeminiResponder : IAiResponder
{
    private const string CollectionName = "knowledge_base";
    private const string EmbeddingModel = "gemini-embedding-001";
    private const string ChatModel = "gemini‑2.5‑flash";
    private const int TopChunks = 4;

    private const string PromptTemplate = @"
        Используй только этот контекст для ответа.
        Если ответа нет — скажи, что информации недостаточно.

        Контекст:
        {0}

        Вопрос:
        {1}
        ";

    private readonly Client _client;
    private readonly IMemoryStore _memoryStore;

    public GeminiResponder(
        IOptions<AgentConfig> config,
        IMemoryStore memoryStore)
    {
        _client = new Client(apiKey: config.Value.GeminiApiKey);
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
        var embeddingResp = await _client.Models.EmbedContentAsync(EmbeddingModel, text);
        return embeddingResp.Embeddings![0].Values!.ConvertAll(x => (float)x).ToArray();
    }

    private async Task<string> GetResponse(string prompt, CancellationToken cancellationToken)
    {
        var response = await _client.Models.GenerateContentAsync(ChatModel, prompt);
        return response!.Candidates![0].Content!.Parts![0].Text!;
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
