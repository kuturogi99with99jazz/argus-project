using System.Windows.Input;

namespace Argus.Avalonia.ViewModels;

/// <summary>非同期操作の多重実行防止、キャンセル、例外通知を一つのICommandへ集約</summary>
public sealed class AsyncCommand : ViewModelBase, ICommand
{
    private readonly Func<object?, CancellationToken, Task> executeAsync;
    private readonly Func<object?, bool>? canExecute;
    private readonly Action<Exception>? onException;
    private readonly CancellationToken cancellationToken;
    private bool isRunning;

    /// <summary>非同期処理と実行可否をAvaloniaバインディングから利用できる形へ構成</summary>
    public AsyncCommand(
        Func<object?, CancellationToken, Task> executeAsync,
        Func<object?, bool>? canExecute = null,
        Action<Exception>? onException = null,
        CancellationToken cancellationToken = default)
    {
        this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        this.canExecute = canExecute;
        this.onException = onException;
        this.cancellationToken = cancellationToken;
    }

    public event EventHandler? CanExecuteChanged;

    /// <summary>現在このコマンド自身が処理を実行しているかどうか</summary>
    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            if (SetField(ref isRunning, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>多重実行を抑止し画面固有の条件を含めた操作可否を返却</summary>
    public bool CanExecute(object? parameter) =>
        !IsRunning && (canExecute?.Invoke(parameter) ?? true);

    /// <summary>ICommand境界から非同期処理を開始し例外を登録済み境界へ通知</summary>
    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    /// <summary>テストと画面終了処理から完了を待機できる形で非同期処理を実行</summary>
    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        IsRunning = true;
        try
        {
            await executeAsync(parameter, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application shutdown cancellation is an expected completion path.
        }
        catch (Exception exception)
        {
            onException?.Invoke(exception);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>選択状態など外部条件の変更をAvaloniaへ通知</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
