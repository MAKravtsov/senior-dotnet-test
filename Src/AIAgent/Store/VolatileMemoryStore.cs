using AIAgent.Store;
using System.Collections.Concurrent;

internal class VolatileMemoryStore : IMemoryStore
{
    private readonly ConcurrentDictionary<string, List<(string Text, ReadOnlyMemory<float> Embedding)>> _store = new();

    public async void SaveAsync(string collection, string text, ReadOnlyMemory<float> embedding)
    {
        _store.AddOrUpdate(
            collection,
            _ => new List<(string, ReadOnlyMemory<float>)> { (text, embedding) },
            (_, list) =>
            {
                list.Add((text, embedding));
                return list;
            });
    }

    public List<string> SearchAsync(string collection, ReadOnlyMemory<float> queryEmbedding, int topK = 3)
    {
        if (!_store.TryGetValue(collection, out var list))
            return new List<string>();

        return list
            .Select(item => new
            {
                Text = item.Text,
                Score = CosineSimilarity(queryEmbedding, item.Embedding)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Text)
            .ToList();
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> rm1, ReadOnlyMemory<float> rm2)
    {
        var v1 = rm1.Span;
        var v2 = rm2.Span;
        if (v1.Length == 0 || v2.Length == 0) return 0;
        double dot = 0, norm1 = 0, norm2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            norm1 += v1[i] * v1[i];
            norm2 += v2[i] * v2[i];
        }
        return dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }
}
