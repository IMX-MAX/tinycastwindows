using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Tinycast.Windows;

public sealed class MistralClient
{
    public const string DefaultBaseUrl = "https://api.mistral.ai/v1";
    public const string DefaultModel = "mistral-small-latest";

    static readonly string[] Catalog =
    [
        "mistral-small-latest",
        "mistral-medium-latest",
        "mistral-large-latest",
        "codestral-latest",
        "ministral-8b-latest",
        "pixtral-large-latest"
    ];

    readonly HttpMessageHandler _handler;
    readonly Func<HttpClient> _clientFactory;

    public MistralClient(HttpMessageHandler? handler = null)
    {
        _handler = handler ?? new HttpClientHandler();
        _clientFactory = () =>
        {
            var client = handler is null ? new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            }, disposeHandler: true) : new HttpClient(handler, disposeHandler: false);
            client.Timeout = TimeSpan.FromMinutes(5);
            return client;
        };
    }

    public static IReadOnlyList<string> Models => Catalog;

    public async IAsyncEnumerable<string> StreamChatAsync(
        string apiKey,
        string baseUrl,
        string model,
        IReadOnlyList<ChatMessage> messages,
        string? instructions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Add a Mistral API key in Settings → AI.");

        var payload = new Dictionary<string, object?>
        {
            ["model"] = string.IsNullOrWhiteSpace(model) ? DefaultModel : model,
            ["stream"] = true,
            ["messages"] = BuildMessages(messages, instructions)
        };

        using var client = _clientFactory();
        using var request = new HttpRequestMessage(HttpMethod.Post, Combine(baseUrl, "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if ((int)response.StatusCode == 401)
            throw new InvalidOperationException("Mistral rejected the API key.");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Mistral HTTP {(int)response.StatusCode}: {Trim(body)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].Trim();
            if (data is "[DONE]" or "") continue;
            foreach (var token in DecodeDelta(data))
                yield return token;
        }
    }

    public async Task<string> CompleteAsync(
        string apiKey, string baseUrl, string model, string prompt, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        await foreach (var token in StreamChatAsync(
            apiKey, baseUrl, model, [new ChatMessage { Role = "user", Content = prompt }],
            null, cancellationToken).ConfigureAwait(false))
        {
            builder.Append(token);
        }
        return builder.ToString();
    }

    public static IReadOnlyList<object> BuildMessages(IReadOnlyList<ChatMessage> messages, string? instructions)
    {
        var list = new List<object>();
        if (!string.IsNullOrWhiteSpace(instructions))
            list.Add(new { role = "system", content = instructions });
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content)) continue;
            list.Add(new { role = message.Role, content = message.Content });
        }
        return list;
    }

    public static IEnumerable<string> DecodeDelta(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("choices", out var choices)) yield break;
        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("delta", out var delta)) continue;
            if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            {
                var text = content.GetString();
                if (!string.IsNullOrEmpty(text)) yield return text;
            }
        }
    }

    static Uri Combine(string baseUrl, string path)
    {
        var trimmed = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim().TrimEnd('/');
        if (!trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !IsLoopback(trimmed))
            throw new InvalidOperationException("Mistral endpoints must be HTTPS (or localhost).");
        return new Uri(trimmed + "/" + path);
    }

    static bool IsLoopback(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp &&
               (uri.Host is "localhost" or "127.0.0.1" or "::1");
    }

    static string Trim(string body) => body.Length <= 240 ? body : body[..240];
}
