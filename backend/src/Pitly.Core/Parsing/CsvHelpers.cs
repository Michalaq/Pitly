using System.Globalization;
using System.Text;

namespace Pitly.Core.Parsing;

public static class CsvHelpers
{
    public static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }

    public static string Clean(string value) => value.Trim().Trim('"').Trim();

    public static bool TryParseDecimal(string? s, out decimal result)
    {
        result = 0;
        if (string.IsNullOrEmpty(s)) return false;
        return decimal.TryParse(s.Replace(",", ""), NumberStyles.Any,
            CultureInfo.InvariantCulture, out result);
    }
}
