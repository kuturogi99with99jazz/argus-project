using Argus.ViewModels;
using Argus.Core.Models;

namespace Argus.Tests.ViewModels;

/// <summary>Coreモデルから一覧表示状態への変換を検証するテスト</summary>
public sealed class WatchTargetRowViewModelTests
{
    /// <summary>CSS監視対象が日本語の表示値へ変換されることを検証</summary>
    [Fact]
    public void Constructor_WhenCssSelectorTarget_MapsDisplayValues()
    {
        var target = CreateTarget(WatchMode.CssSelector, false);

        var row = new WatchTargetRowViewModel(target);

        Assert.Equal("CSSセレクタ比較", row.ModeText);
        Assert.Equal("いいえ", row.EnabledText);
        Assert.Equal("未確認", row.StatusText);
        Assert.Equal("—", row.LastCheckedText);
    }

    /// <summary>チェック結果と実行数が状態名と日時へ反映されることを検証</summary>
    [Fact]
    public void ApplyCheckResult_WhenUpdated_MapsStatusAndLocalTime()
    {
        var target = CreateTarget(WatchMode.HtmlText, true);
        var row = new WatchTargetRowViewModel(target);
        var completedAt = new DateTimeOffset(2026, 8, 11, 1, 2, 0, TimeSpan.Zero);

        row.SetRunningCount(2);
        row.ApplyCheckResult(new CheckResult(
            Guid.NewGuid(), target.Id, CheckStatus.Updated, completedAt, "hash", null));

        Assert.Equal("更新あり / チェック中 ×2", row.StatusText);
        Assert.Equal(completedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"), row.LastCheckedText);
        Assert.Equal("—", row.ErrorText);
    }

    /// <summary>テストごとに同じ必須値を持つ監視対象を生成</summary>
    private static WatchTarget CreateTarget(WatchMode mode, bool isEnabled) =>
        new(
            Guid.NewGuid(),
            "Example",
            new Uri("https://example.com/"),
            mode,
            isEnabled,
            null,
            null,
            mode == WatchMode.CssSelector ? "main" : null);
}
