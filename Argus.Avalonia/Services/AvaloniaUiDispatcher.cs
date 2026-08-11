using Avalonia.Threading;

namespace Argus.Avalonia.Services;

/// <summary>Avalonia DispatcherをUIプロジェクト内だけで利用する実装</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <summary>UIスレッド上なら即時実行しそれ以外の場合だけキューへ登録</summary>
    public void Dispatch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}
