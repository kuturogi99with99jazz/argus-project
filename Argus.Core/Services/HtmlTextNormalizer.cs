using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Argus.Core.Services;

/// <summary>HTMLから比較対象となる本文テキストを抽出・正規化する実装</summary>
public sealed partial class HtmlTextNormalizer : IContentNormalizer
{
    private readonly HtmlParser parser = new();
    /// <summary>HTML構造の差を除き利用者に見えるテキストだけを比較可能に正規化</summary>
    public string Normalize(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var document = parser.ParseDocument(html);
        foreach (var element in document.QuerySelectorAll("script, style").ToArray())
        {
            element.Remove();
        }

        var comments = document
            .All
            .SelectMany(element => element.ChildNodes)
            .Concat(document.ChildNodes)
            .Where(node => node.NodeType == NodeType.Comment)
            .ToArray();

        foreach (var comment in comments)
        {
            comment.Parent?.RemoveChild(comment);
        }

        var text = document.DocumentElement?.TextContent ?? document.TextContent;
        return WhitespacePattern().Replace(text, " ").Trim();
    }

    /// <summary>空白の正規化規則を生成時にコンパイルして再利用</summary>
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();
}
