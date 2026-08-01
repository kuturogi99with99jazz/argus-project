namespace Argus.Core.Models;

/// <summary>1回の更新確認で得られた判定結果</summary>
public sealed record CheckResult(
    Guid OperationId,
    Guid TargetId,
    CheckStatus Status,
    DateTimeOffset CompletedAtUtc,
    string? ContentHash,
    string? ErrorMessage);
