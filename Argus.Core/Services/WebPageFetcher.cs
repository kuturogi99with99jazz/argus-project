using System.Text;
using System.Text.RegularExpressions;

namespace Argus.Core.Services;

/// <summary>共有HttpClientを使ってWebページ本文を取得する実装</summary>
public sealed partial class WebPageFetcher : IWebPageFetcher
{
    private const int MetaScanLength = 8192;
    private readonly HttpClient httpClient;

    static WebPageFetcher()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }


    /// <summary>共有HTTPクライアントを受け取り接続資源の再利用を保証</summary>
    public WebPageFetcher(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>外部I/Oの失敗を呼び出し側へ伝播しつつデータを非同期に取得</summary>
    public async Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "HTTPまたはHTTPSの絶対URLが必要です。",
                nameof(uri));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("Argus/0.1");
        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content
            .ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var headerCharset = response.Content.Headers.ContentType?.CharSet?.Trim('"', '\'');
        var encoding = DetermineEncoding(bytes, headerCharset);
        return Decode(bytes, encoding);
    }

    /// <summary>Web応答の文字コード判定を取得処理から分離して一貫性を維持</summary>
    private static Encoding DetermineEncoding(byte[] bytes, string? headerCharset)
    {
        if (!string.IsNullOrWhiteSpace(headerCharset))
        {
            return GetStrictEncoding(headerCharset);
        }

        if (TryGetBomEncoding(bytes, out var bomEncoding))
        {
            return bomEncoding;
        }

        var scanLength = Math.Min(bytes.Length, MetaScanLength);
        var headerText = Encoding.Latin1.GetString(bytes, 0, scanLength);
        var match = MetaCharsetPattern().Match(headerText);
        if (match.Success)
        {
            return GetStrictEncoding(match.Groups["charset"].Value);
        }

        return new UTF8Encoding(false, true);
    }

    /// <summary>Web応答の文字コード判定を取得処理から分離して一貫性を維持</summary>
    private static Encoding GetStrictEncoding(string name)
    {
        try
        {
            return Encoding.GetEncoding(
                name.Trim(),
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"未対応の文字コードです: {name}",
                exception);
        }
    }

    /// <summary>Web応答の文字コード判定を取得処理から分離して一貫性を維持</summary>
    private static bool TryGetBomEncoding(byte[] bytes, out Encoding encoding)
    {
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
        {
            encoding = new UTF8Encoding(false, true);
            return true;
        }

        if (bytes.AsSpan().StartsWith(Encoding.UTF32.Preamble))
        {
            encoding = new UTF32Encoding(false, true, true);
            return true;
        }

        var utf32BigEndian = new UTF32Encoding(true, true, true);
        if (bytes.AsSpan().StartsWith(utf32BigEndian.GetPreamble()))
        {
            encoding = utf32BigEndian;
            return true;
        }

        if (bytes.AsSpan().StartsWith(Encoding.Unicode.Preamble))
        {
            encoding = new UnicodeEncoding(false, true, true);
            return true;
        }

        if (bytes.AsSpan().StartsWith(Encoding.BigEndianUnicode.Preamble))
        {
            encoding = new UnicodeEncoding(true, true, true);
            return true;
        }

        encoding = null!;
        return false;
    }

    /// <summary>Web応答の文字コード判定を取得処理から分離して一貫性を維持</summary>
    private static string Decode(byte[] bytes, Encoding encoding)
    {
        try
        {
            var preamble = encoding.GetPreamble();
            var offset = preamble.Length > 0 && bytes.AsSpan().StartsWith(preamble)
                ? preamble.Length
                : 0;
            return encoding.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                "ページの文字コードを正しくデコードできませんでした。",
                exception);
        }
    }

    [GeneratedRegex(
        """<meta\b[^>]*charset\s*=\s*["']?\s*(?<charset>[A-Za-z0-9._:-]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    /// <summary>Web応答の文字コード判定を取得処理から分離して一貫性を維持</summary>
    private static partial Regex MetaCharsetPattern();
}
