using System.Drawing;
using Argus.Core.Models;

namespace Argus.WinForms.Presentation;

/// <summary>チェック状態に対応する画面表示属性</summary>
public sealed record StatusAppearance(
    string DisplayName,
    Color Background,
    Color Foreground);

/// <summary>チェック状態を画面表示属性へ変換する機能</summary>
public static class CheckStatusAppearance
{
    /// <summary>内部状態を変更せず呼び出し側に必要な情報を提供</summary>
    public static StatusAppearance Get(CheckStatus status) =>
        status switch
        {
            CheckStatus.Unchecked => new(
                "未確認",
                SummerPalette.UncheckedBackground,
                SummerPalette.UncheckedText),
            CheckStatus.FirstFetch => new(
                "初回取得",
                SummerPalette.InitialBackground,
                SummerPalette.InitialText),
            CheckStatus.Unchanged => new(
                "更新なし",
                SummerPalette.UnchangedBackground,
                SummerPalette.UnchangedText),
            CheckStatus.Updated => new(
                "更新あり",
                SummerPalette.UpdatedBackground,
                SummerPalette.UpdatedText),
            CheckStatus.Error => new(
                "エラー",
                SummerPalette.ErrorBackground,
                SummerPalette.ErrorText),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    /// <summary>確認処理中であることを最終結果と区別する表示属性</summary>
    public static StatusAppearance Checking { get; } = new(
        "チェック中",
        SummerPalette.CheckingBackground,
        SummerPalette.CheckingText);
}
