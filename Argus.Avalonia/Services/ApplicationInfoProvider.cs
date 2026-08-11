using System.Reflection;

namespace Argus.Avalonia.Services;

/// <summary>画面下部へ表示するアプリケーション情報</summary>
public sealed record ApplicationInfo(string? Copyright, string Version, bool IsDebug);

/// <summary>実行アセンブリからバージョンとビルド種別を取得するプロバイダー</summary>
public sealed class ApplicationInfoProvider
{
    /// <summary>画面表示に不要なビルドメタデータを除いてアプリ情報を生成</summary>
    public ApplicationInfo Get()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var copyright = assembly
            .GetCustomAttribute<AssemblyCopyrightAttribute>()
            ?.Copyright;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var version = informationalVersion?.Split('+', 2)[0];
        if (string.IsNullOrWhiteSpace(version))
        {
            var fallback = assembly.GetName().Version;
            version = fallback is null
                ? "Unknown"
                : $"{fallback.Major}.{fallback.Minor}.{fallback.Build}";
        }

#if DEBUG
        const bool isDebug = true;
#else
        const bool isDebug = false;
#endif

        return new ApplicationInfo(
            string.IsNullOrWhiteSpace(copyright) ? null : copyright,
            $"v{version}",
            isDebug);
    }
}
