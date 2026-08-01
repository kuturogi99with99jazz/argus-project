using Argus.Core.Models;
using Argus.WinForms.Presentation;

namespace Argus.WinForms.Tests.Presentation;

/// <summary>監視対象行ビューモデルの表示状態を検証するテスト</summary>
public sealed class WatchTargetRowViewModelTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Constructor_EvenWithSnapshot_StartsAsUnchecked()
    {
        var target = CreateTarget() with
        {
            PreviousSnapshot = new WatchSnapshot(
                new string('a', 64),
                DateTimeOffset.UtcNow)
        };

        var row = new WatchTargetRowViewModel(target);

        Assert.Equal(CheckStatus.Unchecked, row.Status);
        Assert.Equal("未確認", row.StatusText);
        Assert.Equal("—", row.LastCheckedText);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void SetRunningCount_PreservesFinalStatusAndAddsCheckingText()
    {
        var target = CreateTarget();
        var row = new WatchTargetRowViewModel(target);
        row.ApplyCheckResult(
            new CheckResult(
                Guid.NewGuid(),
                target.Id,
                CheckStatus.Unchanged,
                new DateTimeOffset(2026, 7, 31, 0, 0, 0, TimeSpan.Zero),
                new string('a', 64),
                null));

        row.SetRunningCount(2);

        Assert.Equal(CheckStatus.Unchanged, row.Status);
        Assert.Equal("更新なし / チェック中 ×2", row.StatusText);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData(CheckStatus.Unchecked, "未確認", "#ECEFF1", "#455A64")]
    [InlineData(CheckStatus.FirstFetch, "初回取得", "#E3F2FD", "#0D47A1")]
    [InlineData(CheckStatus.Unchanged, "更新なし", "#E8F5E9", "#1B5E20")]
    [InlineData(CheckStatus.Updated, "更新あり", "#FFF3E0", "#BF360C")]
    [InlineData(CheckStatus.Error, "エラー", "#FFEBEE", "#B71C1C")]
    public void CheckStatusAppearance_ReturnsDesignedNameAndColors(
        CheckStatus status,
        string expectedName,
        string expectedBackground,
        string expectedForeground)
    {
        var result = CheckStatusAppearance.Get(status);

        Assert.Equal(expectedName, result.DisplayName);
        Assert.Equal(expectedBackground, ToHex(result.Background));
        Assert.Equal(expectedForeground, ToHex(result.Foreground));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void CheckingAppearance_ReturnsDesignedNameAndColors()
    {
        Assert.Equal("チェック中", CheckStatusAppearance.Checking.DisplayName);
        Assert.Equal(
            "#E0F7FA",
            ToHex(CheckStatusAppearance.Checking.Background));
        Assert.Equal(
            "#006064",
            ToHex(CheckStatusAppearance.Checking.Foreground));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData("0.1.0+abcdef", "v0.1.0")]
    [InlineData("2.3.4", "v2.3.4")]
    [InlineData(null, "v1.2.3")]
    public void FormatVersion_ReturnsDisplayVersion(
        string? informationalVersion,
        string expected)
    {
        var result = ApplicationInfoProvider.FormatVersion(
            informationalVersion,
            new Version(1, 2, 3));

        Assert.Equal(expected, result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void ApplicationInfoProvider_ReportsCurrentBuildConfiguration()
    {
        var result = new ApplicationInfoProvider().Get();

#if DEBUG
        Assert.True(result.IsDebug);
#else
        Assert.False(result.IsDebug);
#endif
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchTarget CreateTarget() =>
        new(
            Guid.NewGuid(),
            "Sample",
            new Uri("https://example.com/"),
            WatchMode.HtmlText,
            true,
            null,
            null);
    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static string ToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
