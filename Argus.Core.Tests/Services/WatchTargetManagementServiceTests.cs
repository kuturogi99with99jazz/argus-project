using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>監視対象の登録・編集・削除を検証するテスト</summary>
public sealed class WatchTargetManagementServiceTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task AddAsync_WithValidInput_GeneratesIdWithoutSnapshotAndPersists()
    {
        var store = new MemoryStore();
        var repository = new WatchTargetRepository(store, TargetStoreDocument.Empty);
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.AddAsync(
            new WatchTargetInput(
                "Sample",
                "https://example.com/",
                WatchMode.HtmlText,
                true,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Target);
        Assert.NotEqual(Guid.Empty, result.Target.Id);
        Assert.Null(result.Target.PreviousSnapshot);
        Assert.Single(repository.GetAll());
        Assert.Single(store.LastSaved.Targets);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task EditAsync_WhenUrlChanges_ClearsSnapshotAndKeepsId()
    {
        var target = CreateTarget();
        var repository = CreateRepository(target);
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.EditAsync(
            target.Id,
            new WatchTargetInput(
                "Changed",
                "https://example.com/new",
                WatchMode.HtmlText,
                true,
                "memo"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(target.Id, result.Target?.Id);
        Assert.Null(result.Target?.PreviousSnapshot);
    }

    /// <summary>比較モード変更時に異なる比較値を引き継がないことを検証</summary>
    [Fact]
    public async Task EditAsync_WhenModeChanges_ClearsSnapshot()
    {
        var target = CreateTarget();
        var repository = CreateRepository(target);
        var service = new WatchTargetManagementService(repository, new IdleExecutionState());

        var result = await service.EditAsync(
            target.Id,
            new WatchTargetInput(target.Name, target.Url.AbsoluteUri, WatchMode.HtmlWhole, true, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Target?.PreviousSnapshot);
    }

    /// <summary>CSSセレクタ変更時に異なる抽出範囲の比較値を引き継がないことを検証</summary>
    [Fact]
    public async Task EditAsync_WhenCssSelectorChanges_ClearsSnapshot()
    {
        var target = CreateTarget() with { Mode = WatchMode.CssSelector, CssSelector = ".old" };
        var repository = CreateRepository(target);
        var service = new WatchTargetManagementService(repository, new IdleExecutionState());

        var result = await service.EditAsync(
            target.Id,
            new WatchTargetInput(target.Name, target.Url.AbsoluteUri, target.Mode, true, null, ".new"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Target?.PreviousSnapshot);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task EditAsync_WhenUrlPathCaseChanges_ClearsSnapshot()
    {
        var target = CreateTarget() with
        {
            Url = new Uri("https://example.com/Path")
        };
        var repository = CreateRepository(target);
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.EditAsync(
            target.Id,
            new WatchTargetInput(
                target.Name,
                "https://example.com/path",
                target.Mode,
                target.IsEnabled,
                target.Memo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Target?.PreviousSnapshot);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task EditAsync_WhenOnlyMetadataChanges_KeepsSnapshot()
    {
        var target = CreateTarget();
        var repository = CreateRepository(target);
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.EditAsync(
            target.Id,
            new WatchTargetInput(
                "Changed",
                target.Url.AbsoluteUri,
                target.Mode,
                false,
                "memo"),
            CancellationToken.None);

        Assert.Equal(target.PreviousSnapshot, result.Target?.PreviousSnapshot);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task DeleteAsync_WhenTargetIsChecking_DoesNotDelete()
    {
        var target = CreateTarget();
        var repository = CreateRepository(target);
        var service = new WatchTargetManagementService(
            repository,
            new BusyExecutionState(target.Id));

        var result = await service.DeleteAsync(target.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(repository.GetAll());
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task DeleteAsync_WhenTargetIsIdle_RemovesTargetAndSnapshot()
    {
        var target = CreateTarget();
        var store = new MemoryStore();
        var repository = new WatchTargetRepository(
            store,
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.DeleteAsync(target.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(target, result.Target);
        Assert.Empty(repository.GetAll());
        Assert.Empty(store.LastSaved.Targets);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task AddAsync_WhenInputIsInvalid_DoesNotSave()
    {
        var store = new MemoryStore();
        var repository = new WatchTargetRepository(store, TargetStoreDocument.Empty);
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.AddAsync(
            new WatchTargetInput(
                " ",
                "ftp://example.com/",
                WatchMode.HtmlText,
                true,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ValidationErrors);
        Assert.Empty(repository.GetAll());
        Assert.Equal(0, store.SaveCount);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task AddAsync_WhenSaveFails_KeepsEmptyRepository()
    {
        var repository = new WatchTargetRepository(
            new ThrowingStore(),
            TargetStoreDocument.Empty);
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.AddAsync(
            new WatchTargetInput(
                "Sample",
                "https://example.com/",
                WatchMode.HtmlText,
                true,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repository.GetAll());
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task EditAsync_WhenSaveFails_KeepsOriginalTarget()
    {
        var target = CreateTarget();
        var repository = new WatchTargetRepository(
            new ThrowingStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.EditAsync(
            target.Id,
            new WatchTargetInput(
                "Changed",
                "https://example.com/changed",
                WatchMode.HtmlText,
                false,
                "changed"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(target, repository.Find(target.Id));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task DeleteAsync_WhenSaveFails_KeepsOriginalTarget()
    {
        var target = CreateTarget();
        var repository = new WatchTargetRepository(
            new ThrowingStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
        var service = new WatchTargetManagementService(
            repository,
            new IdleExecutionState());

        var result = await service.DeleteAsync(target.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(target, repository.Find(target.Id));
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchTargetRepository CreateRepository(WatchTarget target) =>
        new(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));
    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchTarget CreateTarget() =>
        new(
            Guid.NewGuid(),
            "Original",
            new Uri("https://example.com/"),
            WatchMode.HtmlText,
            true,
            null,
            new WatchSnapshot(new string('a', 64), DateTimeOffset.UtcNow));
    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class IdleExecutionState : ICheckExecutionState
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public int GetRunningCount(Guid targetId) => 0;
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class BusyExecutionState(Guid busyTargetId) : ICheckExecutionState
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public int GetRunningCount(Guid targetId) =>
            targetId == busyTargetId ? 1 : 0;
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class MemoryStore : ITargetStore
    {
        /// <summary>テスト対象の結果を副作用なく観測するための状態値</summary>
        public int SaveCount { get; private set; }
        /// <summary>テスト対象の結果を副作用なく観測するための状態値</summary>
        public TargetStoreDocument LastSaved { get; private set; } =
            TargetStoreDocument.Empty;
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(LastSaved);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task SaveAsync(
            TargetStoreDocument document,
            CancellationToken cancellationToken)
        {
            SaveCount++;
            LastSaved = document;
            return Task.CompletedTask;
        }
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class ThrowingStore : ITargetStore
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task SaveAsync(
            TargetStoreDocument document,
            CancellationToken cancellationToken) =>
            throw new IOException("save failed");
    }
}
