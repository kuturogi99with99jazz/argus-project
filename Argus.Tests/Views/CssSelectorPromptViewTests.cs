namespace Argus.Tests.Views;

/// <summary>CSSセレクタ相談UIに必要なXAML接続を検証するテスト</summary>
public sealed class CssSelectorPromptViewTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    /// <summary>編集画面にCSSモード限定のAI相談ボタンとアクセシビリティ情報が存在することを検証</summary>
    [Fact]
    public void TargetEditWindow_ContainsCssSelectorPromptAction()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "Views", "TargetEditWindow.axaml"));

        Assert.Contains("ShowCssSelectorPromptCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsCssSelectorVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes=\"aiAction\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AIにCSSセレクタを相談", xaml, StringComparison.Ordinal);
        Assert.Contains("IconAi", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name", xaml, StringComparison.Ordinal);
        Assert.Contains("TextBox Grid.Column=\"0\" Text=\"{Binding CssSelector}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"28\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"28\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment=\"Center\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>AI相談ボタンの主要な操作状態で文字と背景のスタイルが定義されることを検証</summary>
    [Fact]
    public void AppStyles_AiActionDefinesInteractiveStates()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "App.axaml"));

        Assert.Contains("Style Selector=\"Button.aiAction\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style Selector=\"Button.aiAction:pointerover\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style Selector=\"Button.aiAction:pressed\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style Selector=\"Button.aiAction:focus\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style Selector=\"Button.aiAction:disabled\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Foreground\" Value=\"White\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Foreground\" Value=\"{DynamicResource AppDisabledForegroundBrush}\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>プロンプト小窓がコピーコマンドと読み取り専用表示欄を持つことを検証</summary>
    [Fact]
    public void CssSelectorPromptWindow_ContainsCopyActionAndReadOnlyPrompt()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "Argus", "Views", "CssSelectorPromptWindow.axaml"));

        Assert.Contains("CopyCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("PromptText", xaml, StringComparison.Ordinal);
        Assert.Contains("IsReadOnly=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("CopyStatus", xaml, StringComparison.Ordinal);
        Assert.Contains("OperationError", xaml, StringComparison.Ordinal);
        Assert.Contains("<ScrollViewer", xaml, StringComparison.Ordinal);
        Assert.Contains("<ScrollViewer Grid.Row=\"0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid RowDefinitions=\"*,Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<StackPanel Grid.Row=\"1\" Spacing=\"4\">", xaml, StringComparison.Ordinal);
    }
}
