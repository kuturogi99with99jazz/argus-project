using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Argus.Services;

/// <summary>単一ファイル配布を維持しながら埋め込みマニュアルを表示するサービス</summary>
public sealed class ManualService : IManualService
{
    private const string ManualRootResourceName = "Argus.Manual";
    private static readonly string[] ResourceFileNames = ["index.html", "main.png", "entry.png"];
    private readonly Assembly resourceAssembly;
    private readonly string extractionDirectory;

    /// <summary>アプリ版ごとに分離したOS一時領域を展開先として構成</summary>
    public ManualService(string version)
        : this(version, typeof(ManualService).Assembly)
    {
    }

    /// <summary>テスト可能なアセンブリ境界を保ちながら展開先を構成</summary>
    internal ManualService(string version, Assembly resourceAssembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        this.resourceAssembly = resourceAssembly ?? throw new ArgumentNullException(nameof(resourceAssembly));
        var safeVersion = string.Concat(version.Where(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_'));
        extractionDirectory = Path.Combine(
            Path.GetTempPath(),
            "Argus",
            "Manual",
            string.IsNullOrEmpty(safeVersion) ? "current" : safeVersion);
    }

    /// <summary>固定名の資産だけを展開し相対画像参照を保ったままブラウザへ委譲</summary>
    public ManualOpenResult Open()
    {
        try
        {
            Directory.CreateDirectory(extractionDirectory);
            foreach (var fileName in ResourceFileNames)
            {
                ExtractResource(fileName);
            }

            var indexPath = Path.Combine(extractionDirectory, "index.html");
            _ = Process.Start(new ProcessStartInfo(indexPath)
            {
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("既定ブラウザのプロセスを開始できませんでした。");
            return ManualOpenResult.Success;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException or
            Win32Exception or PlatformNotSupportedException or NotSupportedException)
        {
            return ManualOpenResult.Failure(
                "ユーザーマニュアルを開けませんでした。一時フォルダーと既定ブラウザの設定を確認してください。");
        }
    }

    /// <summary>アセンブリに固定された資産を元のバイト列のまま展開</summary>
    private void ExtractResource(string fileName)
    {
        var resourceName = $"{ManualRootResourceName}.{fileName}";
        using var source = resourceAssembly.GetManifestResourceStream(resourceName) ??
            throw new InvalidOperationException($"埋め込みマニュアル資産が見つかりません: {resourceName}");
        using var destination = new FileStream(
            Path.Combine(extractionDirectory, fileName),
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        source.CopyTo(destination);
    }
}
