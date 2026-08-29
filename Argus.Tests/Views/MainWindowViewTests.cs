namespace Argus.Tests.Views;

/// <summary>メイン画面に設定移行操作が接続されていることを検証するテスト</summary>
public sealed class MainWindowViewTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    /// <summary>設定インポートとエクスポートのラベル付きボタンが存在することを検証</summary>
    [Fact]
    public void MainWindow_ContainsSettingsTransferActions()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "Views", "MainWindow.axaml"));

        Assert.Contains("ImportSettingsCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("ExportSettingsCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("設定をインポート", xaml, StringComparison.Ordinal);
        Assert.Contains("設定をエクスポート", xaml, StringComparison.Ordinal);
    }
}
