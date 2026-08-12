using Argus.Core.Models;

namespace Argus.Services;

/// <summary>ブラウザ起動結果をOS固有例外から分離して表す値</summary>
public sealed record BrowserOpenResult(bool IsSuccess, string? ErrorMessage)
{
    /// <summary>成功結果を割り当てなしで再利用</summary>
    public static BrowserOpenResult Success { get; } = new(true, null);

    /// <summary>利用者向けメッセージを保持する失敗結果を生成</summary>
    public static BrowserOpenResult Failure(string message) => new(false, message);
}

/// <summary>検証済み監視対象をOSの既定ブラウザへ渡すUIサービス境界</summary>
public interface IBrowserService
{
    /// <summary>対象URLを既定ブラウザで開きOS固有失敗を結果へ変換</summary>
    BrowserOpenResult Open(WatchTarget target);
}
