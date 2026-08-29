using Argus.ViewModels;
using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Tests.ViewModels;

/// <summary>監視対象編集ViewModelの入力状態と検証反映を検証するテスト</summary>
public sealed class TargetEditViewModelTests
{
    /// <summary>監視モード変更に応じてCSSセレクタ入力欄の表示状態が変わることを検証</summary>
    [Fact]
    public void SelectedMode_WhenChangedToCssSelector_ShowsCssSelectorField()
    {
        var viewModel = CreateViewModel((_, _) =>
            Task.FromResult(WatchTargetChangeResult.Success(null)));

        viewModel.SelectedMode = WatchMode.CssSelector;

        Assert.True(viewModel.IsCssSelectorVisible);
    }

    /// <summary>CSSセレクタ比較時だけAI相談コマンドへ現在のURLを渡すことを検証</summary>
    [Fact]
    public async Task ShowCssSelectorPromptCommand_WhenCssSelectorMode_PassesCurrentUrl()
    {
        string? capturedUrl = null;
        var viewModel = CreateViewModel(
            (_, _) => Task.FromResult(WatchTargetChangeResult.Success(null)),
            (url, _) =>
            {
                capturedUrl = url;
                return Task.CompletedTask;
            });
        viewModel.Url = "https://example.com/news";
        viewModel.SelectedMode = WatchMode.CssSelector;

        await viewModel.ShowCssSelectorPromptCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com/news", capturedUrl);
    }

    /// <summary>CSSセレクタ比較以外ではAI相談コマンドを実行できないことを検証</summary>
    [Fact]
    public void ShowCssSelectorPromptCommand_WhenModeIsNotCssSelector_CannotExecute()
    {
        var viewModel = CreateViewModel(
            (_, _) => Task.FromResult(WatchTargetChangeResult.Success(null)),
            (_, _) => Task.CompletedTask);

        Assert.False(viewModel.ShowCssSelectorPromptCommand.CanExecute(null));

        viewModel.SelectedMode = WatchMode.HtmlWhole;

        Assert.False(viewModel.ShowCssSelectorPromptCommand.CanExecute(null));
    }

    /// <summary>Coreの項目別入力エラーが対応する表示プロパティへ反映されることを検証</summary>
    [Fact]
    public async Task SaveAsync_WhenCoreReturnsValidationErrors_MapsFieldErrors()
    {
        var errors = new[]
        {
            new ValidationError(nameof(WatchTargetInput.Name), "名前エラー"),
            new ValidationError(nameof(WatchTargetInput.Url), "URLエラー"),
            new ValidationError(nameof(WatchTargetInput.CssSelector), "CSSエラー")
        };
        var viewModel = CreateViewModel((_, _) => Task.FromResult(
            WatchTargetChangeResult.ValidationFailure(errors)));

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("名前エラー", viewModel.NameError);
        Assert.Equal("URLエラー", viewModel.UrlError);
        Assert.Equal("CSSエラー", viewModel.CssSelectorError);
        Assert.Null(viewModel.SavedTarget);
    }

    /// <summary>保存成功時にCoreへ渡した値と保存済み対象が確定することを検証</summary>
    [Fact]
    public async Task SaveAsync_WhenSuccessful_PublishesSavedTarget()
    {
        WatchTargetInput? captured = null;
        var saved = new WatchTarget(
            Guid.NewGuid(), "Saved", new Uri("https://example.com/"),
            WatchMode.HtmlWhole, true, "memo", null);
        var viewModel = CreateViewModel((input, _) =>
        {
            captured = input;
            return Task.FromResult(WatchTargetChangeResult.Success(saved));
        });
        viewModel.Name = "Saved";
        viewModel.Url = "https://example.com/";
        viewModel.SelectedMode = WatchMode.HtmlWhole;
        viewModel.Memo = "memo";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Saved", captured?.Name);
        Assert.Equal(WatchMode.HtmlWhole, captured?.Mode);
        Assert.Same(saved, viewModel.SavedTarget);
    }

    /// <summary>保存処理を差し替えた編集ViewModelを生成</summary>
    private static TargetEditViewModel CreateViewModel(
        Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync,
        Func<string, CancellationToken, Task>? showCssSelectorPromptAsync = null) =>
        new(null, saveAsync, CancellationToken.None, showCssSelectorPromptAsync);
}
