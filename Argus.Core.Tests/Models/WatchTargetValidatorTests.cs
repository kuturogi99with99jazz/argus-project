using Argus.Core.Models;

namespace Argus.Core.Tests.Models;

/// <summary>監視対象入力の検証と生成を検証するテスト</summary>
public sealed class WatchTargetValidatorTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsBlank_ReturnsNameError(string name)
    {
        var input = new WatchTargetInput(name, "https://example.com/", WatchMode.HtmlText, true, null);

        var result = WatchTargetValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(WatchTargetInput.Name));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData("/relative")]
    [InlineData("ftp://example.com/")]
    [InlineData("not-a-url")]
    public void Validate_WhenUrlIsNotAbsoluteHttpOrHttps_ReturnsUrlError(string url)
    {
        var input = new WatchTargetInput("Sample", url, WatchMode.HtmlText, true, null);

        var result = WatchTargetValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(WatchTargetInput.Url));
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    [Fact]
    public void Create_WhenInputIsValid_TrimsTextAndCreatesTarget()
    {
        var id = Guid.NewGuid();
        var input = new WatchTargetInput(
            "  Sample  ",
            "https://example.com/path",
            WatchMode.HtmlText,
            true,
            "  memo  ");

        var result = WatchTargetValidator.Create(id, input, null);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Sample", result.Value.Name);
        Assert.Equal(new Uri("https://example.com/path"), result.Value.Url);
        Assert.Equal("memo", result.Value.Memo);
        Assert.Null(result.Value.PreviousSnapshot);
    }
}
