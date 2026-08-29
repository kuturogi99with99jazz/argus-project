using System.Text;
using Argus.Services;

namespace Argus.Tests.Services;

/// <summary>設定ファイルのローカル読み書き境界を検証するテスト</summary>
public sealed class SettingsFileDialogServiceTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"ArgusSettingsTests-{Guid.NewGuid():N}");

    /// <summary>設定JSONをBOMなしUTF-8で保存して読み戻せることを検証</summary>
    [Fact]
    public async Task WriteAndReadAsync_UsesUtf8WithoutBom()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        var service = new SettingsFileDialogService(() => null);

        await service.WriteAsync(path, "{\"名前\":\"監視対象\"}", CancellationToken.None);

        var bytes = await File.ReadAllBytesAsync(path);
        var content = await service.ReadAsync(path, CancellationToken.None);

        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        Assert.Equal("{\"名前\":\"監視対象\"}", content);
    }

    /// <summary>運用中の設定ファイルをポータブル設定で上書きせず既存内容を保持することを検証</summary>
    [Fact]
    public async Task WriteAsync_WhenOperationalSettingsPathIsSelected_RejectsWithoutChangingFile()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "targets.json");
        await File.WriteAllTextAsync(path, "operational settings");
        var service = new SettingsFileDialogService(() => null, path);

        var exception = await Assert.ThrowsAsync<SettingsFileException>(
            () => service.WriteAsync(path, "portable settings", CancellationToken.None));

        Assert.Contains("運用中", exception.Message, StringComparison.Ordinal);
        Assert.Equal("operational settings", await File.ReadAllTextAsync(path));
    }

    /// <summary>各テストの一時ファイルを終了時に削除</summary>
    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
