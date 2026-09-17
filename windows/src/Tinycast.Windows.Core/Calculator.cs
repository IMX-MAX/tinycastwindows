using System.Globalization;
using System.Text.RegularExpressions;

namespace Tinycast.Windows;

public readonly record struct CalcResult(string Expression, string Display, string CopyText, bool IsError);

/// <summary>
/// Compact Windows calculator: arithmetic, percent, hex/bin, and a handful of unit conversions.
/// Currency conversion uses injected rates (USD-based) when present.
/// </summary>
public static class Calculator
{
    static readonly Dictionary<string, (string Category, double ToBase)> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m"] = ("length", 1), ["meter"] = ("length", 1), ["meters"] = ("length", 1),
        ["km"] = ("length", 1000), ["cm"] = ("length", 0.01), ["mm"] = ("length", 0.001),
        ["mi"] = ("length", 1609.344), ["mile"] = ("length", 1609.344),
        ["ft"] = ("length", 0.3048), ["in"] = ("length", 0.0254), ["yd"] = ("length", 0.9144),
        ["kg"] = ("mass", 1), ["g"] = ("mass", 0.001), ["lb"] = ("mass", 0.45359237),
        ["oz"] = ("mass", 0.028349523125),
        ["c"] = ("temp", 0), ["f"] = ("temp", 0), ["k"] = ("temp", 0),
        ["l"] = ("vol", 1), ["ml"] = ("vol", 0.001), ["gal"] = ("vol", 3.785411784),
        ["px"] = ("px", 1),
    };

    public static CalcResult? Evaluate(string raw, IReadOnlyDictionary<string, double>? usdRates = null)
    {
        var query = raw.Trim();
        if (query.Length is 0 or > 256) return null;
        if (query.All(char.IsLetter)) return null;

        if (TryRadix(query, out var radix)) return radix;
        if (TryConversion(query, usdRates, out var converted)) return converted;

        if (!LooksLikeMath(query)) return null;
        try
        {
            var value = EvalExpression(NormalizeOps(query));
            if (double.IsNaN(value) || double.IsInfinity(value)) return null;
            var copy = Format(value);
            return new CalcResult(query, Group(copy), copy, false);
        }
        catch
        {
            return null;
        }
    }

    static bool LooksLikeMath(string query) =>
        query.Any(ch => "+-*/^%().".Contains(ch) || char.IsDigit(ch));

    static string NormalizeOps(string query) =>
        query.Replace("×", "*").Replace("÷", "/").Replace("−", "-").Replace(" ", "");

    static bool TryRadix(string query, out CalcResult result)
    {
        result = default;
        if (Regex.IsMatch(query, @"^0x[0-9a-fA-F]+$") &&
            long.TryParse(query[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
        {
            result = new CalcResult(query, Group(hex.ToString(CultureInfo.InvariantCulture)), hex.ToString(CultureInfo.InvariantCulture), false);
            return true;
        }
        if (Regex.IsMatch(query, @"^0b[01]+$") &&
            long.TryParse(query[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            var value = Convert.ToInt64(query[2..], 2);
            result = new CalcResult(query, Group(value.ToString(CultureInfo.InvariantCulture)), value.ToString(CultureInfo.InvariantCulture), false);
            return true;
        }
        return false;
    }

    static readonly Regex Conversion = new(
        @"^\s*([+-]?(?:\d+\.?\d*|\.\d+))\s*([a-zA-Z]{1,8})\s+(?:to|in)\s+([a-zA-Z]{1,8})\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static bool TryConversion(string query, IReadOnlyDictionary<string, double>? usdRates, out CalcResult result)
    {
        result = default;
        var match = Conversion.Match(query);
        if (!match.Success) return false;
        var amount = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var from = match.Groups[2].Value;
        var to = match.Groups[3].Value;

        if (usdRates is not null &&
            usdRates.TryGetValue(from.ToUpperInvariant(), out var fromRate) &&
            usdRates.TryGetValue(to.ToUpperInvariant(), out var toRate) &&
            fromRate > 0 && toRate > 0)
        {
            var usd = amount / fromRate;
            var converted = usd * toRate;
            var copy = Format(converted);
            result = new CalcResult($"{amount} {from.ToUpperInvariant()}", $"{Group(copy)} {to.ToUpperInvariant()}", copy, false);
            return true;
        }

        if (IsTemp(from) && IsTemp(to))
        {
            var kelvin = ToKelvin(amount, from);
            var converted = FromKelvin(kelvin, to);
            var copy = Format(converted);
            result = new CalcResult($"{amount} {from.ToUpperInvariant()}", $"{Group(copy)} {to.ToUpperInvariant()}", copy, false);
            return true;
        }

        if (Units.TryGetValue(from, out var src) && Units.TryGetValue(to, out var dst) && src.Category == dst.Category && src.Category != "temp")
        {
            var converted = amount * src.ToBase / dst.ToBase;
            var copy = Format(converted);
            result = new CalcResult($"{amount} {from}", $"{Group(copy)} {to}", copy, false);
            return true;
        }

        result = new CalcResult(query, $"Can't convert {from} to {to}", "", true);
        return true;
    }

    static bool IsTemp(string unit) => unit.Equals("c", StringComparison.OrdinalIgnoreCase)
        || unit.Equals("f", StringComparison.OrdinalIgnoreCase)
        || unit.Equals("k", StringComparison.OrdinalIgnoreCase);

    static double ToKelvin(double value, string unit) => unit.ToLowerInvariant() switch
    {
        "c" => value + 273.15,
        "f" => (value - 32) * 5 / 9 + 273.15,
        _ => value
    };

    static double FromKelvin(double kelvin, string unit) => unit.ToLowerInvariant() switch
    {
        "c" => kelvin - 273.15,
        "f" => (kelvin - 273.15) * 9 / 5 + 32,
        _ => kelvin
    };

    static double EvalExpression(string expr)
    {
        var i = 0;
        double ParseExpr()
        {
            var left = ParseTerm();
            while (i < expr.Length && (expr[i] is '+' or '-'))
            {
                var op = expr[i++];
                var right = ParseTerm();
                left = op == '+' ? left + right : left - right;
            }
            return left;
        }

        double ParseTerm()
        {
            var left = ParsePower();
            while (i < expr.Length && (expr[i] is '*' or '/' or '%'))
            {
                var op = expr[i++];
                var right = ParsePower();
                left = op switch
                {
                    '*' => left * right,
                    '/' => left / right,
                    _ => left % right
                };
            }
            return left;
        }

        double ParsePower()
        {
            var left = ParseFactor();
            if (i < expr.Length && expr[i] == '^')
            {
                i++;
                return Math.Pow(left, ParsePower());
            }
            return left;
        }

        double ParseFactor()
        {
            if (i < expr.Length && expr[i] == '+') { i++; return ParseFactor(); }
            if (i < expr.Length && expr[i] == '-') { i++; return -ParseFactor(); }
            if (i < expr.Length && expr[i] == '(')
            {
                i++;
                var inner = ParseExpr();
                if (i < expr.Length && expr[i] == ')') i++;
                return inner;
            }
            var start = i;
            while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
            if (start == i) throw new FormatException();
            return double.Parse(expr[start..i], CultureInfo.InvariantCulture);
        }

        var value = ParseExpr();
        if (i != expr.Length) throw new FormatException();
        return value;
    }

    static string Format(double value)
    {
        if (Math.Abs(value - Math.Round(value)) < 1e-9)
            return Math.Round(value).ToString(CultureInfo.InvariantCulture);
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    static string Group(string text)
    {
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            return text;
        return number.ToString("#,0.######", CultureInfo.InvariantCulture);
    }
}
