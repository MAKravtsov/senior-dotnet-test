namespace AIAgent.Store;

internal interface IMemoryStore
{
    void SaveAsync(string collection, string text, ReadOnlyMemory<float> embedding);

    List<string> SearchAsync(string collection, ReadOnlyMemory<float> queryEmbedding, int topK = 3);
}
