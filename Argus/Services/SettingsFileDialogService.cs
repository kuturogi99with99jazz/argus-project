using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Argus.Services;

/// <summary>Avaloniaのファイル選択と設定JSONのローカル入出力を担当</summary>
public sealed class SettingsFileDialogService(
    Func<Window?> ownerProvider) : ISettingsFileService
{
    private static readonly FilePickerFileType SettingsFileType =
        new("Argus設定JSON")
        {
            Patterns = ["*.json"]
        };
    private readonly Func<Window?> ownerProvider = ownerProvider ??
        throw new ArgumentNullException(nameof(ownerProvider));

    /// <summary>JSON設定ファイルを一つだけ選択しローカルパスを返却</summary>
    public async Task<string?> PickImportPathAsync(CancellationToken cancellationToken)
    {
        var files = await GetStorageProvider()
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [SettingsFileType]
            })
            .WaitAsync(cancellationToken)
            .ConfigureAwait(true);
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    /// <summary>JSON設定の保存先を選択しローカルパスを返却</summary>
    public async Task<string?> PickExportPathAsync(CancellationToken cancellationToken)
    {
        var file = await GetStorageProvider()
            .SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "argus-settings.json",
                DefaultExtension = "json",
                FileTypeChoices = [SettingsFileType],
                ShowOverwritePrompt = true
            })
            .WaitAsync(cancellationToken)
            .ConfigureAwait(true);
        return file?.TryGetLocalPath();
    }

    /// <summary>選択済みファイルをUTF-8として非同期に読み込む</summary>
    public Task<string> ReadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
    }

    /// <summary>設定JSONをUTF-8 BOMなしで非同期に保存する</summary>
    public Task WriteAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        return File.WriteAllTextAsync(
            path,
            content,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
    }

    /// <summary>所有ウィンドウからAvaloniaのストレージ境界を取得</summary>
    private IStorageProvider GetStorageProvider() =>
        ownerProvider()?.StorageProvider
        ?? throw new InvalidOperationException("メイン画面が利用できません。");
}
