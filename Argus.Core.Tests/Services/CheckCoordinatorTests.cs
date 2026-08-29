using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>
/// チェック調停処理が、同時実行数、重複要求、キャンセル、エラー時の保存条件を満たすことを検証します。
/// </summary>
/// <summary>複数対象のチェック実行調整を検証するテスト</summary>
public sealed class CheckCoordinatorTests
{
    /// <summary>
    /// 有効な監視対象だけを全件チェックの対象に含めることを検証します。
    /// </summary>
    [Fact]
    public async Task StartAll_QueuesOnlyEnabledTargets()
    {
        var targets = new[]
        {
            CreateTarget(true),
            CreateTarget(false),
            CreateTarget(true)
        };
        var coordinator = CreateCoordinator(targets, new ImmediateFetcher());

        var tasks = coordinator.StartAll(CancellationToken.None);
        var results = await Task.WhenAll(tasks);

        Assert.Equal(2, tasks.Count);
        Assert.All(results, result => Assert.Equal(CheckStatus.FirstFetch, result.Status));
        Assert.All(results, result => Assert.Null(result.Diff));
    }

    /// <summary>正常チェックで抽出済み比較内容をスナップショットへ保存することを検証</summary>
    [Fact]
    public async Task SuccessfulCheck_SavesComparisonContentAndReturnsNoDiffOnFirstFetch()
    {
        var target = CreateTarget(true);
        var repository = new WatchTargetRepository(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var coordinator = CreateCoordinator(repository, new FixedFetcher("first"));

        var result = await Assert.Single(
            coordinator.StartSelected([target.Id], CancellationToken.None));

        Assert.Equal(CheckStatus.FirstFetch, result.Status);
        Assert.Null(result.Diff);
        Assert.Equal("first", repository.Find(target.Id)?.PreviousSnapshot?.ComparisonContent);
    }

    /// <summary>更新あり時に前回と今回の比較内容から差分を返すことを検証</summary>
    [Fact]
    public async Task UpdatedCheck_ReturnsDiffAndStoresOnlyLatestComparisonContent()
    {
        var checkedAt = new WatchSnapshot(
            new Sha256HashService().Compute("old"),
            DateTimeOffset.UtcNow,
            "old");
        var target = CreateTarget(true) with { PreviousSnapshot = checkedAt };
        var repository = new WatchTargetRepository(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var coordinator = CreateCoordinator(repository, new FixedFetcher("new"));

        var result = await Assert.Single(
            coordinator.StartSelected([target.Id], CancellationToken.None));

        Assert.Equal(CheckStatus.Updated, result.Status);
        var entry = Assert.Single(result.Diff!.Entries);
        Assert.Equal(ChangeKind.Changed, entry.Kind);
        Assert.Equal("old", entry.PreviousText);
        Assert.Equal("new", entry.CurrentText);
        Assert.Equal("new", repository.Find(target.Id)?.PreviousSnapshot?.ComparisonContent);
    }

    /// <summary>比較内容がない既存スナップショットでもハッシュ判定と内容更新を継続することを検証</summary>
    [Fact]
    public async Task UpdatedCheck_WhenPreviousComparisonContentIsMissing_KeepsHashBasedStatusAndStoresCurrentContent()
    {
        var target = CreateTarget(true) with
        {
            PreviousSnapshot = new WatchSnapshot(
                new Sha256HashService().Compute("old"),
                DateTimeOffset.UtcNow)
        };
        var repository = new WatchTargetRepository(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var coordinator = CreateCoordinator(repository, new FixedFetcher("new"));

        var result = await Assert.Single(
            coordinator.StartSelected([target.Id], CancellationToken.None));

        Assert.Equal(CheckStatus.Updated, result.Status);
        Assert.Null(result.Diff);
        Assert.Equal("new", repository.Find(target.Id)?.PreviousSnapshot?.ComparisonContent);
    }

    /// <summary>差分生成に失敗した場合に正常スナップショットと保存処理を保護することを検証</summary>
    [Fact]
    public async Task UpdatedCheck_WhenDiffGenerationFails_KeepsPreviousSnapshotAndDoesNotSave()
    {
        var snapshot = new WatchSnapshot(
            new Sha256HashService().Compute("old"),
            DateTimeOffset.UtcNow,
            "old");
        var target = CreateTarget(true) with { PreviousSnapshot = snapshot };
        var store = new RecordingMemoryStore();
        var repository = new WatchTargetRepository(
            store,
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var checkService = new WatchCheckService(
            new FixedFetcher("new"),
            new StubNormalizer(),
            new Sha256HashService());
        var coordinator = new CheckCoordinator(
            repository,
            checkService,
            contentDiffService: new ThrowingDiffService());

        var result = await Assert.Single(
            coordinator.StartSelected([target.Id], CancellationToken.None));

        Assert.Equal(CheckStatus.Error, result.Status);
        Assert.Equal("差分を生成できませんでした。", result.ErrorMessage);
        Assert.Equal(snapshot, repository.Find(target.Id)?.PreviousSnapshot);
        Assert.Equal(0, store.SaveCount);
    }


    /// <summary>
    /// 同一監視対象への重複チェック要求を受け付け、実行中件数を追跡できることを検証します。
    /// </summary>
    [Fact]
    public async Task StartSelected_AllowsDuplicateTargetAndTracksRunningCount()
    {
        var target = CreateTarget(true);
        var fetcher = new GatedFetcher();
        var coordinator = CreateCoordinator([target], fetcher);

        var tasks = coordinator.StartSelected(
            [target.Id, target.Id],
            CancellationToken.None);

        Assert.Equal(2, tasks.Count);
        Assert.Equal(2, coordinator.GetRunningCount(target.Id));

        fetcher.Release();
        await Task.WhenAll(tasks);

        Assert.Equal(0, coordinator.GetRunningCount(target.Id));
    }


    /// <summary>
    /// 同時実行数が 4 件を超えないことを検証します。
    /// </summary>
    [Fact]
    public async Task StartAll_DoesNotExceedFourConcurrentFetches()
    {
        var targets = Enumerable.Range(0, 6).Select(_ => CreateTarget(true)).ToArray();
        var fetcher = new ConcurrencyTrackingFetcher(expectedConcurrency: 4);
        var coordinator = CreateCoordinator(targets, fetcher);

        var tasks = coordinator.StartAll(CancellationToken.None);
        await fetcher.WaitUntilExpectedConcurrencyAsync();

        Assert.Equal(4, fetcher.MaximumConcurrency);

        fetcher.Release();
        await Task.WhenAll(tasks);
        Assert.Equal(4, fetcher.MaximumConcurrency);
    }


    /// <summary>
    /// 通信エラー時に前回の正常なスナップショットを保持することを検証します。
    /// </summary>
    [Fact]
    public async Task ErrorResult_KeepsPreviousSnapshot()
    {
        var snapshot = new WatchSnapshot(new string('a', 64), DateTimeOffset.UtcNow);
        var target = CreateTarget(true) with { PreviousSnapshot = snapshot };
        var repository = new WatchTargetRepository(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var coordinator = CreateCoordinator(
            repository,
            new ThrowingFetcher());

        var result = await Assert.Single(
            coordinator.StartSelected([target.Id], CancellationToken.None));

        Assert.Equal(CheckStatus.Error, result.Status);
        Assert.Equal(snapshot, repository.Find(target.Id)?.PreviousSnapshot);
    }


    /// <summary>
    /// 重複チェックの完了順に応じて、最新スナップショットを基準に更新判定を行うことを検証します。
    /// </summary>
    [Fact]
    public async Task DuplicateChecks_CommitInCompletionOrderAgainstLatestSnapshot()
    {
        var target = CreateTarget(true);
        var fetcher = new OrderedFetcher(expectedCalls: 2);
        var repository = new WatchTargetRepository(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        using var coordinator = CreateCoordinator(repository, fetcher);

        var tasks = coordinator.StartSelected(
            [target.Id, target.Id],
            CancellationToken.None);
        await fetcher.WaitForExpectedCallsAsync();

        fetcher.Complete(callIndex: 1, "second");
        var secondResult = await tasks[1];
        fetcher.Complete(callIndex: 0, "first");
        var firstResult = await tasks[0];

        Assert.Equal(CheckStatus.FirstFetch, secondResult.Status);
        Assert.Equal(CheckStatus.Updated, firstResult.Status);
        Assert.Equal(
            new Sha256HashService().Compute("first"),
            repository.Find(target.Id)?.PreviousSnapshot?.ContentHash);
    }


    /// <summary>
    /// 実行中および待機中のチェックをキャンセルしたときに、保存や破棄競合が起きないことを検証します。
    /// </summary>
    [Fact]
    public async Task Cancellation_DuringRunningAndQueuedChecks_DoesNotCommitOrRaceDisposal()
    {
        var targets = Enumerable.Range(0, 5).Select(_ => CreateTarget(true)).ToArray();
        var store = new RecordingMemoryStore();
        var repository = new WatchTargetRepository(
            store,
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, targets));
        var fetcher = new ConcurrencyTrackingFetcher(expectedConcurrency: 4);
        var coordinator = CreateCoordinator(repository, fetcher);
        using var cancellation = new CancellationTokenSource();

        var tasks = coordinator.StartAll(cancellation.Token);
        await fetcher.WaitUntilExpectedConcurrencyAsync();
        cancellation.Cancel();
        coordinator.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.WhenAll(tasks));
        Assert.All(tasks, task => Assert.True(task.IsCanceled));
        Assert.All(targets, target =>
            Assert.Equal(0, coordinator.GetRunningCount(target.Id)));
        Assert.Equal(0, store.SaveCount);
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static CheckCoordinator CreateCoordinator(
        IReadOnlyList<WatchTarget> targets,
        IWebPageFetcher fetcher)
    {
        var repository = new WatchTargetRepository(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, targets));
        return CreateCoordinator(repository, fetcher);
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static CheckCoordinator CreateCoordinator(
        WatchTargetRepository repository,
        IWebPageFetcher fetcher)
    {
        var checkService = new WatchCheckService(
            fetcher,
            new StubNormalizer(),
            new Sha256HashService());
        return new CheckCoordinator(repository, checkService);
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchTarget CreateTarget(bool enabled) =>
        new(
            Guid.NewGuid(),
            "Sample",
            new Uri($"https://example.com/{Guid.NewGuid():N}"),
            WatchMode.HtmlText,
            enabled,
            null,
            null);
    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class ImmediateFetcher : IWebPageFetcher
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken) =>
            Task.FromResult("<p>content</p>");
    }

    /// <summary>固定したHTMLを返して比較内容の保存を検証する取得スタブ</summary>
    private sealed class FixedFetcher(string html) : IWebPageFetcher
    {
        /// <summary>外部サイトへ接続せず指定HTMLを返却</summary>
        public Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken) =>
            Task.FromResult(html);
    }

    /// <summary>差分生成失敗時の保存保護を再現するテスト用サービス</summary>
    private sealed class ThrowingDiffService : IContentDiffService
    {
        /// <summary>差分生成を失敗させるためのテスト実装</summary>
        public ContentDiff Generate(string previousContent, string currentContent) =>
            throw new InvalidOperationException("diff");
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class ThrowingFetcher : IWebPageFetcher
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken) =>
            throw new HttpRequestException("network");
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class GatedFetcher : IWebPageFetcher
    {
        private readonly TaskCompletionSource gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public async Task<string> FetchAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            await gate.Task.WaitAsync(cancellationToken);
            return "<p>content</p>";
        }

        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public void Release() => gate.TrySetResult();
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class ConcurrencyTrackingFetcher(int expectedConcurrency)
        : IWebPageFetcher
    {
        private readonly TaskCompletionSource gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource expectedReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int currentConcurrency;
        private int maximumConcurrency;
        /// <summary>テスト対象の結果を副作用なく観測するための状態値</summary>
        public int MaximumConcurrency => Volatile.Read(ref maximumConcurrency);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public async Task<string> FetchAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref currentConcurrency);
            UpdateMaximum(current);
            if (current == expectedConcurrency)
            {
                expectedReached.TrySetResult();
            }

            try
            {
                await gate.Task.WaitAsync(cancellationToken);
                return "<p>content</p>";
            }
            finally
            {
                Interlocked.Decrement(ref currentConcurrency);
            }
        }

        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public Task WaitUntilExpectedConcurrencyAsync() =>
            expectedReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public void Release() => gate.TrySetResult();
        /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
        private void UpdateMaximum(int value)
        {
            while (true)
            {
                var currentMaximum = Volatile.Read(ref maximumConcurrency);
                if (value <= currentMaximum ||
                    Interlocked.CompareExchange(
                        ref maximumConcurrency,
                        value,
                        currentMaximum) == currentMaximum)
                {
                    return;
                }
            }
        }
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class StubNormalizer : IComparisonContentExtractor
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public string Extract(WatchTarget target, string html) => html;
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class MemoryStore : ITargetStore
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task SaveAsync(
            TargetStoreDocument document,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class RecordingMemoryStore : ITargetStore
    {
        private int saveCount;
        /// <summary>テスト対象の結果を副作用なく観測するための状態値</summary>
        public int SaveCount => Volatile.Read(ref saveCount);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task SaveAsync(
            TargetStoreDocument document,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref saveCount);
            return Task.CompletedTask;
        }
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class OrderedFetcher(int expectedCalls) : IWebPageFetcher
    {
        private readonly object sync = new();
        private readonly List<TaskCompletionSource<string>> calls = [];
        private readonly TaskCompletionSource expectedReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<string> FetchAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            TaskCompletionSource<string> call;
            lock (sync)
            {
                call = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                calls.Add(call);
                if (calls.Count == expectedCalls)
                {
                    expectedReached.TrySetResult();
                }
            }

            return call.Task.WaitAsync(cancellationToken);
        }

        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public Task WaitForExpectedCallsAsync() =>
            expectedReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public void Complete(int callIndex, string html)
        {
            TaskCompletionSource<string> call;
            lock (sync)
            {
                call = calls[callIndex];
            }

            call.TrySetResult(html);
        }
    }
}
