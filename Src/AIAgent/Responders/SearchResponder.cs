using System.Text.Json;
using System.Text.RegularExpressions;
using AIAgent.KnowledgeBase;

namespace AIAgent.Responders;

internal class SearchResponder : IAiResponder
{
    private readonly List<Article> _articles = new();

    public SearchResponder()
    {
    }

    public async Task Prepare(CancellationToken cancellationToken)
    {
        var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var path = Path.Combine(baseDir, Constants.KnowledgeBaseFileName);
        if (!File.Exists(path)) return;

        var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        KnowledgeRoot? root = null;
        try
        {
            root = JsonSerializer.Deserialize<KnowledgeRoot>(text, options);
        }
        catch (JsonException)
        {
            // ignore and fallback to raw text
        }

        _articles.Clear();

        if (root?.Articles != null && root.Articles.Count > 0)
        {
            foreach (var a in root.Articles)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var article = new Article
                {
                    Slug = a.Slug ?? string.Empty,
                    Title = a.Title ?? string.Empty,
                    Content = a.Content ?? string.Empty,
                    Tokens = TokenizeToSet((a.Title ?? string.Empty) + " " + (a.Content ?? string.Empty))
                };
                _articles.Add(article);
            }
        }
        else
        {
            // fallback — entire file as a single article
            var article = new Article
            {
                Slug = "knowledge",
                Title = string.Empty,
                Content = text ?? string.Empty,
                Tokens = TokenizeToSet(text ?? string.Empty)
            };
            _articles.Add(article);
        }
    }

    public Task<string> Respond(string message, CancellationToken cancellationToken)
    {
        if (_articles.Count == 0) return Task.FromResult("Извини, база знаний не загружена.");
        if (string.IsNullOrWhiteSpace(message)) return Task.FromResult("Пожалуйста, задайте вопрос.");

        var qTokens = TokenizeToSet(message);
        if (qTokens.Count == 0) return Task.FromResult("Извини, не могу понять запрос.");

        Article? best = null;
        int bestScore = 0;

        foreach (var a in _articles)
        {
            var score = a.Tokens.Count(t => qTokens.Contains(t));
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        if (best is null || bestScore == 0)
            return Task.FromResult("Извини, не нашёл подходящей информации в базе знаний.");

        var match = FindBestSentence(best.Content, qTokens);
        if (!string.IsNullOrWhiteSpace(match)) return Task.FromResult(match.Trim());

        var snippet = best.Content?.Trim() ?? string.Empty;
        if (snippet.Length > 800) snippet = snippet[..800] + "...";
        return Task.FromResult(snippet.Length > 0 ? snippet : "Извини, не могу сгенерировать ответ.");
    }

    // Simple tokenizer: collect words (Cyrillic/Latin/digits) of length >=2
    private static HashSet<string> TokenizeToSet(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return tokens;

        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), "[\u0400-\u04FF\u0020-\u007F]+"))
        {
            foreach (var t in Regex.Split(m.Value, @"[^\p{L}0-9]+"))
            {
                var w = t.Trim();
                if (w.Length >= 2) tokens.Add(w);
            }
        }

        return tokens;
    }

    private static string FindBestSentence(string content, HashSet<string> qTokens)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        var sentences = Regex.Split(content, "(?<=[.!?])\\s+");
        string best = string.Empty;
        int bestScore = 0;

        foreach (var s in sentences)
        {
            var sTokens = TokenizeToSet(s);
            var score = sTokens.Count(t => qTokens.Contains(t));
            if (score > bestScore)
            {
                bestScore = score;
                best = s;
            }
        }

        return bestScore > 0 ? best : string.Empty;
    }
}
