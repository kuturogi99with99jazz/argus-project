using Argus.Core.Models;

namespace Argus.Core.Services;

/// <summary>取得・正規化・ハッシュ化の試行結果</summary>
public sealed record CheckAttempt(string? ContentHash, string? ErrorMessage)
{
    /// <summary>正常なハッシュが得られたかを内容値から一貫して判定</summary>
    public bool IsSuccess => ContentHash is not null;
    /// <summary>成功時の戻り値生成を呼び出し側で統一するためのファクトリ</summary>
    public static CheckAttempt Success(string contentHash) =>
        new(contentHash, null);
    /// <summary>失敗情報の生成を呼び出し側で統一するためのファクトリ</summary>
    public static CheckAttempt Failure(string errorMessage) =>
        new(null, errorMessage);
}


/// <summary>監視対象の取得結果を前回値と比較して判定するサービス</summary>
public sealed class WatchCheckService
{
    private readonly IWebPageFetcher fetcher;
    private readonly IComparisonContentExtractor contentExtractor;
    private readonly IHashService hashService;
    private readonly TimeProvider timeProvider;

    /// <summary>取得、正規化、ハッシュ化、時刻取得の責務を組み合わせ</summary>
    public WatchCheckService(
        IWebPageFetcher fetcher,
        IComparisonContentExtractor contentExtractor,
        IHashService hashService,
        TimeProvider? timeProvider = null)
    {
        this.fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
        this.contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));
        this.hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>外部I/Oの失敗を呼び出し側へ伝播しつつデータを非同期に取得</summary>
    public async Task<CheckAttempt> FetchHashAsync(
        WatchTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);

        try
        {
            var html = await fetcher
                .FetchAsync(target.Url, cancellationToken)
                .ConfigureAwait(false);
            var comparisonContent = contentExtractor.Extract(target, html);
            return CheckAttempt.Success(hashService.Compute(comparisonContent));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return CheckAttempt.Failure("ページの取得がタイムアウトしました。");
        }
        catch (HttpRequestException)
        {
            return CheckAttempt.Failure("ページを取得できませんでした。");
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            NotSupportedException or
            InvalidOperationException)
        {
            return CheckAttempt.Failure("ページの解析に失敗しました。");
        }
    }

    /// <summary>取得失敗時に正常スナップショットを生成せず確認結果を確定</summary>
    public async Task<CheckResult> CheckAsync(
        WatchTarget target,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var attempt = await FetchHashAsync(target, cancellationToken).ConfigureAwait(false);
        var completedAt = timeProvider.GetUtcNow();

        if (!attempt.IsSuccess)
        {
            return new CheckResult(
                operationId,
                target.Id,
                CheckStatus.Error,
                completedAt,
                null,
                attempt.ErrorMessage);
        }

        var status = target.PreviousSnapshot switch
        {
            null => CheckStatus.FirstFetch,
            { ContentHash: var previousHash }
                when string.Equals(
                    previousHash,
                    attempt.ContentHash,
                    StringComparison.Ordinal) =>
                CheckStatus.Unchanged,
            _ => CheckStatus.Updated
        };

        return new CheckResult(
            operationId,
            target.Id,
            status,
            completedAt,
            attempt.ContentHash,
            null);
    }
}
