using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Markup.Xaml;
using Argus.Avalonia.ViewModels;

namespace Argus.Avalonia.Views;

/// <summary>Avalonia PoCの監視対象一覧を表示するルート画面</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>XAMLレイアウトを読み込みメイン画面を初期化</summary>
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <summary>手動構築したViewModelを設定し終了時の購読解除を接続</summary>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Closed += (_, _) => viewModel.Dispose();
    }

    /// <summary>ListBoxの複数選択を画面操作可否へ同期</summary>
    private void TargetList_SelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is ListBox listBox)
        {
            viewModel.SetSelection(
                listBox.SelectedItems?.OfType<WatchTargetRowViewModel>() ?? []);
        }
    }
}
