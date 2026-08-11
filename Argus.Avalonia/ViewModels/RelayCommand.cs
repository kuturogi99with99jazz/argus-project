using System.Windows.Input;

namespace Argus.Avalonia.ViewModels;

/// <summary>同期的な画面操作と実行可否を外部ライブラリなしで公開するコマンド</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Func<object?, bool>? canExecute;

    /// <summary>処理と任意の実行可否判定を一つのICommandへ構成</summary>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    /// <summary>画面状態に基づく現在の操作可否を返却</summary>
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    /// <summary>登録済みの同期操作を実行</summary>
    public void Execute(object? parameter) => execute(parameter);

    /// <summary>選択状態など外部条件の変更をAvaloniaへ通知</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
