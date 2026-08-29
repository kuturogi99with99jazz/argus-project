namespace Argus.Core.Models;

/// <summary>比較対象の差分種別を表す値</summary>
public enum ChangeKind
{
    Added,
    Removed,
    Changed
}

/// <summary>差分行内の変更有無を保った表示単位。</summary>
public sealed record ContentDiffSegment(string Text, bool IsChanged);

/// <summary>前回と今回の比較対象における一つの行差分</summary>
public sealed record ContentDiffEntry(
    ChangeKind Kind,
    string? PreviousText,
    string? CurrentText,
    IReadOnlyList<ContentDiffSegment>? PreviousSegments = null,
    IReadOnlyList<ContentDiffSegment>? CurrentSegments = null);

/// <summary>UIへ渡す追加・削除・変更の差分結果</summary>
public sealed record ContentDiff
{
    /// <summary>差分結果を順序を保った読み取り専用一覧として保持</summary>
    public ContentDiff(IReadOnlyList<ContentDiffEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries.ToArray();
    }

    /// <summary>比較対象の文書順に並んだ差分行</summary>
    public IReadOnlyList<ContentDiffEntry> Entries { get; }

    /// <summary>表示対象となる差分行が存在するかどうか</summary>
    public bool HasChanges => Entries.Count > 0;
}
