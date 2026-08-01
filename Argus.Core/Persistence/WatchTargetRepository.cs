using Argus.Core.Models;

namespace Argus.Core.Persistence;

/// <summary>リポジトリ更新後の状態と結果を表す値</summary>
public sealed record RepositoryUpdate<T>(
    IReadOnlyList<WatchTarget> Targets,
    T Result);

/// <summary>監視対象のメモリ上の一覧を管理するリポジトリ</summary>
public sealed class WatchTargetRepository
{
    private readonly ITargetStore store;
    private readonly SemaphoreSlim commitLock = new(1, 1);
    private WatchTarget[] targets;

    /// <summary>永続化境界と初期文書を受け取りメモリ状態を構成</summary>
    public WatchTargetRepository(
        ITargetStore store,
        TargetStoreDocument initialDocument)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentNullException.ThrowIfNull(initialDocument);
        targets = initialDocument.Targets.ToArray();
    }

    /// <summary>内部状態を変更せず呼び出し側に必要な情報を提供</summary>
    public IReadOnlyList<WatchTarget> GetAll() =>
        Array.AsReadOnly(targets);
    /// <summary>内部状態を変更せず呼び出し側に必要な情報を提供</summary>
    public WatchTarget? Find(Guid id) =>
        targets.FirstOrDefault(target => target.Id == id);

    public async Task<T> CommitAsync<T>(
        Func<IReadOnlyList<WatchTarget>, RepositoryUpdate<T>> updateFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updateFactory);
        await commitLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = Array.AsReadOnly(targets);
            var update = updateFactory(current);
            var nextTargets = update.Targets.ToArray();
            var document = new TargetStoreDocument(
                TargetStoreDocument.CurrentSchemaVersion,
                nextTargets);

            await store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            targets = nextTargets;
            return update.Result;
        }
        finally
        {
            commitLock.Release();
        }
    }
}
