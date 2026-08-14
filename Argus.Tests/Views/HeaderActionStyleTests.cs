namespace Argus.Tests.Views;

/// <summary>ヘッダー操作の状態配色が共通スタイルから逸脱しないことを検証するテスト</summary>
public sealed class HeaderActionStyleTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    /// <summary>隣接するマニュアル操作とテーマ操作が同じ配色契約を共有することを検証</summary>
    [Fact]
    public void MainWindow_HeaderActionsUseSharedStyle()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "Views", "MainWindow.axaml"));

        Assert.Contains("<Button Classes=\"headerAction\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ToggleButton Classes=\"headerAction themeToggle\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>ホバー時の背景変更で文字とアイコンの視認性が失われないことを検証</summary>
    [Fact]
    public void AppStyles_HeaderActionHoverKeepsWhiteForeground()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "App.axaml"));

        Assert.Contains("Style Selector=\"Button.headerAction:pointerover\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style Selector=\"ToggleButton.headerAction:pointerover\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Background\" Value=\"{DynamicResource AppAccentBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Foreground\" Value=\"White\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"BorderBrush\" Value=\"White\"", xaml, StringComparison.Ordinal);
    }
}
