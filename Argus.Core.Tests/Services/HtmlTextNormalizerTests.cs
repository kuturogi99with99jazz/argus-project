using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>HTMLテキストの正規化規則を検証するテスト</summary>
public sealed class HtmlTextNormalizerTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Normalize_RemovesScriptStyleAndComments()
    {
        const string html = """
            <html>
              <style>.hidden { display:none; }</style>
              <body>Hello<!-- comment --><script>dynamic()</script> World</body>
            </html>
            """;
        var normalizer = new HtmlTextNormalizer();

        var result = normalizer.Normalize(html);

        Assert.Equal("Hello World", result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Normalize_DecodesEntitiesAndCollapsesWhitespace()
    {
        const string html = "<p>  Fish &amp;   Chips\r\n Today </p>";
        var normalizer = new HtmlTextNormalizer();

        var result = normalizer.Normalize(html);

        Assert.Equal("Fish & Chips Today", result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Normalize_PreservesMeaningfulTextChanges()
    {
        var normalizer = new HtmlTextNormalizer();

        var today = normalizer.Normalize("<p>今日は更新です</p>");
        var tomorrow = normalizer.Normalize("<p>明日は更新です</p>");

        Assert.NotEqual(today, tomorrow);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Normalize_HandlesIncompleteHtml()
    {
        var normalizer = new HtmlTextNormalizer();

        var result = normalizer.Normalize("<main><p>本文");

        Assert.Equal("本文", result);
    }
}
