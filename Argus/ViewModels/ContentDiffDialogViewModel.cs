using Argus.Core.Models;

namespace Argus.ViewModels;

/// <summary>差分ダイアログの見出しと行一覧を表示用に保持するViewModel</summary>
public sealed class ContentDiffDialogViewModel : ViewModelBase
{
    /// <summary>監視対象と差分結果を受け取り表示用の行へ変換</summary>
    public ContentDiffDialogViewModel(WatchTarget target, ContentDiff diff)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(diff);
        Title = $"「{target.Name}」の差分";
        ModeText = $"比較方式: {WatchModeOption.GetDisplayName(target.Mode)}";
        Entries = diff.Entries.Select(entry => new ContentDiffEntryViewModel(entry)).ToArray();
        Summary = $"追加 {Entries.Count(entry => entry.IsAdded)}件 / " +
            $"削除 {Entries.Count(entry => entry.IsRemoved)}件 / " +
            $"変更 {Entries.Count(entry => entry.IsChanged)}件";
    }

    /// <summary>差分ダイアログのタイトル</summary>
    public string Title { get; }

    /// <summary>監視対象に設定された比較方式の表示名</summary>
    public string ModeText { get; }

    /// <summary>差分種別ごとの件数</summary>
    public string Summary { get; }

    /// <summary>文書順に並んだ差分行</summary>
    public IReadOnlyList<ContentDiffEntryViewModel> Entries { get; }
}

/// <summary>差分1行を種別ラベルと前後の値へ変換する表示用ViewModel</summary>
public sealed class ContentDiffEntryViewModel
{
    /// <summary>Coreの差分行を空値を含む表示可能な値へ変換</summary>
    public ContentDiffEntryViewModel(ContentDiffEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        PreviousText = entry.PreviousText ?? "（なし）";
        CurrentText = entry.CurrentText ?? "（なし）";
        IsAdded = entry.Kind == ChangeKind.Added;
        IsRemoved = entry.Kind == ChangeKind.Removed;
        IsChanged = entry.Kind == ChangeKind.Changed;
        PreviousSegments = CreateSegments(
            entry.PreviousSegments,
            entry.PreviousText,
            entry.Kind is ChangeKind.Removed or ChangeKind.Changed);
        CurrentSegments = CreateSegments(
            entry.CurrentSegments,
            entry.CurrentText,
            entry.Kind is ChangeKind.Added or ChangeKind.Changed);
    }

    /// <summary>差分が追加種別かどうか</summary>
    public bool IsAdded { get; }

    /// <summary>差分が削除種別かどうか</summary>
    public bool IsRemoved { get; }

    /// <summary>差分が変更種別かどうか</summary>
    public bool IsChanged { get; }

    /// <summary>前回比較対象の表示値</summary>
    public string PreviousText { get; }

    /// <summary>今回比較対象の表示値</summary>
    public string CurrentText { get; }

    /// <summary>前回比較対象を変更有無付きで表示するセグメント一覧</summary>
    public IReadOnlyList<ContentDiffSegmentViewModel> PreviousSegments { get; }

    /// <summary>今回比較対象を変更有無付きで表示するセグメント一覧</summary>
    public IReadOnlyList<ContentDiffSegmentViewModel> CurrentSegments { get; }

    /// <summary>Coreのセグメントまたは旧形式の行文字列を表示用セグメントへ変換。</summary>
    private static IReadOnlyList<ContentDiffSegmentViewModel> CreateSegments(
        IReadOnlyList<ContentDiffSegment>? segments,
        string? text,
        bool fallbackIsChanged)
    {
        if (segments is not null)
        {
            return segments
                .Select(segment => new ContentDiffSegmentViewModel(segment.Text, segment.IsChanged))
                .ToArray();
        }

        return text is null
            ? [new ContentDiffSegmentViewModel("（なし）", false)]
            : [new ContentDiffSegmentViewModel(text, fallbackIsChanged)];
    }
}

/// <summary>差分ダイアログで文字列片の変更有無を保持するViewModel。</summary>
public sealed record ContentDiffSegmentViewModel(string Text, bool IsChanged)
{
    /// <summary>文字列片が変更されていないかどうか</summary>
    public bool IsUnchanged => !IsChanged;
}
