namespace Argus.Services;

/// <summary>設定ファイルの選択とローカル読み書きをViewModelから分離</summary>
public interface ISettingsFileService
{
    /// <summary>設定インポート用のJSONファイルをユーザーへ選択させる境界</summary>
    Task<string?> PickImportPathAsync(CancellationToken cancellationToken);

    /// <summary>設定エクスポート用の保存先をユーザーへ選択させる境界</summary>
    Task<string?> PickExportPathAsync(CancellationToken cancellationToken);

    /// <summary>選択済みの設定ファイルをUTF-8文字列として読み込む境界</summary>
    Task<string> ReadAsync(string path, CancellationToken cancellationToken);

    /// <summary>設定JSONをUTF-8 BOMなしで選択済みファイルへ保存する境界</summary>
    Task WriteAsync(string path, string content, CancellationToken cancellationToken);
}
