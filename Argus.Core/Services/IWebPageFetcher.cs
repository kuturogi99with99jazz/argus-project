namespace Argus.Core.Services;

/// <summary>HTTP経由で監視対象ページを取得する契約</summary>
public interface IWebPageFetcher
{
    /// <summary>HTTP実装を差し替え可能にして取得処理を実サイトから分離</summary>
    Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken);
}
