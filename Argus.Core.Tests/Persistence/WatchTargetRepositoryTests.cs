using Argus.Core.Models;
using Argus.Core.Persistence;

namespace Argus.Core.Tests.Persistence;

/// <summary>監視対象リポジトリの更新整合性を検証するテスト</summary>
public sealed class WatchTargetRepositoryTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CommitAsync_WhenSaveFails_KeepsPreviousMemoryState()
    {
        var original = CreateTarget("Original");
        var store = new FailingStore();
        var repository = new WatchTargetRepository(
            store,
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [original]));

        await Assert.ThrowsAsync<IOException>(() =>
            repository.CommitAsync(
                targets => new RepositoryUpdate<bool>(
                    [CreateTarget("Changed")],
                    true),
                CancellationToken.None));

        Assert.Single(repository.GetAll());
        Assert.Equal("Original", repository.GetAll()[0].Name);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CommitAsync_WhenCalledConcurrently_SerializesSaveAndMemoryUpdates()
    {
        var store = new GatedStore();
        var repository = new WatchTargetRepository(
            store,
            TargetStoreDocument.Empty);

        var first = repository.CommitAsync(
            targets => new RepositoryUpdate<bool>(
                targets.Concat([CreateTarget("First")]).ToArray(),
                true),
            CancellationToken.None);
        await store.WaitForFirstSaveAsync();
        var second = repository.CommitAsync(
            targets => new RepositoryUpdate<bool>(
                targets.Concat([CreateTarget("Second")]).ToArray(),
                true),
            CancellationToken.None);

        Assert.Equal(1, store.SaveCount);
        store.ReleaseFirstSave();
        await Task.WhenAll(first, second);

        Assert.Equal(2, store.SaveCount);
        Assert.Equal(1, store.MaximumConcurrency);
        Assert.Equal(["First", "Second"], repository.GetAll().Select(x => x.Name));
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchTarget CreateTarget(string name) =>
        new(
            Guid.NewGuid(),
            name,
            new Uri("https://example.com/"),
            WatchMode.HtmlText,
            true,
            null,
            null);
    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class FailingStore : ITargetStore
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task SaveAsync(TargetStoreDocument document, CancellationToken cancellationToken) =>
            throw new IOException("Simulated save failure.");
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class GatedStore : ITargetStore
    {
        private readonly TaskCompletionSource firstSaveStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseFirstSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int currentConcurrency;
        private int maximumConcurrency;
        private int saveCount;
        /// <summary>テスト対象の結果を副作用なく観測するための状態値</summary>
        public int SaveCount => Volatile.Read(ref saveCount);
        /// <summary>テスト対象の結果を副作用なく観測するための状態値</summary>
        public int MaximumConcurrency => Volatile.Read(ref maximumConcurrency);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public async Task SaveAsync(
            TargetStoreDocument document,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref saveCount);
            var concurrency = Interlocked.Increment(ref currentConcurrency);
            InterlockedExtensions.UpdateMaximum(ref maximumConcurrency, concurrency);
            try
            {
                if (call == 1)
                {
                    firstSaveStarted.TrySetResult();
                    await releaseFirstSave.Task.WaitAsync(cancellationToken);
                }
            }
            finally
            {
                Interlocked.Decrement(ref currentConcurrency);
            }
        }

        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public Task WaitForFirstSaveAsync() =>
            firstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        /// <summary>非同期テストの実行順と完了タイミングを決定的に制御するための操作</summary>
        public void ReleaseFirstSave() => releaseFirstSave.TrySetResult();
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private static class InterlockedExtensions
    {
        /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
        public static void UpdateMaximum(ref int target, int value)
        {
            while (true)
            {
                var current = Volatile.Read(ref target);
                if (value <= current ||
                    Interlocked.CompareExchange(ref target, value, current) == current)
                {
                    return;
                }
            }
        }
    }
}
