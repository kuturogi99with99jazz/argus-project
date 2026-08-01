using System.Reflection;

namespace Argus.WinForms.Presentation;

/// <summary>画面表示に使用するアプリケーション情報</summary>
public sealed record ApplicationInfo(
    string? Copyright,
    string Version,
    bool IsDebug);

/// <summary>実行環境からアプリケーション情報を取得するプロバイダー</summary>
public sealed class ApplicationInfoProvider
{
    /// <summary>内部状態を変更せず呼び出し側に必要な情報を提供</summary>
    public ApplicationInfo Get()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var copyright = assembly
            .GetCustomAttribute<AssemblyCopyrightAttribute>()
            ?.Copyright;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var version = FormatVersion(
            informationalVersion,
            assembly.GetName().Version);

#if DEBUG
        const bool isDebug = true;
#else
        const bool isDebug = false;
#endif

        return new ApplicationInfo(
            string.IsNullOrWhiteSpace(copyright) ? null : copyright,
            version,
            isDebug);
    }

    /// <summary>画面表示に不要なメタデータを除き安定したバージョン表記を生成</summary>
    internal static string FormatVersion(
        string? informationalVersion,
        Version? fallbackVersion)
    {
        var value = informationalVersion?.Split('+', 2)[0];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = fallbackVersion is null
                ? "Unknown"
                : $"{fallbackVersion.Major}.{fallbackVersion.Minor}.{fallbackVersion.Build}";
        }

        return $"v{value}";
    }
}
