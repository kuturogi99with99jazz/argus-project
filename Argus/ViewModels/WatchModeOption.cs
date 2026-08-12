using Argus.Core.Models;

namespace Argus.ViewModels;

/// <summary>監視モードのCore値と利用者向け表示名を対応付ける選択肢</summary>
public sealed record WatchModeOption(WatchMode Value, string DisplayName)
{
    /// <summary>編集画面で選択可能な比較方式を表示順に提供</summary>
    public static IReadOnlyList<WatchModeOption> All { get; } =
    [
        new(WatchMode.HtmlText, "HTMLテキスト比較"),
        new(WatchMode.HtmlWhole, "HTML全体比較"),
        new(WatchMode.CssSelector, "CSSセレクタ比較")
    ];

    /// <summary>Core値に対応する利用者向け表示名を取得</summary>
    public static string GetDisplayName(WatchMode mode) =>
        All.First(option => option.Value == mode).DisplayName;
}
