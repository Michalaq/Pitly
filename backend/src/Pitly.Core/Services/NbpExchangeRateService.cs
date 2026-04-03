using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Pitly.Core.Services;

public class NbpExchangeRateService : INbpExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NbpExchangeRateService> _logger;
    private readonly ConcurrentDictionary<string, decimal> _cache = new();

    public NbpExchangeRateService(HttpClient httpClient, ILogger<NbpExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<decimal> GetRateAsync(string currency, DateTime transactionDate)
    {
        if (currency.Equals("PLN", StringComparison.OrdinalIgnoreCase))
            return 1m;
        const int maxAttempts = 10;

        // Polish tax law: rate from last business day BEFORE the transaction date
        var rateDate = transactionDate.Date.AddDays(-1);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var dateStr = rateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var cacheKey = $"{currency.ToUpperInvariant()}_{dateStr}";

            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                var url = $"https://api.nbp.pl/api/exchangerates/rates/A/{currency.ToUpperInvariant()}/{dateStr}/?format=json";
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("NBP rate not found for {Currency} on {Date}, trying previous day", currency, dateStr);
                    rateDate = rateDate.AddDays(-1);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var rate = doc.RootElement
                    .GetProperty("rates")[0]
                    .GetProperty("mid")
                    .GetDecimal();

                _cache.TryAdd(cacheKey, rate);
                _logger.LogDebug("NBP rate for {Currency} on {Date}: {Rate}", currency, dateStr, rate);
                return rate;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts - 1)
            {
                _logger.LogWarning(ex, "NBP API request failed for {Currency} on {Date} (attempt {Attempt}/{MaxAttempts}), retrying",
                    currency, dateStr, attempt + 1, maxAttempts);
                rateDate = rateDate.AddDays(-1);
            }
        }

        _logger.LogError("Failed to get NBP rate for {Currency} near {Date} after {MaxAttempts} attempts", currency, transactionDate, maxAttempts);
        throw new InvalidOperationException(
            $"Could not find NBP exchange rate for {currency} near {transactionDate:yyyy-MM-dd} after {maxAttempts} attempts.");
    }
}
