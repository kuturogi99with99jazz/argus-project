using Argus.Core.Models;

namespace Argus.Core.Services;

/// <summary>監視モード固有の規則でHTMLから比較対象を取り出す契約</summary>
public interface IComparisonContentExtractor
{
    /// <summary>取得とハッシュ化から比較範囲の選択責務を分離</summary>
    string Extract(WatchTarget target, string html);
}
