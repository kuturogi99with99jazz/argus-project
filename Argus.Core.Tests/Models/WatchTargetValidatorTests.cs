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

    /// <summary>CSSセレクタ比較だけ抽出条件を必須とすることを検証</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenCssSelectorModeHasNoSelector_ReturnsCssSelectorError(string? selector)
    {
        var input = new WatchTargetInput(
            "Sample",
            "https://example.com/",
            WatchMode.CssSelector,
            true,
            null,
            selector);

        var result = WatchTargetValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(WatchTargetInput.CssSelector));
    }

    /// <summary>CSSセレクタの前後空白が保存前に除去されることを検証</summary>
    [Fact]
    public void Create_WhenCssSelectorIsValid_TrimsSelector()
    {
        var input = new WatchTargetInput(
            "Sample",
            "https://example.com/",
            WatchMode.CssSelector,
            true,
            null,
            "  main > .news  ");

        var result = WatchTargetValidator.Create(Guid.NewGuid(), input, null);

        Assert.True(result.IsValid);
        Assert.Equal("main > .news", result.Value?.CssSelector);
    }
}
