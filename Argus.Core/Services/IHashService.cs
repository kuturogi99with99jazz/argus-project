namespace Argus.Core.Services;

/// <summary>正規化済みコンテンツから比較用ハッシュを生成する契約</summary>
public interface IHashService
{
    /// <summary>内容比較に用いるハッシュ方式を確認処理から分離</summary>
    string Compute(string content);
}
