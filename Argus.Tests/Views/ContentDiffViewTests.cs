namespace Argus.Tests.Views;

/// <summary>差分ダイアログが文字列内の変更箇所を視認できる表示契約を持つことを検証するテスト。</summary>
public sealed class ContentDiffViewTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    /// <summary>差分ダイアログが前回・今回のセグメント一覧と折り返しを使用することを検証。</summary>
    [Fact]
    public void ContentDiffWindow_RendersInlineSegmentsWithWrapAndScroll()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "Views", "ContentDiffWindow.axaml"));

        Assert.Contains("PreviousSegments", xaml, StringComparison.Ordinal);
        Assert.Contains("CurrentSegments", xaml, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AppDiffInlineChangedBackgroundBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("MaxWidth=\"760\"", xaml, StringComparison.Ordinal);
        Assert.Contains("BorderThickness=\"0,0,0,1\"", xaml, StringComparison.Ordinal);
        Assert.Contains("変更部分", xaml, StringComparison.Ordinal);
    }

    /// <summary>差分リソースがライトテーマとダークテーマの両方に定義されることを検証。</summary>
    [Fact]
    public void AppStyles_DefineInlineDiffResourcesForBothThemes()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "App.axaml"));

        Assert.Equal(2, CountOccurrences(xaml, "AppDiffInlineChangedBackgroundBrush"));
        Assert.Equal(2, CountOccurrences(xaml, "AppDiffInlineChangedForegroundBrush"));
    }

    /// <summary>指定文字列の出現回数を数える補助処理</summary>
    private static int CountOccurrences(string value, string term)
    {
        var count = 0;
        var startIndex = 0;
        while ((startIndex = value.IndexOf(term, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += term.Length;
        }

        return count;
    }
}
