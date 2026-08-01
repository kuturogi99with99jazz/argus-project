using System.Net;
using System.Text;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>Webページ取得時の文字コード判定を検証するテスト</summary>
public sealed class WebPageFetcherTests
{
    static WebPageFetcherTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task FetchAsync_UsesHeaderCharset()
    {
        var encoding = Encoding.GetEncoding("shift_jis");
        using var client = CreateClient(
            encoding.GetBytes("<p>日本語</p>"),
            "text/html; charset=shift_jis");
        var fetcher = new WebPageFetcher(client);

        var result = await fetcher.FetchAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.Contains("日本語", result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task FetchAsync_UsesBomBeforeMeta()
    {
        var content = Encoding.Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes(
                """<meta charset="utf-8"><p>日本語</p>"""))
            .ToArray();
        using var client = CreateClient(content, "text/html");
        var fetcher = new WebPageFetcher(client);

        var result = await fetcher.FetchAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.Contains("日本語", result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData("""<meta charset="euc-jp"><p>日本語</p>""")]
    [InlineData("""<meta http-equiv="Content-Type" content="text/html; charset=euc-jp"><p>日本語</p>""")]
    public async Task FetchAsync_UsesMetaCharset(string html)
    {
        var encoding = Encoding.GetEncoding("euc-jp");
        using var client = CreateClient(encoding.GetBytes(html), "text/html");
        var fetcher = new WebPageFetcher(client);

        var result = await fetcher.FetchAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.Contains("日本語", result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task FetchAsync_UsesUtf8WhenCharsetIsMissing()
    {
        using var client = CreateClient(
            Encoding.UTF8.GetBytes("<p>日本語</p>"),
            "text/html");
        var fetcher = new WebPageFetcher(client);

        var result = await fetcher.FetchAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.Contains("日本語", result);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task FetchAsync_WhenStatusIsNotSuccess_Throws()
    {
        using var client = new HttpClient(
            new StubHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var fetcher = new WebPageFetcher(client);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            fetcher.FetchAsync(new Uri("https://example.com/"), CancellationToken.None));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task FetchAsync_WhenCharsetIsUnsupported_Throws()
    {
        using var client = CreateClient(
            Encoding.UTF8.GetBytes("<p>test</p>"),
            "text/html; charset=not-a-real-charset");
        var fetcher = new WebPageFetcher(client);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            fetcher.FetchAsync(new Uri("https://example.com/"), CancellationToken.None));
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static HttpClient CreateClient(byte[] content, string contentType)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
        };
        response.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        return new HttpClient(new StubHandler(response));
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
