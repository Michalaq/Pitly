using System.Globalization;
using Microsoft.Extensions.Logging;
using Pitly.Core.Models;
using Pitly.Core.Parsing;
using static Pitly.Core.Parsing.CsvHelpers;

namespace Pitly.Broker.Trading212;

public class Trading212StatementParser : IStatementParser
{
    private static readonly string[] DateTimeFormats =
        ["yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"];

    private readonly ILogger<Trading212StatementParser> _logger;

    public Trading212StatementParser(ILogger<Trading212StatementParser> logger)
    {
        _logger = logger;
    }

    public ParsedStatement Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new FormatException("File is empty.");

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            throw new FormatException("File contains no data rows.");

        var headerFields = ParseCsvLine(lines[0].Trim());
        var columnMap = BuildColumnMap(headerFields);

        if (!columnMap.ContainsKey("action") || !columnMap.ContainsKey("time"))
            throw new FormatException("File does not appear to be a Trading 212 export. Missing required columns.");

        var trades = new List<Trade>();
        var dividends = new List<RawDividend>();
        var withholdingTaxes = new List<RawWithholdingTax>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = ParseCsvLine(line);
            var action = GetField(fields, columnMap, "action");
            if (string.IsNullOrEmpty(action)) continue;

            var actionLower = action.ToLowerInvariant();

            if (actionLower.Contains("buy") || actionLower.Contains("sell"))
            {
                var tradeType = actionLower.Contains("buy") ? TradeType.Buy : TradeType.Sell;
                TryParseTrade(fields, columnMap, tradeType, trades, i + 1);
            }
            else if (actionLower.StartsWith("dividend"))
            {
                TryParseDividend(fields, columnMap, dividends, withholdingTaxes, i + 1);
            }
        }

        if (trades.Count == 0 && dividends.Count == 0)
            throw new FormatException(
                "No trades or dividends found. Please upload a valid Trading 212 CSV export.");

        _logger.LogInformation("Parsed Trading 212 statement: {Trades} trades, {Dividends} dividends, {Withholdings} withholding tax entries",
            trades.Count, dividends.Count, withholdingTaxes.Count);

        return new ParsedStatement(trades, dividends, withholdingTaxes);
    }

    private record RowFields(string Ticker, DateTime DateTime, decimal Shares, decimal PricePerShare, string Currency);

    private RowFields? TryParseCommonFields(List<string> fields, Dictionary<string, int> columnMap,
        string action, int lineNumber)
    {
        var ticker = GetField(fields, columnMap, "ticker");
        var timeStr = GetField(fields, columnMap, "time");
        var sharesStr = GetField(fields, columnMap, "no. of shares");
        var priceStr = GetField(fields, columnMap, "price / share");
        var currency = GetField(fields, columnMap, "currency (price / share)");

        if (string.IsNullOrEmpty(ticker) || string.IsNullOrEmpty(timeStr))
        {
            _logger.LogWarning("Skipping {Action} on line {Line}: missing ticker or time", action, lineNumber);
            return null;
        }

        if (!TryParseDateTime(timeStr, out var dateTime))
        {
            _logger.LogWarning("Skipping {Action} for {Ticker} on line {Line}: could not parse date '{DateStr}'",
                action, ticker, lineNumber, timeStr);
            return null;
        }
        if (!TryParseDecimal(sharesStr, out var shares))
        {
            _logger.LogWarning("Skipping {Action} for {Ticker} on line {Line}: could not parse shares '{Str}'",
                action, ticker, lineNumber, sharesStr);
            return null;
        }
        if (!TryParseDecimal(priceStr, out var price))
        {
            _logger.LogWarning("Skipping {Action} for {Ticker} on line {Line}: could not parse price '{Str}'",
                action, ticker, lineNumber, priceStr);
            return null;
        }
        if (string.IsNullOrEmpty(currency))
        {
            _logger.LogWarning("Skipping {Action} for {Ticker} on line {Line}: missing currency",
                action, ticker, lineNumber);
            return null;
        }

        var (normalizedCurrency, normalizedPrice) = NormalizeGbx(currency, price);
        return new RowFields(ticker, dateTime, shares, normalizedPrice, normalizedCurrency);
    }

    private void TryParseTrade(List<string> fields, Dictionary<string, int> columnMap,
        TradeType tradeType, List<Trade> trades, int lineNumber)
    {
        var row = TryParseCommonFields(fields, columnMap, "trade", lineNumber);
        if (row is null) return;

        if (row.Shares == 0)
        {
            _logger.LogWarning("Skipping trade for {Ticker} on line {Line}: zero quantity",
                row.Ticker, lineNumber);
            return;
        }

        var proceeds = row.Shares * row.PricePerShare;

        TryParseDecimal(GetField(fields, columnMap, "currency conversion fee"), out var conversionFee);
        var feeCurrency = GetField(fields, columnMap, "currency (currency conversion fee)") ?? "PLN";

        trades.Add(new Trade(
            Symbol: row.Ticker,
            Currency: row.Currency,
            DateTime: row.DateTime,
            Quantity: row.Shares,
            Price: row.PricePerShare,
            Proceeds: proceeds,
            Commission: Math.Abs(conversionFee),
            CommissionCurrency: feeCurrency,
            RealizedPnL: 0m,
            Type: tradeType));
    }

    private void TryParseDividend(List<string> fields, Dictionary<string, int> columnMap,
        List<RawDividend> dividends, List<RawWithholdingTax> withholdingTaxes, int lineNumber)
    {
        var row = TryParseCommonFields(fields, columnMap, "dividend", lineNumber);
        if (row is null) return;

        var grossAmount = row.Shares * row.PricePerShare;

        dividends.Add(new RawDividend(row.Ticker, row.Currency, row.DateTime.Date, grossAmount));

        var withholdingStr = GetField(fields, columnMap, "withholding tax");
        var withholdingCurrency = GetField(fields, columnMap, "currency (withholding tax)");

        if (TryParseDecimal(withholdingStr, out var withholdingAmount) && withholdingAmount != 0
            && !string.IsNullOrEmpty(withholdingCurrency))
        {
            withholdingTaxes.Add(new RawWithholdingTax(
                row.Ticker, withholdingCurrency, row.DateTime.Date, Math.Abs(withholdingAmount)));
        }
    }

    private static Dictionary<string, int> BuildColumnMap(List<string> headerFields)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerFields.Count; i++)
        {
            var name = Clean(headerFields[i]);
            if (!string.IsNullOrEmpty(name))
                map[name] = i;
        }
        return map;
    }

    private static string? GetField(List<string> fields, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var index) || index >= fields.Count)
            return null;

        var value = Clean(fields[index]);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool TryParseDateTime(string s, out DateTime result)
    {
        return DateTime.TryParseExact(s.Trim(), DateTimeFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result);
    }

    private static (string Currency, decimal Price) NormalizeGbx(string currency, decimal price)
        => currency.Equals("GBX", StringComparison.OrdinalIgnoreCase)
            ? ("GBP", price / 100m)
            : (currency, price);
}
