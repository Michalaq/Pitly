using Pitly.Broker.InteractiveBrokers;
using Pitly.Broker.Trading212;
using Pitly.Core.Models;
using Pitly.Core.Parsing;

namespace Pitly.Api.Services;

public class StatementParserDispatcher : IStatementParser
{
    private readonly InteractiveBrokersStatementParser _ibParser;
    private readonly Trading212StatementParser _t212Parser;

    public StatementParserDispatcher(
        InteractiveBrokersStatementParser ibParser,
        Trading212StatementParser t212Parser)
    {
        _ibParser = ibParser;
        _t212Parser = t212Parser;
    }

    public ParsedStatement Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new FormatException("File is empty.");

        var firstLine = content.AsSpan();
        var newlineIndex = content.IndexOfAny('\n', '\r');
        if (newlineIndex > 0)
            firstLine = content.AsSpan(0, newlineIndex);

        if (firstLine.StartsWith("Action,", StringComparison.OrdinalIgnoreCase))
            return _t212Parser.Parse(content);

        if (firstLine.Contains("Statement,".AsSpan(), StringComparison.Ordinal) ||
            firstLine.Contains("Trades,".AsSpan(), StringComparison.Ordinal) ||
            firstLine.Contains("Dividends,".AsSpan(), StringComparison.Ordinal) ||
            firstLine.Contains("Withholding Tax,".AsSpan(), StringComparison.Ordinal))
            return _ibParser.Parse(content);

        throw new FormatException(
            "Unrecognized file format. Please upload an Interactive Brokers or Trading 212 CSV export.");
    }
}
