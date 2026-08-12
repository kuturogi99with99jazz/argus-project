using Argus.ViewModels;

namespace Argus.Tests.ViewModels;

/// <summary>非同期操作の多重実行防止と例外境界を検証するテスト</summary>
public sealed class AsyncCommandTests
{
    /// <summary>処理中は再実行できず完了後に再実行可能となることを検証</summary>
    [Fact]
    public async Task ExecuteAsync_WhileRunning_DisablesThenReenablesCommand()
    {
        var release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncCommand((_, _) => release.Task);

        var execution = command.ExecuteAsync(null);

        Assert.True(command.IsRunning);
        Assert.False(command.CanExecute(null));
        release.SetResult();
        await execution;
        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    /// <summary>操作例外が指定したハンドラーへ渡され次回実行を妨げないことを検証</summary>
    [Fact]
    public async Task ExecuteAsync_WhenOperationThrows_ReportsErrorAndAllowsRetry()
    {
        Exception? reported = null;
        var command = new AsyncCommand(
            (_, _) => throw new InvalidOperationException("failure"),
            onException: exception => reported = exception);

        await command.ExecuteAsync(null);

        Assert.IsType<InvalidOperationException>(reported);
        Assert.True(command.CanExecute(null));
    }
}
