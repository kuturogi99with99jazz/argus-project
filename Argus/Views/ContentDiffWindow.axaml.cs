using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Argus.ViewModels;

namespace Argus.Views;

/// <summary>追加・削除・変更をテーマ対応で確認する差分モーダル画面</summary>
public sealed partial class ContentDiffWindow : Window
{
    /// <summary>デザイン時の空ViewModelでXAMLを読み込む初期化</summary>
    public ContentDiffWindow()
        : this(new ContentDiffDialogViewModel(
            new Argus.Core.Models.WatchTarget(
                Guid.Empty,
                "",
                new Uri("https://example.com/"),
                Argus.Core.Models.WatchMode.HtmlText,
                true,
                null,
                null),
            new Argus.Core.Models.ContentDiff([])))
    {
    }

    /// <summary>差分ViewModelを画面へ接続してウィンドウを初期化</summary>
    public ContentDiffWindow(ContentDiffDialogViewModel viewModel)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <summary>閉じるボタンの操作だけをウィンドウへ伝達</summary>
    private void CloseButton_Click(object? sender, RoutedEventArgs eventArgs) => Close(true);

    /// <summary>EnterとEscapeを画面固有の閉じる操作として処理</summary>
    private void ContentDiffWindow_KeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key is Key.Enter or Key.Escape)
        {
            Close(true);
            eventArgs.Handled = true;
        }
    }
}
