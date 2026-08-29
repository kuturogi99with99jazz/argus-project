using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>監視モードごとの比較対象抽出規則を検証するテスト</summary>
public sealed class ComparisonContentExtractorTests
{
    /// <summary>三つの比較方式が差分入力となる比較文字列をそれぞれ生成することを検証</summary>
    [Theory]
    [InlineData(WatchMode.HtmlText, null, "<html><body>  Visible  </body></html>", "Visible")]
    [InlineData(WatchMode.HtmlWhole, null, "<p>Whole</p>", "<p>Whole</p>")]
    [InlineData(WatchMode.CssSelector, ".item", "<p class='item'>Selected</p><p>Ignore</p>", "<p class=\"item\">Selected</p>")]
    public void Extract_WhenModeIsSelected_ReturnsComparisonContentForDiff(
        WatchMode mode,
        string? selector,
        string html,
        string expected)
    {
        var extractor = new ComparisonContentExtractor(new HtmlTextNormalizer());

        var result = extractor.Extract(CreateTarget(mode, selector), html);

        Assert.Equal(expected, result);
    }

    /// <summary>HTML全体比較で取得文字列が加工されないことを検証</summary>
    [Fact]
    public void Extract_WhenModeIsHtmlWhole_ReturnsOriginalHtml()
    {
        const string html = "  <!-- comment -->\r\n<P data-id=\"1\">Text</P>  ";
        var extractor = new ComparisonContentExtractor(new HtmlTextNormalizer());

        var result = extractor.Extract(CreateTarget(WatchMode.HtmlWhole), html);

        Assert.Equal(html, result);
    }

    /// <summary>CSSセレクタ比較で一致要素だけが文書順に抽出されることを検証</summary>
    [Fact]
    public void Extract_WhenModeIsCssSelector_ReturnsMatchingOuterHtmlInDocumentOrder()
    {
        const string html = "<main><p class='item'>First</p><aside>Ignore</aside><p class='item'>Second</p></main>";
        var extractor = new ComparisonContentExtractor(new HtmlTextNormalizer());

        var result = extractor.Extract(CreateTarget(WatchMode.CssSelector, ".item"), html);

        Assert.Equal("<p class=\"item\">First</p>\n<p class=\"item\">Second</p>", result);
    }

    /// <summary>CSSセレクタ外の変更が比較文字列へ影響しないことを検証</summary>
    [Fact]
    public void Extract_WhenOnlyContentOutsideSelectorChanges_ReturnsSameContent()
    {
        var extractor = new ComparisonContentExtractor(new HtmlTextNormalizer());
        var target = CreateTarget(WatchMode.CssSelector, "#target");

        var first = extractor.Extract(target, "<div id='target'>Keep</div><p>Before</p>");
        var second = extractor.Extract(target, "<div id='target'>Keep</div><p>After</p>");

        Assert.Equal(first, second);
    }

    /// <summary>未指定・不正・一致なしのセレクタを保存可能な比較値にしないことを検証</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[")]
    [InlineData(".missing")]
    public void Extract_WhenCssSelectorCannotSelectContent_ThrowsInvalidDataException(string? selector)
    {
        var extractor = new ComparisonContentExtractor(new HtmlTextNormalizer());

        Assert.Throws<InvalidDataException>(
            () => extractor.Extract(CreateTarget(WatchMode.CssSelector, selector), "<p>Text</p>"));
    }

    /// <summary>テスト対象の比較条件だけを切り替えた監視対象を生成</summary>
    private static WatchTarget CreateTarget(WatchMode mode, string? cssSelector = null) =>
        new(
            Guid.NewGuid(),
            "Sample",
            new Uri("https://example.com/"),
            mode,
            true,
            null,
            null,
            cssSelector);
}
