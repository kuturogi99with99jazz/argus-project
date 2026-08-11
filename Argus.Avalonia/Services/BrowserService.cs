using System.ComponentModel;
using System.Diagnostics;
using Argus.Core.Models;

namespace Argus.Avalonia.Services;

/// <summary>WindowsとmacOSのシェルを通じて既定ブラウザを起動するサービス</summary>
public sealed class BrowserService : IBrowserService
{
    /// <summary>HTTPまたはHTTPSの検証済みURLだけをOSへ引き渡し</summary>
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
            exception is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            return BrowserOpenResult.Failure(
                "既定のブラウザを開けませんでした。OSの関連付けを確認してください。");
        }
    }
}
