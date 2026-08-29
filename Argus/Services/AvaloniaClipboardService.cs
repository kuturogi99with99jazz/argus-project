using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace Argus.Services;

/// <summary>Avaloniaのトップレベル画面を通じてOSクリップボードへテキストをコピーするサービス</summary>
public sealed class AvaloniaClipboardService(Func<Window?> ownerProvider) : IClipboardService
{
    private readonly Func<Window?> ownerProvider = ownerProvider ??
        throw new ArgumentNullException(nameof(ownerProvider));

    /// <summary>アクティブなAvalonia画面のクリップボードへテキストを設定</summary>
    public async Task CopyTextAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        var owner = ownerProvider() ??
            throw new InvalidOperationException("クリップボードを利用できる画面がありません。");
        var clipboard = TopLevel.GetTopLevel(owner)?.Clipboard ??
            throw new InvalidOperationException("クリップボードを利用できません。");

        await clipboard.SetTextAsync(text);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
