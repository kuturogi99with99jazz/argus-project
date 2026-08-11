using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Argus.Avalonia.ViewModels;
using Argus.Core.Services;

namespace Argus.Avalonia.Views;

/// <summary>監視対象の追加と編集で共有する入力ウィンドウ</summary>
public sealed partial class TargetEditWindow : Window
{
    private readonly TargetEditViewModel viewModel;

    /// <summary>XAMLツールがウィンドウ資源を読み込むためのデザイン時コンストラクター</summary>
    public TargetEditWindow()
        : this(new TargetEditViewModel(
            null,
            (_, _) => Task.FromResult(WatchTargetChangeResult.Failure(
                "デザイン時には保存できません。")),
            CancellationToken.None))
    {
    }

    /// <summary>編集ViewModelをバインドし保存とキャンセルの終了結果を接続</summary>
    public TargetEditWindow(TargetEditViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
        viewModel.Saved += ViewModel_Saved;
        viewModel.CancelRequested += ViewModel_CancelRequested;
        Closed += Window_Closed;
    }

    /// <summary>保存成功したCoreモデルをモーダル呼び出し元へ返却</summary>
    private void ViewModel_Saved(object? sender, EventArgs eventArgs) =>
        Close(viewModel.SavedTarget);

    /// <summary>入力変更を保存せずモーダル画面を閉じる</summary>
    private void ViewModel_CancelRequested(object? sender, EventArgs eventArgs) =>
        Close(null);

    /// <summary>終了後にViewModelイベント購読を解除</summary>
    private void Window_Closed(object? sender, EventArgs eventArgs)
    {
        viewModel.Saved -= ViewModel_Saved;
        viewModel.CancelRequested -= ViewModel_CancelRequested;
    }
}
