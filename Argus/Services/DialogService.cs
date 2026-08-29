using Avalonia.Controls;
using Argus.ViewModels;
using Argus.Views;
using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Services;

/// <summary>Avaloniaウィンドウを生成してViewModelへ結果だけを返すダイアログ実装</summary>
public sealed class DialogService(
    Func<Window?> ownerProvider,
    IClipboardService clipboardService) : IDialogService
{
    private readonly Func<Window?> ownerProvider = ownerProvider ??
        throw new ArgumentNullException(nameof(ownerProvider));
    private readonly IClipboardService clipboardService = clipboardService ??
        throw new ArgumentNullException(nameof(clipboardService));

    /// <summary>追加または編集ViewModelをモーダル画面へ接続</summary>
    public async Task<WatchTarget?> ShowTargetEditorAsync(
        WatchTarget? target,
        Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync,
        CancellationToken cancellationToken)
    {
        var owner = GetOwner();
        TargetEditWindow? window = null;
        var viewModel = new TargetEditViewModel(
            target,
            saveAsync,
            cancellationToken,
            (url, token) => ShowCssSelectorPromptAsync(window, url, token));
        window = new TargetEditWindow(viewModel);
        using var registration = cancellationToken.Register(() => window.Close(null));
        return await window.ShowDialog<WatchTarget?>(owner);
    }

    /// <summary>削除対象名と保存済みデータへの影響を明示して確認</summary>
    public async Task<bool> ConfirmDeleteAsync(
        WatchTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        var window = new MessageDialogWindow(
            "監視対象の削除",
            $"「{target.Name}」を削除しますか？\n保存済みの前回チェックデータも削除されます。",
            true);
        using var registration = cancellationToken.Register(() => window.Close(false));
        return await window.ShowDialog<bool>(GetOwner());
    }

    /// <summary>操作エラーをプラットフォーム非依存のモーダル画面で通知</summary>
    public async Task ShowErrorAsync(string message, CancellationToken cancellationToken)
    {
        var window = new MessageDialogWindow("Argus", message, false);
        using var registration = cancellationToken.Register(() => window.Close(false));
        await window.ShowDialog<bool>(GetOwner());
    }

    /// <summary>Coreの差分結果を専用ViewModelへ渡してモーダル表示</summary>
    public async Task ShowContentDiffAsync(
        WatchTarget target,
        ContentDiff diff,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(diff);
        var window = new ContentDiffWindow(new ContentDiffDialogViewModel(target, diff));
        using var registration = cancellationToken.Register(() => window.Close());
        await window.ShowDialog<bool>(GetOwner());
    }

    /// <summary>現在の編集画面を所有者としてCSSセレクタ相談小窓を表示</summary>
    private async Task ShowCssSelectorPromptAsync(
        Window? editorWindow,
        string url,
        CancellationToken cancellationToken)
    {
        var window = new CssSelectorPromptWindow(
            new CssSelectorPromptViewModel(url, clipboardService, cancellationToken));
        using var registration = cancellationToken.Register(() => window.Close());
        await window.ShowDialog<bool>(editorWindow ?? GetOwner());
    }

    /// <summary>モーダル画面の所有者が失われた場合を明示的なエラーとして検出</summary>
    private Window GetOwner() =>
        ownerProvider() ?? throw new InvalidOperationException("メイン画面が利用できません。");
}
