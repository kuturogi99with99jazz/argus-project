using System.Drawing;

namespace Argus.WinForms.Presentation;

/// <summary>アプリケーションで統一して使用する配色</summary>
public static class SummerPalette
{
    /// <summary>Backgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Background { get; } = FromHtml("#F4FAFD");
    /// <summary>Surfaceに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Surface { get; } = FromHtml("#FFFFFF");
    /// <summary>Primaryに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Primary { get; } = FromHtml("#0277BD");
    /// <summary>PrimaryHoverに対応する共通配色を画面間で再利用するための色</summary>
    public static Color PrimaryHover { get; } = FromHtml("#01579B");
    /// <summary>Accentに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Accent { get; } = FromHtml("#00ACC1");
    /// <summary>Sunに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Sun { get; } = FromHtml("#F9A825");
    /// <summary>Leafに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Leaf { get; } = FromHtml("#2E7D32");
    /// <summary>TextPrimaryに対応する共通配色を画面間で再利用するための色</summary>
    public static Color TextPrimary { get; } = FromHtml("#17324D");
    /// <summary>TextSecondaryに対応する共通配色を画面間で再利用するための色</summary>
    public static Color TextSecondary { get; } = FromHtml("#526D7A");
    /// <summary>Borderに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Border { get; } = FromHtml("#B8D8E8");
    /// <summary>Selectionに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Selection { get; } = FromHtml("#B3E5FC");
    /// <summary>SelectionTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color SelectionText { get; } = FromHtml("#102A43");
    /// <summary>DisabledBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color DisabledBackground { get; } = FromHtml("#E8F1F5");
    /// <summary>DisabledTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color DisabledText { get; } = FromHtml("#718792");
    /// <summary>Dangerに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Danger { get; } = FromHtml("#C62828");
    /// <summary>Focusに対応する共通配色を画面間で再利用するための色</summary>
    public static Color Focus { get; } = FromHtml("#00695C");
    /// <summary>GridHeaderに対応する共通配色を画面間で再利用するための色</summary>
    public static Color GridHeader { get; } = FromHtml("#D9F0FA");
    /// <summary>GridAlternateに対応する共通配色を画面間で再利用するための色</summary>
    public static Color GridAlternate { get; } = FromHtml("#F7FCFE");
    /// <summary>UncheckedBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color UncheckedBackground { get; } = FromHtml("#ECEFF1");
    /// <summary>UncheckedTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color UncheckedText { get; } = FromHtml("#455A64");
    /// <summary>InitialBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color InitialBackground { get; } = FromHtml("#E3F2FD");
    /// <summary>InitialTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color InitialText { get; } = FromHtml("#0D47A1");
    /// <summary>UnchangedBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color UnchangedBackground { get; } = FromHtml("#E8F5E9");
    /// <summary>UnchangedTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color UnchangedText { get; } = FromHtml("#1B5E20");
    /// <summary>UpdatedBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color UpdatedBackground { get; } = FromHtml("#FFF3E0");
    /// <summary>UpdatedTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color UpdatedText { get; } = FromHtml("#BF360C");
    /// <summary>ErrorBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color ErrorBackground { get; } = FromHtml("#FFEBEE");
    /// <summary>ErrorTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color ErrorText { get; } = FromHtml("#B71C1C");
    /// <summary>CheckingBackgroundに対応する共通配色を画面間で再利用するための色</summary>
    public static Color CheckingBackground { get; } = FromHtml("#E0F7FA");
    /// <summary>CheckingTextに対応する共通配色を画面間で再利用するための色</summary>
    public static Color CheckingText { get; } = FromHtml("#006064");
    /// <summary>画面間で一貫した配色と操作状態を再利用するための表示処理</summary>
    private static Color FromHtml(string value) => ColorTranslator.FromHtml(value);
}
