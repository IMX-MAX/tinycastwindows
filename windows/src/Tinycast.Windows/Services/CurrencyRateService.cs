using System.Text.Json;

namespace Tinycast.Windows;

sealed class CurrencyRateService
{
    readonly JsonStore<CurrencyRateSnapshot> _cache = new(AppPaths.CurrencyRates);

    public CurrencyRateSnapshot LoadCached() => _cache.Load();

    public async Task<CurrencyRateSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            UseCookies = false
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "https://api.frankfurter.app/latest?from=USD");
        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1
        };
        foreach (var property in document.RootElement.GetProperty("rates").EnumerateObject())
        {
            if (property.Value.TryGetDouble(out var value) && value > 0)
                rates[property.Name] = value;
        }

        var snapshot = new CurrencyRateSnapshot
        {
            FetchedAt = DateTimeOffset.UtcNow,
            Rates = rates
        };
        _cache.Save(snapshot);
        return snapshot;
    }
}

sealed class CurrencyRateSnapshot
{
    public DateTimeOffset FetchedAt { get; set; }
    public Dictionary<string, double> Rates { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { ["USD"] = 1 };
}
