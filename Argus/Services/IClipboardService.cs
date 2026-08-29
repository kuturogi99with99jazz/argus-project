namespace Argus.Services;

/// <summary>UIからOSクリップボードへテキストを渡すためのサービス境界</summary>
public interface IClipboardService
{
    /// <summary>指定したテキストをOSクリップボードへ設定</summary>
    Task CopyTextAsync(string text, CancellationToken cancellationToken);
}
