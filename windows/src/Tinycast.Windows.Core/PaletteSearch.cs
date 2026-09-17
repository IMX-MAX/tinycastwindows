namespace Tinycast.Windows;

public static class PaletteSearch
{
    public static IReadOnlyList<PaletteEntry> Rank(
        string query,
        IEnumerable<PaletteEntry> entries,
        IReadOnlyDictionary<string, int> ranking)
    {
        var q = new FuzzyMatch.Query(query);
        if (q.IsEmpty)
        {
            return entries
                .OrderByDescending(e => RankingStore.Score(ranking, e.Id))
                .ThenBy(e => KindOrder(e.Kind))
                .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToList();
        }

        var scored = new List<(PaletteEntry Entry, int Score, FuzzyMatch.Tier Tier)>();
        foreach (var entry in entries)
        {
            var title = FuzzyMatch.MatchQuery(q, entry.Title);
            var sub = string.IsNullOrEmpty(entry.Subtitle) ? null : FuzzyMatch.MatchQuery(q, entry.Subtitle);
            if (title is null && sub is null) continue;
            var best = title?.Score ?? int.MinValue;
            var tier = title?.Tier ?? FuzzyMatch.Tier.Subsequence;
            if (sub is { } s && s.Score > best)
            {
                best = s.Score - 5_000;
                tier = s.Tier;
            }
            scored.Add((entry, best + RankingStore.Score(ranking, entry.Id), tier));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => KindOrder(x.Entry.Kind))
            .ThenBy(x => x.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Entry)
            .Take(40)
            .ToList();
    }

    static int KindOrder(EntryKind kind) => kind switch
    {
        EntryKind.Favorite => 0,
        EntryKind.App => 1,
        EntryKind.Command => 2,
        EntryKind.Quicklink => 3,
        EntryKind.Snippet => 4,
        EntryKind.Note => 5,
        EntryKind.SystemAction => 6,
        EntryKind.Window => 7,
        _ => 8
    };
}
