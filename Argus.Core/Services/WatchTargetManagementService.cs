using Argus.Core.Models;
using Argus.Core.Persistence;

namespace Argus.Core.Services;

/// <summary>対象ごとの実行中チェック数を提供する契約</summary>
public interface ICheckExecutionState
{
    /// <summary>編集や削除の可否判定に必要な実行中件数を提供</summary>
    int GetRunningCount(Guid targetId);
}


/// <summary>監視対象の登録・編集・削除結果</summary>
public sealed class WatchTargetChangeResult
{
    /// <summary>成功、検証失敗、永続化失敗を一つの結果型へ集約</summary>
    private WatchTargetChangeResult(
        bool isSuccess,
        WatchTarget? target,
        IReadOnlyList<ValidationError> validationErrors,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        Target = target;
        ValidationErrors = validationErrors;
        ErrorMessage = errorMessage;
    }


    /// <summary>操作が成功したかどうか</summary>
    public bool IsSuccess { get; }

    /// <summary>操作後の監視対象</summary>
    public WatchTarget? Target { get; }

    /// <summary>入力検証エラー一覧</summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    /// <summary>永続化などで発生したエラーメッセージ</summary>
    public string? ErrorMessage { get; }
    /// <summary>成功時の戻り値生成を呼び出し側で統一するためのファクトリ</summary>
    public static WatchTargetChangeResult Success(WatchTarget? target) =>
        new(true, target, Array.Empty<ValidationError>(), null);
    /// <summary>失敗情報の生成を呼び出し側で統一するためのファクトリ</summary>
    public static WatchTargetChangeResult ValidationFailure(
        IReadOnlyList<ValidationError> errors) =>
        new(false, null, errors, null);
    /// <summary>失敗情報の生成を呼び出し側で統一するためのファクトリ</summary>
    public static WatchTargetChangeResult Failure(string errorMessage) =>
        new(false, null, Array.Empty<ValidationError>(), errorMessage);
}

/// <summary>入力検証、実行状態確認、永続化を調停して変更の整合性を維持</summary>
public sealed class WatchTargetManagementService
{
    private readonly WatchTargetRepository repository;
    private readonly ICheckExecutionState executionState;

    /// <summary>検証、実行状態、永続化を調停する依存関係を構成</summary>
    public WatchTargetManagementService(
        WatchTargetRepository repository,
        ICheckExecutionState executionState)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.executionState = executionState
            ?? throw new ArgumentNullException(nameof(executionState));
    }

    /// <summary>永続化の成功後だけ状態変更を確定して整合性を維持</summary>
    public async Task<WatchTargetChangeResult> AddAsync(
        WatchTargetInput input,
        CancellationToken cancellationToken)
    {
        var validation = WatchTargetValidator.Create(Guid.NewGuid(), input, null);
        if (!validation.IsValid || validation.Value is null)
        {
            return WatchTargetChangeResult.ValidationFailure(validation.Errors);
        }

        var target = validation.Value;
        try
        {
            return await repository.CommitAsync(
                    current => new RepositoryUpdate<WatchTargetChangeResult>(
                        current.Concat([target]).ToArray(),
                        WatchTargetChangeResult.Success(target)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or TargetStoreException)
        {
            return WatchTargetChangeResult.Failure(
                "監視対象を保存できませんでした。変更前のデータを維持しています。");
        }
    }

    /// <summary>永続化の成功後だけ状態変更を確定して整合性を維持</summary>
    public async Task<WatchTargetChangeResult> EditAsync(
        Guid id,
        WatchTargetInput input,
        CancellationToken cancellationToken)
    {
        var existing = repository.Find(id);
        if (existing is null)
        {
            return WatchTargetChangeResult.Failure("編集対象が見つかりません。");
        }

        if (executionState.GetRunningCount(id) > 0)
        {
            return WatchTargetChangeResult.Failure(
                "チェック中の監視対象は編集できません。");
        }

        var inputValidation = WatchTargetValidator.Validate(input);
        if (!inputValidation.IsValid || inputValidation.Value is null)
        {
            return WatchTargetChangeResult.ValidationFailure(inputValidation.Errors);
        }

        try
        {
            return await repository.CommitAsync(
                    current =>
                    {
                        var currentTarget = current.FirstOrDefault(target => target.Id == id);
                        if (currentTarget is null)
                        {
                            return new RepositoryUpdate<WatchTargetChangeResult>(
                                current,
                                WatchTargetChangeResult.Failure(
                                    "編集対象が見つかりません。"));
                        }

                        if (executionState.GetRunningCount(id) > 0)
                        {
                            return new RepositoryUpdate<WatchTargetChangeResult>(
                                current,
                                WatchTargetChangeResult.Failure(
                                    "チェック中の監視対象は編集できません。"));
                        }

                        var preserveSnapshot =
                            currentTarget.Mode == inputValidation.Value.Mode &&
                            string.Equals(
                                currentTarget.CssSelector,
                                inputValidation.Value.CssSelector,
                                StringComparison.Ordinal) &&
                            currentTarget.Url.Equals(
                                new Uri(inputValidation.Value.Url));
                        var created = WatchTargetValidator.Create(
                            currentTarget.Id,
                            inputValidation.Value,
                            preserveSnapshot ? currentTarget.PreviousSnapshot : null);
                        var changedTarget = created.Value!;
                        var changed = current
                            .Select(target => target.Id == id ? changedTarget : target)
                            .ToArray();

                        return new RepositoryUpdate<WatchTargetChangeResult>(
                            changed,
                            WatchTargetChangeResult.Success(changedTarget));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or TargetStoreException)
        {
            return WatchTargetChangeResult.Failure(
                "監視対象を保存できませんでした。変更前のデータを維持しています。");
        }
    }

    /// <summary>永続化の成功後だけ状態変更を確定して整合性を維持</summary>
    public async Task<WatchTargetChangeResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var existing = repository.Find(id);
        if (existing is null)
        {
            return WatchTargetChangeResult.Failure("削除対象が見つかりません。");
        }

        if (executionState.GetRunningCount(id) > 0)
        {
            return WatchTargetChangeResult.Failure(
                "チェック中の監視対象は削除できません。");
        }

        try
        {
            return await repository.CommitAsync(
                    current =>
                    {
                        if (executionState.GetRunningCount(id) > 0)
                        {
                            return new RepositoryUpdate<WatchTargetChangeResult>(
                                current,
                                WatchTargetChangeResult.Failure(
                                    "チェック中の監視対象は削除できません。"));
                        }

                        var target = current.FirstOrDefault(item => item.Id == id);
                        if (target is null)
                        {
                            return new RepositoryUpdate<WatchTargetChangeResult>(
                                current,
                                WatchTargetChangeResult.Failure(
                                    "削除対象が見つかりません。"));
                        }

                        return new RepositoryUpdate<WatchTargetChangeResult>(
                            current.Where(item => item.Id != id).ToArray(),
                            WatchTargetChangeResult.Success(target));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or TargetStoreException)
        {
            return WatchTargetChangeResult.Failure(
                "監視対象を削除できませんでした。変更前のデータを維持しています。");
        }
    }

    /// <summary>インポート済み監視対象を全件検証後に一括置換して永続化</summary>
    public async Task<WatchTargetChangeResult> ReplaceAllAsync(
        IReadOnlyList<WatchTarget> targets,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var nextTargets = targets.ToArray();

        if (repository.GetAll().Any(target => executionState.GetRunningCount(target.Id) > 0))
        {
            return WatchTargetChangeResult.Failure(
                "チェック中は設定をインポートできません。");
        }

        try
        {
            return await repository.CommitAsync(
                    current =>
                    {
                        if (current.Any(target => executionState.GetRunningCount(target.Id) > 0))
                        {
                            return new RepositoryUpdate<WatchTargetChangeResult>(
                                current,
                                WatchTargetChangeResult.Failure(
                                    "チェック中は設定をインポートできません。"));
                        }

                        return new RepositoryUpdate<WatchTargetChangeResult>(
                            nextTargets,
                            WatchTargetChangeResult.Success(null));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or TargetStoreException)
        {
            return WatchTargetChangeResult.Failure(
                "設定を保存できませんでした。変更前のデータを維持しています。");
        }
    }
}
