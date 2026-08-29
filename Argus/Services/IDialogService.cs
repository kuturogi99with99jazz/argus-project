using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Services;

/// <summary>ViewModelからウィンドウ生成と利用者確認を分離するUIサービス境界</summary>
public interface IDialogService
{
    /// <summary>追加または編集画面を表示し保存成功したCoreモデルだけを返却</summary>
    Task<WatchTarget?> ShowTargetEditorAsync(
        WatchTarget? target,
        Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync,
        CancellationToken cancellationToken);

    /// <summary>保存済みチェック情報を含む削除について利用者の確認を取得</summary>
    Task<bool> ConfirmDeleteAsync(WatchTarget target, CancellationToken cancellationToken);

    /// <summary>操作を継続できないエラーを利用者へ通知</summary>
    Task ShowErrorAsync(string message, CancellationToken cancellationToken);

    /// <summary>更新あり結果の差分をモーダル画面で利用者へ表示</summary>
    Task ShowContentDiffAsync(
        WatchTarget target,
        ContentDiff diff,
        CancellationToken cancellationToken);
}
