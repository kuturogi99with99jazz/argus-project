using Avalonia;

namespace Argus.Avalonia;

/// <summary>WindowsとmacOSで共通するAvalonia版のエントリーポイント</summary>
internal static class Program
{
    /// <summary>デスクトップホストを初期化してアプリケーションライフサイクルを開始</summary>
    [STAThread]
    private static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>IDE拡張へ依存せず標準デスクトップホストを構築</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
