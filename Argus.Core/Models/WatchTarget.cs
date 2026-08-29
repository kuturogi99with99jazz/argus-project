namespace Argus.Core.Models;

/// <summary>監視対象について最後に正常取得した内容のスナップショット</summary>
public sealed record WatchSnapshot(
    string ContentHash,
    DateTimeOffset CheckedAtUtc,
    string? ComparisonContent = null);

/// <summary>ユーザーが登録したWebページと確認状態</summary>
public sealed record WatchTarget(
    Guid Id,
    string Name,
    Uri Url,
    WatchMode Mode,
    bool IsEnabled,
    string? Memo,
    WatchSnapshot? PreviousSnapshot,
    string? CssSelector = null);

/// <summary>監視対象の登録・編集に使う未検証入力値</summary>
public sealed record WatchTargetInput(
    string Name,
    string Url,
    WatchMode Mode,
    bool IsEnabled,
    string? Memo,
    string? CssSelector = null);
