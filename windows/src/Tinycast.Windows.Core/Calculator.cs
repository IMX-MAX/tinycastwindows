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
        ["um"] = ("length", 0.000001), ["nm"] = ("length", 0.000000001),
        ["mi"] = ("length", 1609.344), ["mile"] = ("length", 1609.344),
        ["ft"] = ("length", 0.3048), ["in"] = ("length", 0.0254), ["yd"] = ("length", 0.9144),
        ["kg"] = ("mass", 1), ["g"] = ("mass", 0.001), ["lb"] = ("mass", 0.45359237),
        ["oz"] = ("mass", 0.028349523125), ["mg"] = ("mass", 0.000001),
        ["ton"] = ("mass", 1000), ["stone"] = ("mass", 6.35029318),
        ["c"] = ("temp", 0), ["f"] = ("temp", 0), ["k"] = ("temp", 0),
        ["l"] = ("volume", 1), ["ml"] = ("volume", 0.001), ["cl"] = ("volume", 0.01),
        ["gal"] = ("volume", 3.785411784), ["qt"] = ("volume", 0.946352946),
        ["pt"] = ("volume", 0.473176473), ["cup"] = ("volume", 0.2365882365),
        ["tbsp"] = ("volume", 0.0147867648), ["tsp"] = ("volume", 0.00492892159),
        ["m2"] = ("area", 1), ["km2"] = ("area", 1_000_000), ["cm2"] = ("area", 0.0001),
        ["ft2"] = ("area", 0.09290304), ["in2"] = ("area", 0.00064516),
        ["acre"] = ("area", 4046.8564224), ["ha"] = ("area", 10_000),
        ["mps"] = ("speed", 1), ["kph"] = ("speed", 0.2777777778),
        ["mph"] = ("speed", 0.44704), ["knot"] = ("speed", 0.5144444444),
        ["s"] = ("time", 1), ["sec"] = ("time", 1), ["min"] = ("time", 60),
        ["hr"] = ("time", 3600), ["hour"] = ("time", 3600), ["day"] = ("time", 86400),
        ["week"] = ("time", 604800), ["ms"] = ("time", 0.001),
        ["b"] = ("data", 1), ["kb"] = ("data", 1000), ["mb"] = ("data", 1_000_000),
        ["gb"] = ("data", 1_000_000_000), ["tb"] = ("data", 1_000_000_000_000),
        ["kib"] = ("data", 1024), ["mib"] = ("data", 1_048_576),
        ["gib"] = ("data", 1_073_741_824),
        ["j"] = ("energy", 1), ["kj"] = ("energy", 1000), ["cal"] = ("energy", 4.184),
        ["kcal"] = ("energy", 4184), ["wh"] = ("energy", 3600), ["kwh"] = ("energy", 3_600_000),
        ["deg"] = ("angle", Math.PI / 180), ["rad"] = ("angle", 1),
        ["px"] = ("px", 1),
    };

    public static CalcResult? Evaluate(string raw, IReadOnlyDictionary<string, double>? usdRates = null)
    {
        var query = raw.Trim();
        if (query.Length is 0 or > 256) return null;
        if (query.All(char.IsLetter)) return null;

        if (TryRadix(query, out var radix)) return radix;
        if (TryPercent(query, out var percent)) return percent;
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
        @"^\s*([+-]?(?:\d+\.?\d*|\.\d+))\s*([a-zA-Z0-9]{1,8})\s+(?:to|in)\s+([a-zA-Z0-9]{1,8})\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex PercentOf = new(
        @"^\s*([+-]?(?:\d+\.?\d*|\.\d+))\s*%\s*(?:of|\*)\s*([+-]?(?:\d+\.?\d*|\.\d+))\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex PercentChange = new(
        @"^\s*([+-]?(?:\d+\.?\d*|\.\d+))\s*([+-])\s*([+-]?(?:\d+\.?\d*|\.\d+))\s*%\s*$",
        RegexOptions.Compiled);

    static bool TryPercent(string query, out CalcResult result)
    {
        result = default;
        var of = PercentOf.Match(query);
        if (of.Success)
        {
            var percent = double.Parse(of.Groups[1].Value, CultureInfo.InvariantCulture);
            var value = double.Parse(of.Groups[2].Value, CultureInfo.InvariantCulture);
            var answer = value * percent / 100;
            var copy = Format(answer);
            result = new CalcResult(query, Group(copy), copy, false);
            return true;
        }

        var change = PercentChange.Match(query);
        if (!change.Success) return false;
        var source = double.Parse(change.Groups[1].Value, CultureInfo.InvariantCulture);
        var delta = double.Parse(change.Groups[3].Value, CultureInfo.InvariantCulture) / 100;
        var answerChange = change.Groups[2].Value == "+"
            ? source * (1 + delta)
            : source * (1 - delta);
        var changeCopy = Format(answerChange);
        result = new CalcResult(query, Group(changeCopy), changeCopy, false);
        return true;
    }

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
            if (i < expr.Length && char.IsLetter(expr[i]))
            {
                var nameStart = i;
                while (i < expr.Length && char.IsLetter(expr[i])) i++;
                var name = expr[nameStart..i].ToLowerInvariant();
                if (name == "pi") return Math.PI;
                if (name == "e") return Math.E;
                if (i >= expr.Length || expr[i] != '(') throw new FormatException();
                i++;
                var argument = ParseExpr();
                if (i >= expr.Length || expr[i] != ')') throw new FormatException();
                i++;
                return name switch
                {
                    "sqrt" => Math.Sqrt(argument),
                    "abs" => Math.Abs(argument),
                    "sin" => Math.Sin(argument),
                    "cos" => Math.Cos(argument),
                    "tan" => Math.Tan(argument),
                    "ln" => Math.Log(argument),
                    "log" => Math.Log10(argument),
                    "round" => Math.Round(argument),
                    "floor" => Math.Floor(argument),
                    "ceil" => Math.Ceiling(argument),
                    _ => throw new FormatException()
                };
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
