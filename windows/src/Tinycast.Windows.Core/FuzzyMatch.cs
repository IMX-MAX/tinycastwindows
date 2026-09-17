namespace Tinycast.Windows;

/// <summary>
/// Port of the macOS launcher's <c>FuzzyMatch</c>: exact / prefix / word-start / substring / subsequence.
/// </summary>
public static class FuzzyMatch
{
    public enum Tier
    {
        Exact,
        Prefix,
        WordStart,
        Substring,
        Subsequence
    }

    public readonly record struct Match(Tier Tier, int Offset, int QueryLength, int CandidateLength, int Spread)
    {
        public int Score => RawScore(this);
    }

    public readonly record struct Query
    {
        public string Text { get; }
        public IReadOnlyList<char> Characters { get; }
        public bool IsEmpty => Text.Length == 0;

        public Query(string raw)
        {
            Text = Normalize(raw);
            Characters = Text.ToCharArray();
        }
    }

    public static Match? MatchQuery(string query, string candidate) => MatchQuery(new Query(query), candidate);

    public static Match? MatchQuery(Query query, string candidate)
    {
        var q = query.Text;
        var c = Normalize(candidate);
        var length = c.Length;
        if (q.Length == 0)
            return new Match(Tier.Exact, 0, 0, length, 0);
        if (c == q)
            return new Match(Tier.Exact, 0, query.Characters.Count, length, 0);
        if (c.StartsWith(q, StringComparison.Ordinal))
            return new Match(Tier.Prefix, 0, query.Characters.Count, length, 0);
        var index = c.IndexOf(q, StringComparison.Ordinal);
        if (index >= 0)
        {
            var tier = IsWordStart(c, index) ? Tier.WordStart : Tier.Substring;
            return new Match(tier, index, query.Characters.Count, length, 0);
        }
        var spread = SubsequenceScore(query.Characters, c);
        return spread is int s
            ? new Match(Tier.Subsequence, 0, query.Characters.Count, length, s)
            : null;
    }

    public static int? Score(string query, string candidate) =>
        MatchQuery(query, candidate)?.Score;

    public static int? Score(Query query, string candidate) =>
        MatchQuery(query, candidate)?.Score;

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    internal static int RawScore(Match match) => match.Tier switch
    {
        Tier.Exact => 100_000,
        Tier.Prefix => 90_000 - match.CandidateLength,
        Tier.WordStart => 80_000 - match.CandidateLength,
        Tier.Substring => 70_000 - match.CandidateLength,
        _ => match.Spread
    };

    static bool IsWordStart(string candidate, int index)
    {
        if (index == 0) return true;
        var prev = candidate[index - 1];
        return !char.IsLetterOrDigit(prev);
    }

    static int? SubsequenceScore(IReadOnlyList<char> query, string candidate)
    {
        var qi = 0;
        var last = -1;
        var spread = 40_000;
        for (var i = 0; i < candidate.Length && qi < query.Count; i++)
        {
            if (candidate[i] != query[qi]) continue;
            if (last >= 0) spread -= i - last;
            if (i == 0 || !char.IsLetterOrDigit(candidate[i - 1])) spread += 12;
            last = i;
            qi++;
        }
        return qi == query.Count ? Math.Max(1, spread) : null;
    }
}
