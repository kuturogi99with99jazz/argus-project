using Argus.Services;
using Argus.ViewModels;
using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;

namespace Argus.Tests.ViewModels;

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
        Assert.True(viewModel.OpenManualCommand.CanExecute(null));
        Assert.True(viewModel.EditCommand.CanExecute(null));
        Assert.True(viewModel.DeleteCommand.CanExecute(null));
        Assert.False(viewModel.ShowDiffCommand.CanExecute(null));
        Assert.Equal("選択 1件", viewModel.SelectionSummary);
    }

    /// <summary>設定エクスポートで選択した保存先へポータブルJSONを書き込むことを検証</summary>
    [Fact]
    public async Task ExportSettingsAsync_WhenPathIsSelected_WritesPortableSettings()
    {
        using var context = TestContext.Create();
        context.Settings.ExportPath = "settings.json";
        using var viewModel = context.CreateViewModel();

        await viewModel.ExportSettingsCommand.ExecuteAsync(null);

        Assert.Equal("settings.json", context.Settings.LastWrittenPath);
        Assert.NotNull(context.Settings.LastWrittenContent);
        Assert.Contains("argus-settings", context.Settings.LastWrittenContent, StringComparison.Ordinal);
        Assert.DoesNotContain("previousSnapshot", context.Settings.LastWrittenContent, StringComparison.Ordinal);
        Assert.Contains("設定をエクスポートしました", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    /// <summary>設定インポートの確認後に一覧を置き換え新規対象を初回取得状態にすることを検証</summary>
    [Fact]
    public async Task ImportSettingsAsync_WhenConfirmed_ReplacesRowsAsFirstFetchTargets()
    {
        using var context = TestContext.Create();
        context.Settings.ImportPath = "settings.json";
        context.Settings.ImportContent = """
            {
              "format": "argus-settings",
              "formatVersion": 1,
              "targets": [
                {
                  "name": "Imported",
                  "url": "https://example.org/",
                  "mode": "htmlText",
                  "isEnabled": true,
                  "memo": "memo",
                  "cssSelector": null
                }
              ]
            }
            """;
        context.Dialog.ConfirmImport = true;
        using var viewModel = context.CreateViewModel();

        await viewModel.ImportSettingsCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.Rows);
        Assert.Equal("Imported", row.Name);
        Assert.Equal(CheckStatus.Unchecked, row.Status);
        Assert.Equal("設定をインポートしました。", viewModel.StatusMessage);
    }

    /// <summary>設定インポートをキャンセルした場合に現在の一覧を維持することを検証</summary>
    [Fact]
    public async Task ImportSettingsAsync_WhenConfirmationIsDeclined_KeepsRows()
    {
        using var context = TestContext.Create();
        context.Settings.ImportPath = "settings.json";
        context.Settings.ImportContent = TestContext.CreateImportContent("Imported");
        context.Dialog.ConfirmImport = false;
        using var viewModel = context.CreateViewModel();

        await viewModel.ImportSettingsCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.Rows);
        Assert.Equal("Example", row.Name);
        Assert.Null(context.Settings.LastWrittenContent);
    }

    /// <summary>不正な設定ファイルをインポートした場合に一覧と保存内容を維持することを検証</summary>
    [Fact]
    public async Task ImportSettingsAsync_WhenFileIsInvalid_KeepsRowsAndShowsError()
    {
        using var context = TestContext.Create();
        context.Settings.ImportPath = "settings.json";
        context.Settings.ImportContent = "{ invalid";
        using var viewModel = context.CreateViewModel();

        await viewModel.ImportSettingsCommand.ExecuteAsync(null);

        Assert.Equal("Example", Assert.Single(viewModel.Rows).Name);
        Assert.Contains("設定", context.Dialog.LastErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>監視対象のチェック中は設定インポート操作を無効にすることを検証</summary>
    [Fact]
    public async Task ImportSettingsCommand_WhenCheckIsRunning_IsDisabled()
    {
        using var context = TestContext.Create(blockFetch: true);
        using var viewModel = context.CreateViewModel();
        viewModel.SetSelection([viewModel.Rows[0]]);

        var check = viewModel.CheckSelectedCommand.ExecuteAsync(null);

        Assert.False(viewModel.ImportSettingsCommand.CanExecute(null));
        context.ReleaseFetch();
        await check;
    }

    /// <summary>更新ありの行だけ差分表示コマンドを利用できることを検証</summary>
    [Fact]
    public async Task ShowDiffAsync_WhenUpdatedRowSelected_PassesDiffToDialog()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel();
        var row = viewModel.Rows[0];
        var diff = new ContentDiff([
            new ContentDiffEntry(ChangeKind.Changed, "old", "new")]);
        row.ApplyCheckResult(new CheckResult(
            Guid.NewGuid(), row.TargetId, CheckStatus.Updated,
            DateTimeOffset.UtcNow, "hash", null, diff));
        viewModel.SetSelection([row]);

        await viewModel.ShowDiffCommand.ExecuteAsync(null);

        Assert.Same(diff, context.Dialog.LastDiff);
        Assert.Equal("Example", context.Dialog.LastDiffTarget?.Name);
    }

    /// <summary>比較内容がない更新結果では差分表示理由を通知することを検証</summary>
    [Fact]
    public async Task ShowDiffAsync_WhenUpdatedRowHasNoDiff_ShowsReason()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel();
        var row = viewModel.Rows[0];
        row.ApplyCheckResult(new CheckResult(
            Guid.NewGuid(), row.TargetId, CheckStatus.Updated,
            DateTimeOffset.UtcNow, "hash", null));
        viewModel.SetSelection([row]);

        await viewModel.ShowDiffCommand.ExecuteAsync(null);

        Assert.Contains("比較内容", context.Dialog.LastErrorMessage);
        Assert.Null(context.Dialog.LastDiff);
    }

    /// <summary>更新ありの対象を再チェックしている間は古い差分を表示できないことを検証</summary>
    [Fact]
    public void ShowDiffCommand_WhenUpdatedRowIsChecking_IsDisabled()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel();
        var row = viewModel.Rows[0];
        var diff = new ContentDiff([
            new ContentDiffEntry(ChangeKind.Changed, "old", "new")]);
        row.ApplyCheckResult(new CheckResult(
            Guid.NewGuid(), row.TargetId, CheckStatus.Updated,
            DateTimeOffset.UtcNow, "hash", null, diff));
        row.SetRunningCount(1);
        viewModel.SetSelection([row]);

        Assert.False(viewModel.ShowDiffCommand.CanExecute(null));
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
        Assert.True(viewModel.OpenManualCommand.CanExecute(null));
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

    /// <summary>一覧未選択でも正式マニュアルを既定ブラウザへ渡せることを検証</summary>
    [Fact]
    public void OpenManual_WhenNoRowSelected_DelegatesToManualService()
    {
        using var context = TestContext.Create();
        using var viewModel = context.CreateViewModel();

        viewModel.OpenManualCommand.Execute(null);

        Assert.Equal(1, context.Manual.OpenCount);
        Assert.Equal("ユーザーマニュアルを既定ブラウザで開きました。", viewModel.StatusMessage);
    }

    /// <summary>正式マニュアルを開けない場合に利用者向けエラーを表示することを検証</summary>
    [Fact]
    public void OpenManual_WhenServiceFails_ShowsError()
    {
        using var context = TestContext.Create();
        context.Manual.Result = ManualOpenResult.Failure("マニュアルを開けませんでした。");
        using var viewModel = context.CreateViewModel();

        viewModel.OpenManualCommand.Execute(null);

        Assert.Equal("マニュアルを開けませんでした。", viewModel.StatusMessage);
        Assert.Equal("マニュアルを開けませんでした。", context.Dialog.LastErrorMessage);
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

        /// <summary>正式マニュアル表示の委譲結果を観測するテスト用サービス</summary>
        public StubManualService Manual { get; } = new();

        /// <summary>編集入力と確認結果を制御するテスト用サービス</summary>
        public StubDialogService Dialog { get; } = new();

        /// <summary>設定ファイルの選択と読み書きを制御するテスト用サービス</summary>
        public StubSettingsFileService Settings { get; } = new();

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
                new SettingsTransferService(),
                Browser,
                Manual,
                Dialog,
                Settings,
                new ImmediateDispatcher(),
                new ApplicationInfo(null, "v0.2.0", true),
                cancellation,
                startupError);

        /// <summary>待機中のHTTP取得をテストから完了可能にする操作</summary>
        public void ReleaseFetch() => fetcher.Release();

        /// <summary>テスト用インポート文書を監視対象名だけ差し替えて生成</summary>
        public static string CreateImportContent(string name) => $$"""
            {
              "format": "argus-settings",
              "formatVersion": 1,
              "targets": [
                {
                  "name": "{{name}}",
                  "url": "https://example.org/",
                  "mode": "htmlText",
                  "isEnabled": true,
                  "memo": null,
                  "cssSelector": null
                }
              ]
            }
            """;

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

    /// <summary>ファイル展開やブラウザ起動を行わず結果を返すマニュアル境界</summary>
    private sealed class StubManualService : IManualService
    {
        /// <summary>マニュアルを開く操作の呼び出し回数</summary>
        public int OpenCount { get; private set; }

        /// <summary>テストから指定するマニュアル起動結果</summary>
        public ManualOpenResult Result { get; set; } = ManualOpenResult.Success;

        /// <summary>呼び出し回数を記録して指定結果を返却</summary>
        public ManualOpenResult Open()
        {
            OpenCount++;
            return Result;
        }
    }

    /// <summary>画面を開かず利用者操作をキャンセルとして返すUI境界</summary>
    private sealed class StubDialogService : IDialogService
    {
        /// <summary>設定時に編集画面から保存するテスト入力</summary>
        public WatchTargetInput? EditorInput { get; set; }

        /// <summary>削除確認で返す利用者選択</summary>
        public bool ConfirmDelete { get; set; }

        /// <summary>設定置換確認で返す利用者選択</summary>
        public bool ConfirmImport { get; set; }

        /// <summary>直近に表示を依頼されたエラーメッセージ</summary>
        public string? LastErrorMessage { get; private set; }

        /// <summary>直近に差分表示を依頼された比較結果</summary>
        public ContentDiff? LastDiff { get; private set; }

        /// <summary>直近に差分表示を依頼された監視対象</summary>
        public WatchTarget? LastDiffTarget { get; private set; }

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

        /// <summary>設定置換確認を表示せずテスト指定の結果を返却</summary>
        public Task<bool> ConfirmImportSettingsAsync(
            int currentTargetCount,
            int importedTargetCount,
            CancellationToken cancellationToken) =>
            Task.FromResult(ConfirmImport);

        /// <summary>テストではエラー画面を表示せず完了</summary>
        public Task ShowErrorAsync(string message, CancellationToken cancellationToken)
        {
            LastErrorMessage = message;
            return Task.CompletedTask;
        }

        /// <summary>画面を開かず差分表示の引数だけを記録</summary>
        public Task ShowContentDiffAsync(
            WatchTarget target,
            ContentDiff diff,
            CancellationToken cancellationToken)
        {
            LastDiffTarget = target;
            LastDiff = diff;
            return Task.CompletedTask;
        }
    }

    /// <summary>OSのファイル選択と読み書きを行わず操作結果を記録するテスト境界</summary>
    private sealed class StubSettingsFileService : ISettingsFileService
    {
        /// <summary>テストから指定するインポート元パス</summary>
        public string? ImportPath { get; set; }

        /// <summary>テストから指定するインポート内容</summary>
        public string? ImportContent { get; set; }

        /// <summary>テストから指定するエクスポート先パス</summary>
        public string? ExportPath { get; set; }

        /// <summary>直近の書き込み先</summary>
        public string? LastWrittenPath { get; private set; }

        /// <summary>直近の書き込み内容</summary>
        public string? LastWrittenContent { get; private set; }

        /// <summary>インポート元選択をテスト指定値で返却</summary>
        public Task<string?> PickImportPathAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ImportPath);

        /// <summary>エクスポート先選択をテスト指定値で返却</summary>
        public Task<string?> PickExportPathAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ExportPath);

        /// <summary>テスト指定のインポート内容を返却</summary>
        public Task<string> ReadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(ImportContent ?? string.Empty);

        /// <summary>書き込み内容を記録して成功を返却</summary>
        public Task WriteAsync(
            string path,
            string content,
            CancellationToken cancellationToken)
        {
            LastWrittenPath = path;
            LastWrittenContent = content;
            return Task.CompletedTask;
        }
    }

    /// <summary>Coreイベントを呼び出し元スレッドで即時反映するUIスレッド境界</summary>
    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        /// <summary>テストではスレッド切替を行わず処理を実行</summary>
        public void Dispatch(Action action) => action();
    }
}
