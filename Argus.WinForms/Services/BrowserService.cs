using System.ComponentModel;
using System.Diagnostics;
using Argus.Core.Models;

namespace Argus.WinForms.Services;

/// <summary>ブラウザー起動の成否と失敗理由</summary>
public sealed record BrowserOpenResult(bool IsSuccess, string? ErrorMessage)
{
    /// <summary>成功時の戻り値生成を呼び出し側で統一するためのファクトリ</summary>
    public static BrowserOpenResult Success { get; } = new(true, null);
    /// <summary>失敗情報の生成を呼び出し側で統一するためのファクトリ</summary>
    public static BrowserOpenResult Failure(string message) => new(false, message);
}


/// <summary>監視対象URLを既定のブラウザーで開くサービス</summary>
public sealed class BrowserService
{
    /// <summary>検証済みURLだけをOSの既定ブラウザーへ安全に引き渡し</summary>
    public BrowserOpenResult Open(WatchTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!WatchTargetValidator.TryCreateHttpUri(target.Url.AbsoluteUri, out var uri) ||
            uri is null)
        {
            return BrowserOpenResult.Failure("URLが正しくないためブラウザで開けません。");
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
            return BrowserOpenResult.Success;
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            return BrowserOpenResult.Failure(
                "既定のブラウザを開けませんでした。Windowsの関連付けを確認してください。");
        }
    }
}
