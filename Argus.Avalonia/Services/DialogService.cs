using Avalonia.Controls;
using Argus.Avalonia.ViewModels;
using Argus.Avalonia.Views;
using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Avalonia.Services;

/// <summary>Avaloniaウィンドウを生成してViewModelへ結果だけを返すダイアログ実装</summary>
public sealed class DialogService(Func<Window?> ownerProvider) : IDialogService
{
    /// <summary>追加または編集ViewModelをモーダル画面へ接続</summary>
    public async Task<WatchTarget?> ShowTargetEditorAsync(
        WatchTarget? target,
        Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync,
        CancellationToken cancellationToken)
    {
        var owner = GetOwner();
        var viewModel = new TargetEditViewModel(target, saveAsync, cancellationToken);
        var window = new TargetEditWindow(viewModel);
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

    /// <summary>モーダル画面の所有者が失われた場合を明示的なエラーとして検出</summary>
    private Window GetOwner() =>
        ownerProvider() ?? throw new InvalidOperationException("メイン画面が利用できません。");
}
