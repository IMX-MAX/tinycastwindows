using Tinycast.Windows;

namespace Tinycast.Windows.Tests;

public class FuzzyMatchTests
{
    [Fact]
    public void Exact_outranks_prefix()
    {
        var exact = FuzzyMatch.MatchQuery("code", "code")!.Value.Score;
        var prefix = FuzzyMatch.MatchQuery("code", "codeberg")!.Value.Score;
        Assert.True(exact > prefix);
    }

    [Fact]
    public void Word_start_beats_substring()
    {
        var word = FuzzyMatch.MatchQuery("note", "sticky notes")!.Value.Score;
        var sub = FuzzyMatch.MatchQuery("note", "denoted")!.Value.Score;
        Assert.True(word > sub);
    }

    [Fact]
    public void Subsequence_matches_scattered_letters()
    {
        Assert.NotNull(FuzzyMatch.MatchQuery("ff", "Firefox"));
        Assert.Null(FuzzyMatch.MatchQuery("zzz", "Firefox"));
    }
}

public class CalculatorTests
{
    [Fact]
    public void Arithmetic()
    {
        var result = Calculator.Evaluate("3+4*2");
        Assert.NotNull(result);
        Assert.False(result!.Value.IsError);
        Assert.Equal("11", result.Value.CopyText);
    }

    [Fact]
    public void Hex_literal()
    {
        var result = Calculator.Evaluate("0xff");
        Assert.Equal("255", result!.Value.CopyText);
    }

    [Fact]
    public void Length_conversion()
    {
        var result = Calculator.Evaluate("10 km to mi");
        Assert.NotNull(result);
        Assert.False(result!.Value.IsError);
        Assert.Contains("mi", result.Value.Display, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Letters_only_is_not_math() =>
        Assert.Null(Calculator.Evaluate("firefox"));
}

public class WindowPlacementTests
{
    [Fact]
    public void Left_half_uses_the_work_area()
    {
        var screen = new Rect(0, 0, 1920, 1080);
        var next = WindowPlacement.Apply(WindowCommand.LeftHalf, new Rect(10, 10, 800, 600), screen);
        Assert.Equal(new Rect(0, 0, 960, 1080), next);
    }

    [Fact]
    public void Center_keeps_size()
    {
        var screen = new Rect(0, 0, 1000, 1000);
        var window = new Rect(0, 0, 200, 100);
        var next = WindowPlacement.Apply(WindowCommand.Center, window, screen);
        Assert.Equal(400, next.X);
        Assert.Equal(450, next.Y);
        Assert.Equal(200, next.Width);
    }
}

public class PaletteSearchTests
{
    [Fact]
    public void Ranking_lifts_a_used_app()
    {
        var entries = new[]
        {
            new PaletteEntry("app:zeta", "Zeta", "Application", EntryKind.App),
            new PaletteEntry("app:alpha", "Alpha", "Application", EntryKind.App),
        };
        var ranking = new Dictionary<string, int> { ["app:zeta"] = 4 };
        var ranked = PaletteSearch.Rank("", entries, ranking);
        Assert.Equal("Zeta", ranked[0].Title);
    }
}

public class PlaceholderTests
{
    [Fact]
    public void Expands_query_and_clipboard()
    {
        var text = PlaceholderExpander.Expand("https://example.com/{query}?c={clipboard}", "hello world", "clip");
        Assert.Equal("https://example.com/hello world?c=clip", text);
    }
}

public class MistralTests
{
    [Fact]
    public void Decode_delta_reads_content()
    {
        var json = """{"choices":[{"delta":{"content":"Hi"}}]}""";
        Assert.Equal(["Hi"], MistralClient.DecodeDelta(json));
    }

    [Fact]
    public void Preamble_off_sends_nothing() =>
        Assert.Null(AiPreamble.Compose(false, "hello"));

    [Fact]
    public void Preamble_on_includes_user_text()
    {
        var text = AiPreamble.Compose(true, "Be brief");
        Assert.Contains("Tinycast for Windows", text);
        Assert.Contains("Be brief", text);
        Assert.Contains("Mistral", text);
    }

    [Fact]
    public void Build_messages_puts_system_first()
    {
        var messages = MistralClient.BuildMessages(
            [new ChatMessage { Role = "user", Content = "Hi" }],
            "sys");
        Assert.Equal(2, messages.Count);
    }
}
