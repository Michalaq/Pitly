using Pitly.Core.Models;
using Pitly.Core.Tax;

namespace Pitly.Tests;

public class TaxCalculatorTests
{
    [Fact]
    public async Task CalculateAsync_UsesHistoricalTradesButOnlyCountsTargetYear()
    {
        var rateService = new TestRateService(new Dictionary<string, decimal>
        {
            ["USD"] = 1m
        });
        var calculator = new TaxCalculator(
            new CapitalGainsTaxCalculator(rateService),
            new DividendTaxCalculator(rateService));

        var statement = new ParsedStatement(
            Trades:
            [
                new(
                    Symbol: "ABC",
                    Currency: "USD",
                    DateTime: new DateTime(2024, 12, 20, 10, 0, 0),
                    Quantity: 10m,
                    Price: 10m,
                    Proceeds: 100m,
                    Commission: 0m,
                    CommissionCurrency: "USD",
                    RealizedPnL: 0m,
                    Type: TradeType.Buy,
                    Isin: "US1111111111"),
                new(
                    Symbol: "ABC",
                    Currency: "USD",
                    DateTime: new DateTime(2025, 3, 10, 10, 0, 0),
                    Quantity: 5m,
                    Price: 20m,
                    Proceeds: 100m,
                    Commission: 0m,
                    CommissionCurrency: "USD",
                    RealizedPnL: 0m,
                    Type: TradeType.Sell,
                    Isin: "US1111111111")
            ],
            Dividends:
            [
                new("ABC", "USD", new DateTime(2024, 6, 1), 1m, "US1111111111"),
                new("ABC", "USD", new DateTime(2025, 6, 1), 2m, "US1111111111")
            ],
            WithholdingTaxes:
            [
                new("ABC", "USD", new DateTime(2024, 6, 1), 0.1m, "US1111111111"),
                new("ABC", "USD", new DateTime(2025, 6, 1), 0.3m, "US1111111111")
            ]);

        var summary = await calculator.CalculateAsync(statement, targetYear: 2025);

        Assert.Equal(2025, summary.Year);
        Assert.Equal(100m, summary.TotalProceedsPln);
        Assert.Equal(50m, summary.TotalCostPln);
        Assert.Equal(50m, summary.CapitalGainPln);
        Assert.Equal(9.5m, summary.CapitalGainTaxPln);
        Assert.Equal(2m, summary.TotalDividendsPln);
        Assert.Equal(0.3m, summary.TotalWithholdingPln);
        Assert.Equal(0.08m, summary.DividendTaxOwedPln);

        var sell = Assert.Single(summary.TradeResults);
        Assert.Equal(TradeType.Sell, sell.Type);
        Assert.Single(summary.Dividends);
    }
}
