using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Argus.Services;
using Argus.ViewModels;

namespace Argus.Views;

/// <summary>AIへ貼り付けるCSSセレクタ相談用プロンプトを表示するモーダル画面</summary>
public sealed partial class CssSelectorPromptWindow : Window
{
    /// <summary>デザイン時の仮ViewModelでXAMLを読み込む初期化</summary>
    public CssSelectorPromptWindow()
        : this(new CssSelectorPromptViewModel(
            "https://example.com/",
            new AvaloniaClipboardService(() => null),
            CancellationToken.None))
    {
    }

    /// <summary>プロンプトViewModelを画面へ接続してウィンドウを初期化</summary>
    public CssSelectorPromptWindow(CssSelectorPromptViewModel viewModel)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <summary>コピーせず相談小窓だけを閉じる操作を画面へ伝達</summary>
    private void CloseButton_Click(object? sender, RoutedEventArgs eventArgs) => Close(false);
}
