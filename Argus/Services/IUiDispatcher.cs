namespace Argus.Services;

/// <summary>Coreイベントの画面状態反映だけをUIスレッドへ切り替える境界</summary>
public interface IUiDispatcher
{
    /// <summary>必要な場合だけ処理をUIスレッドへディスパッチ</summary>
    void Dispatch(Action action);
}
