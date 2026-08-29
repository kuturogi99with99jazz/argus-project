using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Argus.Views;

/// <summary>外部UIライブラリなしで確認とエラー通知を提供するモーダル画面</summary>
public sealed partial class MessageDialogWindow : Window
{
    /// <summary>XAMLツールがウィンドウ資源を読み込むためのデザイン時コンストラクター</summary>
    public MessageDialogWindow()
        : this("Argus", string.Empty, false)
    {
    }

    /// <summary>メッセージと確認種別に応じてボタン構成を初期化</summary>
    public MessageDialogWindow(
        string title,
        string message,
        bool isConfirmation,
        string confirmationText = "削除")
    {
        AvaloniaXamlLoader.Load(this);
        Title = title;
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        this.FindControl<Button>("CancelButton")!.IsVisible = isConfirmation;
        this.FindControl<Button>("AcceptButton")!.Content = isConfirmation
            ? confirmationText
            : "OK";
    }

    /// <summary>確認操作を肯定結果としてモーダル呼び出し元へ返却</summary>
    private void AcceptButton_Click(object? sender, RoutedEventArgs eventArgs) => Close(true);

    /// <summary>確認操作を否定結果としてモーダル呼び出し元へ返却</summary>
    private void CancelButton_Click(object? sender, RoutedEventArgs eventArgs) => Close(false);
}
