using Argus.Services;
using Argus.ViewModels;

namespace Argus.Tests.ViewModels;

/// <summary>CSSセレクタ相談用プロンプトの生成とコピー状態を検証するテスト</summary>
public sealed class CssSelectorPromptViewModelTests
{
    /// <summary>URLと監視箇所の説明をAI向けプロンプトへ反映することを検証</summary>
    [Fact]
    public void Constructor_WhenUrlAndDescriptionAreProvided_BuildsPromptWithBothValues()
    {
        var viewModel = CreateViewModel("https://example.com/news");

        viewModel.TargetDescription = "最新記事のタイトルだけ";

        Assert.Contains("ページURL: https://example.com/news", viewModel.PromptText, StringComparison.Ordinal);
        Assert.Contains("監視したい箇所: 最新記事のタイトルだけ", viewModel.PromptText, StringComparison.Ordinal);
        Assert.Contains("CSSセレクタ", viewModel.PromptText, StringComparison.Ordinal);
        Assert.Contains("開発者ツール", viewModel.PromptText, StringComparison.Ordinal);
    }

    /// <summary>URLと説明が未入力でも未入力箇所を示すプロンプトを生成することを検証</summary>
    [Fact]
    public void Constructor_WhenUrlAndDescriptionAreEmpty_BuildsSafePrompt()
    {
        var viewModel = CreateViewModel(string.Empty);

        Assert.Contains("ページURL: （未入力）", viewModel.PromptText, StringComparison.Ordinal);
        Assert.Contains("監視したい箇所: （未入力）", viewModel.PromptText, StringComparison.Ordinal);
    }

    /// <summary>プロンプトの説明を変更すると表示内容が更新されることを検証</summary>
    [Fact]
    public void TargetDescription_WhenChanged_UpdatesPromptText()
    {
        var viewModel = CreateViewModel("https://example.com/");

        viewModel.TargetDescription = "更新日時";

        Assert.Contains("監視したい箇所: 更新日時", viewModel.PromptText, StringComparison.Ordinal);
    }

    /// <summary>コピー成功時に生成済みプロンプトと完了メッセージを渡すことを検証</summary>
    [Fact]
    public async Task CopyCommand_WhenClipboardSucceeds_CopiesPromptAndShowsStatus()
    {
        var clipboard = new StubClipboardService();
        var viewModel = CreateViewModel("https://example.com/", clipboard);
        viewModel.TargetDescription = "見出し";

        await viewModel.CopyCommand.ExecuteAsync(null);

        Assert.Equal(viewModel.PromptText, clipboard.CopiedText);
        Assert.Equal("プロンプトをクリップボードへコピーしました。", viewModel.CopyStatus);
        Assert.Null(viewModel.OperationError);
    }

    /// <summary>コピー失敗時にプロンプトを保持してエラーを表示することを検証</summary>
    [Fact]
    public async Task CopyCommand_WhenClipboardFails_PreservesPromptAndShowsError()
    {
        var clipboard = new StubClipboardService(new InvalidOperationException("クリップボードを利用できません。"));
        var viewModel = CreateViewModel("https://example.com/", clipboard);
        viewModel.TargetDescription = "見出し";
        var prompt = viewModel.PromptText;

        await viewModel.CopyCommand.ExecuteAsync(null);

        Assert.Equal(prompt, viewModel.PromptText);
        Assert.Equal("クリップボードを利用できません。", viewModel.OperationError);
        Assert.Null(viewModel.CopyStatus);
    }

    /// <summary>クリップボードを差し替え可能なプロンプトViewModelを生成</summary>
    private static CssSelectorPromptViewModel CreateViewModel(
        string url,
        IClipboardService? clipboard = null) =>
        new(url, clipboard ?? new StubClipboardService(), CancellationToken.None);

    /// <summary>コピー要求を記録または指定例外で失敗させるクリップボードスタブ</summary>
    private sealed class StubClipboardService(Exception? exception = null) : IClipboardService
    {
        private readonly Exception? exception = exception;

        /// <summary>最後に受け取ったコピー文字列</summary>
        public string? CopiedText { get; private set; }

        /// <summary>コピー要求を記録し、必要に応じて失敗させる</summary>
        public Task CopyTextAsync(string text, CancellationToken cancellationToken)
        {
            if (exception is not null)
            {
                throw exception;
            }

            CopiedText = text;
            return Task.CompletedTask;
        }
    }
}
