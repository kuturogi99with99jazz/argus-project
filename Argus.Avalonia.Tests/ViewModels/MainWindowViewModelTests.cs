using Argus.Avalonia.Services;
using Argus.Avalonia.ViewModels;
using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;

namespace Argus.Avalonia.Tests.ViewModels;

/// <summary>メインViewModelの選択状態、Coreイベント、操作可否を検証するテスト</summary>
public sealed class MainWindowViewModelTests
{
    /// <summary>単一選択時にブラウザ、編集、削除操作が有効になることを検証</summary>
    [Fact]
    public void SetSelection_WhenOneIdleRowSelected_EnablesSingleTargetCommands()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel();

        viewModel.SetSelection([viewModel.Rows[0]]);

        Assert.True(viewModel.OpenBrowserCommand.CanExecute(null));
        Assert.True(viewModel.EditCommand.CanExecute(null));
        Assert.True(viewModel.DeleteCommand.CanExecute(null));
        Assert.Equal("選択 1件", viewModel.SelectionSummary);
    }

    /// <summary>Coreの実行開始と完了イベントが一覧行と集計へ反映されることを検証</summary>
    [Fact]
    public async Task CheckSelectedAsync_WhenCheckRuns_UpdatesRunningAndResultState()
    {
        using var context = TestContext.Create(blockFetch: true);
        using var viewModel = context.CreateViewModel();
        var row = viewModel.Rows[0];
        viewModel.SetSelection([row]);

        var execution = viewModel.CheckSelectedCommand.ExecuteAsync(null);

        Assert.Equal(1, row.RunningCount);
        Assert.Equal("チェック中 1件", viewModel.CheckingSummary);
        Assert.False(viewModel.EditCommand.CanExecute(null));
        context.ReleaseFetch();
        await execution;
        Assert.Equal(0, row.RunningCount);
        Assert.Equal(CheckStatus.FirstFetch, row.Status);
        Assert.Equal("チェック中 0件", viewModel.CheckingSummary);
    }

    /// <summary>起動データエラー時に主要な変更操作が無効化されることを検証</summary>
    [Fact]
    public void Constructor_WhenStartupError_DisablesOperationalCommands()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel("読み込みエラー");
        viewModel.SetSelection([viewModel.Rows[0]]);

        Assert.False(viewModel.CheckAllCommand.CanExecute(null));
        Assert.False(viewModel.AddCommand.CanExecute(null));
        Assert.False(viewModel.OpenBrowserCommand.CanExecute(null));
        Assert.Equal("読み込みエラー", viewModel.StatusMessage);
    }

    /// <summary>追加画面の入力がCore管理サービスを経由して一覧へ反映されることを検証</summary>
    [Fact]
    public async Task AddAsync_WhenDialogSavesTarget_ReloadsRows()
    {
        using var context = TestContext.Create();
        context.Dialog.EditorInput = new WatchTargetInput(
            "Added", "https://example.org/", WatchMode.HtmlWhole, true, "memo");
        using var viewModel = context.CreateViewModel();

        await viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Rows.Count);
        Assert.Contains(viewModel.Rows, row => row.Name == "Added");
        Assert.Equal("Added を追加しました。", viewModel.StatusMessage);
    }

    /// <summary>削除確認後にCore管理サービスを経由して一覧から対象が除かれることを検証</summary>
    [Fact]
    public async Task DeleteAsync_WhenConfirmed_RemovesSelectedTarget()
    {
        using var context = TestContext.Create();
        context.Dialog.ConfirmDelete = true;
        using var viewModel = context.CreateViewModel();
        viewModel.SetSelection([viewModel.Rows[0]]);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Rows);
        Assert.Equal("Example を削除しました。", viewModel.StatusMessage);
    }

    /// <summary>単一選択したCoreモデルがブラウザサービスへ渡されることを検証</summary>
    [Fact]
    public void OpenBrowser_WhenOneRowSelected_DelegatesValidatedTarget()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel();
        viewModel.SetSelection([viewModel.Rows[0]]);

        viewModel.OpenBrowserCommand.Execute(null);

        Assert.Equal("Example", context.Browser.LastOpenedTarget?.Name);
        Assert.Equal("Example を既定ブラウザで開きました。", viewModel.StatusMessage);
    }

    /// <summary>CoreとUI境界を決定的なテスト依存でまとめて構築する補助コンテキスト</summary>
    private sealed class TestContext : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new();
        private readonly BlockingFetcher fetcher;
        private readonly WatchTargetRepository repository;
        private readonly WatchTargetManagementService managementService;
        private readonly CheckCoordinator coordinator;

        /// <summary>ブラウザ委譲結果を観測するテスト用サービス</summary>
        public StubBrowserService Browser { get; } = new();

        /// <summary>編集入力と確認結果を制御するテスト用サービス</summary>
        public StubDialogService Dialog { get; } = new();

        /// <summary>実サイトやファイルへ依存しないCore依存を構築</summary>
        private TestContext(bool blockFetch)
        {
            var target = new WatchTarget(
                Guid.NewGuid(), "Example", new Uri("https://example.com/"),
                WatchMode.HtmlText, true, null, null);
            repository = new WatchTargetRepository(
                new MemoryStore(),
                new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
            fetcher = new BlockingFetcher(blockFetch);
            var checkService = new WatchCheckService(
                fetcher,
                new ComparisonContentExtractor(new HtmlTextNormalizer()),
                new Sha256HashService());
            coordinator = new CheckCoordinator(repository, checkService);
            managementService = new WatchTargetManagementService(repository, coordinator);
        }

        /// <summary>指定した取得待機状態でテストコンテキストを生成</summary>
        public static TestContext Create(bool blockFetch = false) => new(blockFetch);

        /// <summary>実依存を使うメインViewModelをUI境界だけ差し替えて生成</summary>
        public MainWindowViewModel CreateViewModel(string? startupError = null) =>
            new(
                repository,
                managementService,
                coordinator,
                Browser,
                Dialog,
                new ImmediateDispatcher(),
                new ApplicationInfo(null, "v0.1.0", true),
                cancellation,
                startupError);

        /// <summary>待機中のHTTP取得をテストから完了可能にする操作</summary>
        public void ReleaseFetch() => fetcher.Release();

        /// <summary>チェック処理とキャンセルトークンをテスト終了時に解放</summary>
        public void Dispose()
        {
            cancellation.Cancel();
            coordinator.Dispose();
            cancellation.Dispose();
        }
    }

    /// <summary>保存内容をメモリだけで保持するテスト用永続化境界</summary>
    private sealed class MemoryStore : ITargetStore
    {
        /// <summary>テストでは初期文書を直接渡すため未使用の読み込み操作</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(TargetStoreDocument.Empty);

        /// <summary>永続化成功だけを再現する保存操作</summary>
        public Task SaveAsync(TargetStoreDocument document, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>チェック中状態を観測できるよう完了時期を制御する取得境界</summary>
    private sealed class BlockingFetcher(bool blockFetch) : IWebPageFetcher
    {
        private readonly TaskCompletionSource release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>指定時だけテストから解放されるまでHTML取得を待機</summary>
        public async Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (blockFetch)
            {
                await release.Task.WaitAsync(cancellationToken);
            }

            return "<html><body>content</body></html>";
        }

        /// <summary>待機中の取得処理を完了させるテスト操作</summary>
        public void Release() => release.TrySetResult();
    }

    /// <summary>ブラウザ起動を行わず成功だけを返すUI境界</summary>
    private sealed class StubBrowserService : IBrowserService
    {
        /// <summary>直近に起動を依頼されたCore監視対象</summary>
        public WatchTarget? LastOpenedTarget { get; private set; }

        /// <summary>OSプロセスを起動せず成功結果を返却</summary>
        public BrowserOpenResult Open(WatchTarget target)
        {
            LastOpenedTarget = target;
            return BrowserOpenResult.Success;
        }
    }

    /// <summary>画面を開かず利用者操作をキャンセルとして返すUI境界</summary>
    private sealed class StubDialogService : IDialogService
    {
        /// <summary>設定時に編集画面から保存するテスト入力</summary>
        public WatchTargetInput? EditorInput { get; set; }

        /// <summary>削除確認で返す利用者選択</summary>
        public bool ConfirmDelete { get; set; }

        /// <summary>編集画面を表示せずキャンセル結果を返却</summary>
        public async Task<WatchTarget?> ShowTargetEditorAsync(
            WatchTarget? target,
            Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync,
            CancellationToken cancellationToken)
        {
            if (EditorInput is null)
            {
                return null;
            }

            var result = await saveAsync(EditorInput, cancellationToken);
            return result.IsSuccess ? result.Target : null;
        }

        /// <summary>削除確認を表示せず拒否結果を返却</summary>
        public Task<bool> ConfirmDeleteAsync(WatchTarget target, CancellationToken cancellationToken) =>
            Task.FromResult(ConfirmDelete);

        /// <summary>テストではエラー画面を表示せず完了</summary>
        public Task ShowErrorAsync(string message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>Coreイベントを呼び出し元スレッドで即時反映するUIスレッド境界</summary>
    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        /// <summary>テストではスレッド切替を行わず処理を実行</summary>
        public void Dispatch(Action action) => action();
    }
}
