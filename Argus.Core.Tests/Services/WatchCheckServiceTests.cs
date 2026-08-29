using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>単一対象の更新確認結果を検証するテスト</summary>
public sealed class WatchCheckServiceTests
{
    private static readonly DateTimeOffset CompletedAt =
        new(2026, 7, 31, 0, 0, 0, TimeSpan.Zero);
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CheckAsync_WithoutSnapshot_ReturnsFirstFetch()
    {
        var service = CreateService("new-hash");
        var target = CreateTarget(null);

        var result = await service.CheckAsync(
            target,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(CheckStatus.FirstFetch, result.Status);
        Assert.Equal("new-hash", result.ContentHash);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CheckAsync_WithSameHash_ReturnsUnchanged()
    {
        var service = CreateService("same-hash");
        var target = CreateTarget(new WatchSnapshot("same-hash", CompletedAt.AddDays(-1)));

        var result = await service.CheckAsync(
            target,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(CheckStatus.Unchanged, result.Status);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CheckAsync_WithDifferentHash_ReturnsUpdated()
    {
        var service = CreateService("new-hash");
        var target = CreateTarget(new WatchSnapshot("old-hash", CompletedAt.AddDays(-1)));

        var result = await service.CheckAsync(
            target,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(CheckStatus.Updated, result.Status);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CheckAsync_WhenFetchFails_ReturnsErrorWithoutSnapshotData()
    {
        var service = new WatchCheckService(
            new ThrowingFetcher(),
            new StubNormalizer(),
            new StubHashService("unused"),
            new FixedTimeProvider(CompletedAt));

        var result = await service.CheckAsync(
            CreateTarget(new WatchSnapshot("old-hash", CompletedAt.AddDays(-1))),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(CheckStatus.Error, result.Status);
        Assert.Null(result.ContentHash);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task CheckAsync_WhenNormalizationFails_ReturnsErrorWithoutSnapshotData()
    {
        var service = new WatchCheckService(
            new StubFetcher(),
            new ThrowingNormalizer(),
            new StubHashService("unused"),
            new FixedTimeProvider(CompletedAt));

        var result = await service.CheckAsync(
            CreateTarget(new WatchSnapshot("old-hash", CompletedAt.AddDays(-1))),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(CheckStatus.Error, result.Status);
        Assert.Null(result.ContentHash);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    /// <summary>監視対象固有のモードとセレクタを抽出処理へ渡すことを検証</summary>
    [Fact]
    public async Task FetchHashAsync_PassesTargetAndFetchedHtmlToContentExtractor()
    {
        var extractor = new RecordingContentExtractor();
        var service = new WatchCheckService(
            new StubFetcher(),
            extractor,
            new StubHashService("hash"),
            new FixedTimeProvider(CompletedAt));
        var target = CreateTarget(null) with
        {
            Mode = WatchMode.CssSelector,
            CssSelector = ".news"
        };

        var attempt = await service.FetchHashAsync(target, CancellationToken.None);

        Assert.True(attempt.IsSuccess);
        Assert.Equal("content", attempt.ComparisonContent);
        Assert.Same(target, extractor.Target);
        Assert.Equal("<p>content</p>", extractor.Html);
    }

    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchCheckService CreateService(string hash) =>
        new(
            new StubFetcher(),
            new StubNormalizer(),
            new StubHashService(hash),
            new FixedTimeProvider(CompletedAt));
    /// <summary>各テストで共通する前提データを一貫して構築するための補助処理</summary>
    private static WatchTarget CreateTarget(WatchSnapshot? snapshot) =>
        new(
            Guid.NewGuid(),
            "Sample",
            new Uri("https://example.com/"),
            WatchMode.HtmlText,
            true,
            null,
            snapshot);
    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class StubFetcher : IWebPageFetcher
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken) =>
            Task.FromResult("<p>content</p>");
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class ThrowingFetcher : IWebPageFetcher
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken) =>
            throw new HttpRequestException("network");
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class StubNormalizer : IComparisonContentExtractor
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public string Extract(WatchTarget target, string html) => "content";
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class ThrowingNormalizer : IComparisonContentExtractor
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public string Extract(WatchTarget target, string html) =>
            throw new InvalidOperationException("parse");
    }

    /// <summary>抽出処理へ渡された監視条件とHTMLを記録するテスト補助型</summary>
    private sealed class RecordingContentExtractor : IComparisonContentExtractor
    {
        /// <summary>最後に抽出要求された監視対象</summary>
        public WatchTarget? Target { get; private set; }

        /// <summary>最後に抽出要求されたHTML</summary>
        public string? Html { get; private set; }

        /// <summary>入力を記録してハッシュ化可能な固定内容を返却</summary>
        public string Extract(WatchTarget target, string html)
        {
            Target = target;
            Html = html;
            return "content";
        }
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class StubHashService(string hash) : IHashService
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public string Compute(string content) => hash;
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public override DateTimeOffset GetUtcNow() => value;
    }
}
