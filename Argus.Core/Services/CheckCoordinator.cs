using System.Collections.Concurrent;
using Argus.Core.Models;
using Argus.Core.Persistence;

namespace Argus.Core.Services;

/// <summary>チェック実行数の変化を通知するイベント引数</summary>
public sealed class CheckExecutionChangedEventArgs(
    Guid targetId,
    int runningCount) : EventArgs
{
    /// <summary>実行数が変化した対象の識別子</summary>
    public Guid TargetId { get; } = targetId;
    /// <summary>同じ対象で待機または実行中の確認件数</summary>
    public int RunningCount { get; } = runningCount;
}


/// <summary>1件のチェック完了を通知するイベント引数</summary>
public sealed class CheckCompletedEventArgs(CheckResult result) : EventArgs
{
    /// <summary>完了したチェック結果</summary>
    public CheckResult Result { get; } = result;
}


/// <summary>複数対象のチェック実行と完了通知を調整するサービス</summary>
public sealed class CheckCoordinator : ICheckExecutionState, IDisposable
{
    private const int MaximumConcurrentFetches = 4;
    private readonly WatchTargetRepository repository;
    private readonly WatchCheckService checkService;
    private readonly IContentDiffService contentDiffService;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim fetchSemaphore =
        new(MaximumConcurrentFetches, MaximumConcurrentFetches);
    private readonly ConcurrentDictionary<Guid, int> runningCounts = new();

    /// <summary>並行取得数と保存コミット順を制御する依存関係を構成</summary>
    public CheckCoordinator(
        WatchTargetRepository repository,
        WatchCheckService checkService,
        TimeProvider? timeProvider = null,
        IContentDiffService? contentDiffService = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.checkService = checkService ?? throw new ArgumentNullException(nameof(checkService));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.contentDiffService = contentDiffService ?? new ContentDiffService();
    }

    public event EventHandler<CheckExecutionChangedEventArgs>? ExecutionChanged;

    public event EventHandler<CheckCompletedEventArgs>? CheckCompleted;
    /// <summary>対象ごとの実行中件数をスレッドセーフに取得</summary>
    public int GetRunningCount(Guid targetId) =>
        runningCounts.TryGetValue(targetId, out var count) ? count : 0;
    /// <summary>対象ごとの重複要求を許容しながら確認処理を非同期に開始</summary>
    public IReadOnlyList<Task<CheckResult>> StartAll(
        CancellationToken cancellationToken)
    {
        var targetIds = repository
            .GetAll()
            .Where(target => target.IsEnabled)
            .Select(target => target.Id)
            .ToArray();
        return StartSelected(targetIds, cancellationToken);
    }

    /// <summary>対象ごとの重複要求を許容しながら確認処理を非同期に開始</summary>
    public IReadOnlyList<Task<CheckResult>> StartSelected(
        IEnumerable<Guid> targetIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetIds);

        var tasks = new List<Task<CheckResult>>();
        foreach (var targetId in targetIds)
        {
            var target = repository.Find(targetId);
            if (target is null || !target.IsEnabled)
            {
                continue;
            }

            var count = runningCounts.AddOrUpdate(targetId, 1, (_, current) => current + 1);
            ExecutionChanged?.Invoke(
                this,
                new CheckExecutionChangedEventArgs(targetId, count));
            tasks.Add(RunAsync(target, Guid.NewGuid(), cancellationToken));
        }

        return tasks;
    }

    /// <summary>並行処理で使用する同期リソースを安全に解放</summary>
    public void Dispose()
    {
        // Shutdown cancellation is intentionally non-blocking. If work is still
        // unwinding, disposing the semaphore would race with its finally blocks.
        if (runningCounts.IsEmpty)
        {
            fetchSemaphore.Dispose();
        }
    }

    /// <summary>並行取得と直列コミットの責務を分離して結果整合性を維持</summary>
    private async Task<CheckResult> RunAsync(
        WatchTarget requestedTarget,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            CheckAttempt attempt;
            await fetchSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                attempt = await checkService
                    .FetchHashAsync(requestedTarget, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                fetchSemaphore.Release();
            }

            CheckResult result;
            if (!attempt.IsSuccess)
            {
                result = CreateErrorResult(
                    operationId,
                    requestedTarget.Id,
                    attempt.ErrorMessage ?? "チェックに失敗しました。");
            }
            else
            {
                try
                {
                    result = await repository.CommitAsync(
                            current => CreateSuccessfulUpdate(
                                current,
                                requestedTarget.Id,
                                operationId,
                                attempt.ContentHash!,
                                attempt.ComparisonContent!),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ContentDiffException)
                {
                    result = CreateErrorResult(
                        operationId,
                        requestedTarget.Id,
                        "差分を生成できませんでした。");
                }
                catch (Exception exception) when (
                    exception is IOException or
                    UnauthorizedAccessException or
                    TargetStoreException)
                {
                    result = CreateErrorResult(
                        operationId,
                        requestedTarget.Id,
                        "チェック結果を保存できませんでした。");
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                CheckCompleted?.Invoke(this, new CheckCompletedEventArgs(result));
            }

            return result;
        }
        finally
        {
            var count = runningCounts.AddOrUpdate(
                requestedTarget.Id,
                0,
                (_, current) => Math.Max(0, current - 1));
            if (count == 0)
            {
                runningCounts.TryRemove(requestedTarget.Id, out _);
            }

            ExecutionChanged?.Invoke(
                this,
                new CheckExecutionChangedEventArgs(requestedTarget.Id, count));
        }
    }

    /// <summary>並行取得と直列コミットの責務を分離して結果整合性を維持</summary>
    private RepositoryUpdate<CheckResult> CreateSuccessfulUpdate(
        IReadOnlyList<WatchTarget> current,
        Guid targetId,
        Guid operationId,
        string contentHash,
        string comparisonContent)
    {
        var target = current.FirstOrDefault(item => item.Id == targetId);
        if (target is null)
        {
            var missingResult = CreateErrorResult(
                operationId,
                targetId,
                "監視対象が見つかりません。");
            return new RepositoryUpdate<CheckResult>(current, missingResult);
        }

        var completedAt = timeProvider.GetUtcNow();
        var status = target.PreviousSnapshot switch
        {
            null => CheckStatus.FirstFetch,
            { ContentHash: var previousHash }
                when string.Equals(
                    previousHash,
                    contentHash,
                    StringComparison.Ordinal) =>
                CheckStatus.Unchanged,
            _ => CheckStatus.Updated
        };

        ContentDiff? diff = null;
        if (status == CheckStatus.Updated &&
            target.PreviousSnapshot?.ComparisonContent is { } previousComparisonContent)
        {
            try
            {
                diff = contentDiffService.Generate(
                    previousComparisonContent,
                    comparisonContent);
            }
            catch (ContentDiffException)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ContentDiffException(
                    "差分を生成できませんでした。",
                    exception);
            }
        }

        var changedTarget = target with
        {
            PreviousSnapshot = new WatchSnapshot(
                contentHash,
                completedAt,
                comparisonContent)
        };
        var changedTargets = current
            .Select(item => item.Id == targetId ? changedTarget : item)
            .ToArray();
        var result = new CheckResult(
            operationId,
            targetId,
            status,
            completedAt,
            contentHash,
            null,
            diff);

        return new RepositoryUpdate<CheckResult>(changedTargets, result);
    }

    /// <summary>並行取得と直列コミットの責務を分離して結果整合性を維持</summary>
    private CheckResult CreateErrorResult(
        Guid operationId,
        Guid targetId,
        string errorMessage) =>
        new(
            operationId,
            targetId,
            CheckStatus.Error,
            timeProvider.GetUtcNow(),
            null,
            errorMessage);
}
