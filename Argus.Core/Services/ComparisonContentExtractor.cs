using AngleSharp.Html.Parser;
using Argus.Core.Models;

namespace Argus.Core.Services;

/// <summary>監視モードに応じてHTML全体、本文テキスト、選択要素を抽出する機能</summary>
public sealed class ComparisonContentExtractor : IComparisonContentExtractor
{
    private readonly IContentNormalizer htmlTextNormalizer;
    private readonly HtmlParser parser = new();

    /// <summary>既存の本文正規化規則を再利用しながらモード選択を集約</summary>
    public ComparisonContentExtractor(IContentNormalizer htmlTextNormalizer)
    {
        this.htmlTextNormalizer = htmlTextNormalizer
            ?? throw new ArgumentNullException(nameof(htmlTextNormalizer));
    }

    /// <summary>監視対象に保存された比較条件を一つの比較文字列へ変換</summary>
    public string Extract(WatchTarget target, string html)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(html);

        return target.Mode switch
        {
            WatchMode.HtmlText => htmlTextNormalizer.Normalize(html),
            WatchMode.HtmlWhole => html,
            WatchMode.CssSelector => ExtractSelectedElements(html, target.CssSelector),
            _ => throw new InvalidDataException("対応していない監視モードです。")
        };
    }

    /// <summary>一致しない条件を正常な空文字として保存しないため選択結果を検証</summary>
    private string ExtractSelectedElements(string html, string? cssSelector)
    {
        if (string.IsNullOrWhiteSpace(cssSelector))
        {
            throw new InvalidDataException("CSSセレクタが指定されていません。");
        }

        try
        {
            var document = parser.ParseDocument(html);
            var elements = document.QuerySelectorAll(cssSelector);
            if (elements.Length == 0)
            {
                throw new InvalidDataException("CSSセレクタに一致する要素がありません。");
            }

            return string.Join('\n', elements.Select(element => element.OuterHtml));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("CSSセレクタが正しくありません。", exception);
        }
    }
}
