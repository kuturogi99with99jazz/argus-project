namespace Argus.Core.Models;

/// <summary>更新確認の結果を表す状態</summary>
public enum CheckStatus
{
    Unchecked,
    FirstFetch,
    Unchanged,
    Updated,
    Error
}
