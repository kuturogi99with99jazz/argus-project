using Argus.Core.Models;

namespace Argus.Core.Persistence;

/// <summary>保存対象とJSON形式のバージョンをまとめた文書</summary>
public sealed record TargetStoreDocument(
    int SchemaVersion,
    IReadOnlyList<WatchTarget> Targets)
{
    public const int CurrentSchemaVersion = 1;
    /// <summary>保存データがない場合に共有する空の初期文書</summary>
    public static TargetStoreDocument Empty { get; } =
        new(CurrentSchemaVersion, Array.Empty<WatchTarget>());
}


/// <summary>監視対象文書を永続化する契約</summary>
public interface ITargetStore
{
    /// <summary>保存方式をリポジトリから分離して監視対象文書を取得</summary>
    Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken);

    /// <summary>保存方式をリポジトリから分離して監視対象文書を永続化</summary>
    Task SaveAsync(TargetStoreDocument document, CancellationToken cancellationToken);
}


/// <summary>監視対象の永続化に失敗したことを示す例外</summary>
public sealed class TargetStoreException : Exception
{
    /// <summary>永続化エラーの説明を保持する例外を生成</summary>
    public TargetStoreException(string message)
        : base(message)
    {
    }


    /// <summary>原因例外を失わず永続化エラーとして境界を明確化</summary>
    public TargetStoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
