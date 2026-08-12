using System.Reflection;

namespace Argus.Tests.Services;

/// <summary>正式ユーザーマニュアルの埋め込み資産とHTML契約を検証するテスト</summary>
public sealed class ManualResourcesTests
{
    private static readonly Assembly ApplicationAssembly = typeof(App).Assembly;

    /// <summary>オフライン表示に必要なHTMLと画像が実行アセンブリへ埋め込まれることを検証</summary>
    [Theory]
    [InlineData("Argus.Manual.index.html")]
    [InlineData("Argus.Manual.main.png")]
    [InlineData("Argus.Manual.entry.png")]
    public void ApplicationAssembly_ContainsManualResource(string resourceName)
    {
        using var resource = ApplicationAssembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(resource);
        Assert.True(resource.Length > 0);
    }

    /// <summary>正式マニュアルが対象版と画像参照とアプリ内導線を説明することを検証</summary>
    [Fact]
    public void ManualHtml_DescribesCurrentAvaloniaApplication()
    {
        using var resource = ApplicationAssembly.GetManifestResourceStream("Argus.Manual.index.html");
        Assert.NotNull(resource);
        using var reader = new StreamReader(resource);

        var html = reader.ReadToEnd();

        Assert.Contains("Avalonia正式版", html, StringComparison.Ordinal);
        Assert.Contains("v0.2.0", html, StringComparison.Ordinal);
        Assert.Contains("main.png", html, StringComparison.Ordinal);
        Assert.Contains("entry.png", html, StringComparison.Ordinal);
        Assert.Contains("「マニュアル」ボタン", html, StringComparison.Ordinal);
    }
}
