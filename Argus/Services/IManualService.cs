namespace Argus.Services;

/// <summary>正式ユーザーマニュアルの表示結果をUIで扱える形に固定する値</summary>
public sealed record ManualOpenResult(bool IsSuccess, string? ErrorMessage)
{
    /// <summary>成功時に割り当てを発生させず再利用する結果</summary>
    public static ManualOpenResult Success { get; } = new(true, null);

    /// <summary>利用者向けメッセージを保持する失敗結果を生成</summary>
    public static ManualOpenResult Failure(string message) => new(false, message);
}

/// <summary>埋め込み済みの正式ユーザーマニュアルをOSの既定ブラウザへ渡す境界</summary>
public interface IManualService
{
    /// <summary>オフラインマニュアルを閲覧可能な場所へ展開して表示</summary>
    ManualOpenResult Open();
}
