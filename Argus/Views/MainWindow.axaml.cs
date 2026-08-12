using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Argus.ViewModels;

namespace Argus.Views;

/// <summary>Argusのメイン画面を表示するルート画面</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>XAMLレイアウトを読み込んでメイン画面を初期化</summary>
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <summary>依存注入済みViewModelを設定して画面破棄時の後始末を結び付ける</summary>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Closed += (_, _) => viewModel.Dispose();
    }

    /// <summary>DataGridの複数選択をViewModelへ同期</summary>
    private void TargetGrid_SelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is DataGrid dataGrid)
        {
            viewModel.SetSelection(
                dataGrid.SelectedItems?.OfType<WatchTargetRowViewModel>() ?? []);
        }
    }

    /// <summary>列境界のダブルクリックを内容に応じた列幅へ変換</summary>
    private void TargetGrid_DoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        const double resizeGripWidth = 6;
        var pointerX = eventArgs.GetPosition(dataGrid).X;
        var columnBoundary = 0d;
        foreach (var column in dataGrid.Columns.OrderBy(column => column.DisplayIndex))
        {
            columnBoundary += column.ActualWidth;
            if (Math.Abs(pointerX - columnBoundary) > resizeGripWidth)
            {
                continue;
            }

            column.Width = DataGridLength.Auto;
            eventArgs.Handled = true;
            return;
        }
    }
}
