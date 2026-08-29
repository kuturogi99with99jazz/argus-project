using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Argus.Core.Persistence;

namespace Argus.Services;

/// <summary>設定ファイルの保存先が利用できないことを示す例外</summary>
public sealed class SettingsFileException : Exception
{
    /// <summary>保存先エラーの説明を保持する例外を生成</summary>
    public SettingsFileException(string message)
        : base(message)
    {
    }
}

/// <summary>Avaloniaのファイル選択と設定JSONのローカル入出力を担当</summary>
public sealed class SettingsFileDialogService : ISettingsFileService
{
    private static readonly FilePickerFileType SettingsFileType =
        new("Argus設定JSON")
        {
            Patterns = ["*.json"]
        };
    private readonly Func<Window?> ownerProvider;
    private readonly string operationalSettingsPath;

    /// <summary>ファイル選択境界と運用中設定ファイルの保護対象を構成</summary>
    public SettingsFileDialogService(
        Func<Window?> ownerProvider,
        string? operationalSettingsPath = null)
    {
        this.ownerProvider = ownerProvider ??
            throw new ArgumentNullException(nameof(ownerProvider));
        this.operationalSettingsPath = Path.GetFullPath(
            operationalSettingsPath ?? JsonTargetStore.ResolveDefaultPath());
    }

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

        var normalizedPath = Path.GetFullPath(path);
        if (string.Equals(
                normalizedPath,
                operationalSettingsPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SettingsFileException(
                "運用中の設定ファイルにはエクスポートできません。別の保存先を選択してください。");
        }

        return WriteAtomicallyAsync(normalizedPath, content, cancellationToken);
    }

    /// <summary>所有ウィンドウからAvaloniaのストレージ境界を取得</summary>
    private IStorageProvider GetStorageProvider() =>
        ownerProvider()?.StorageProvider
        ?? throw new InvalidOperationException("メイン画面が利用できません。");

    /// <summary>一時ファイルを同じディレクトリへ書き込み成功後に出力先を置換</summary>
    private static async Task WriteAtomicallyAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new SettingsFileException("保存先ディレクトリを解決できません。");
        var temporaryPath = Path.Combine(
            directory,
            $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        var moved = false;

        try
        {
            await File.WriteAllTextAsync(
                    temporaryPath,
                    content,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            moved = true;
        }
        finally
        {
            if (!moved)
            {
                TryDelete(temporaryPath);
            }
        }
    }

    /// <summary>保存失敗時に残った一時ファイルを後始末</summary>
    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // The next export can safely leave an unrelated temporary file behind.
        }
    }
}
