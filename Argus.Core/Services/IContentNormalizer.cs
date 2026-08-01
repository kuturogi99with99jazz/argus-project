namespace Argus.Core.Services;

/// <summary>取得したHTMLを比較可能な文字列へ正規化する契約</summary>
public interface IContentNormalizer
{
    /// <summary>比較対象の抽出規則を取得処理やハッシュ処理から分離</summary>
    string Normalize(string html);
}
