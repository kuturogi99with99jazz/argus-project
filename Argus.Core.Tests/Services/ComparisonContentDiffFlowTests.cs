using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>三つの比較方式がチェック結果と差分生成へ接続されることを検証するテスト</summary>
public sealed class ComparisonContentDiffFlowTests
{
    /// <summary>方式ごとの抽出結果を正常スナップショットへ保存することを検証</summary>
    [Theory]
    [InlineData(WatchMode.HtmlText, null, "<html><body>  Old  </body></html>", "Old")]
    [InlineData(WatchMode.HtmlWhole, null, "<p>Old</p>", "<p>Old</p>")]
    [InlineData(WatchMode.CssSelector, ".item", "<p class='item'>Old</p><p>Ignore</p>", "<p class=\"item\">Old</p>")]
    public async Task FirstCheck_SavesExtractorContentForEachMode(
        WatchMode mode,
        string? selector,
        string html,
        string expectedContent)
    {
        var target = CreateTarget(mode, selector);
        var repository = CreateRepository(target);
        var checkService = CreateCheckService(new SequenceFetcher(html));
        var coordinator = new CheckCoordinator(repository, checkService);

        await Assert.Single(coordinator.StartSelected([target.Id], CancellationToken.None));

        Assert.Equal(
            expectedContent,
            repository.Find(target.Id)?.PreviousSnapshot?.ComparisonContent);
    }

    /// <summary>方式ごとに抽出された前回・今回内容から更新差分を生成することを検証</summary>
    [Theory]
    [InlineData(WatchMode.HtmlText, null, "<p>Old</p>", "<p>New</p>", "Old", "New")]
    [InlineData(WatchMode.HtmlWhole, null, "<p>Old</p>", "<p>New</p>", "<p>Old</p>", "<p>New</p>")]
    [InlineData(WatchMode.CssSelector, ".item", "<p class='item'>Old</p>", "<p class='item'>New</p>", "<p class=\"item\">Old</p>", "<p class=\"item\">New</p>")]
    public async Task UpdatedCheck_GeneratesDiffFromExtractorContentForEachMode(
        WatchMode mode,
        string? selector,
        string firstHtml,
        string secondHtml,
        string expectedPrevious,
        string expectedCurrent)
    {
        var target = CreateTarget(mode, selector);
        var repository = CreateRepository(target);
        var checkService = CreateCheckService(new SequenceFetcher(firstHtml, secondHtml));
        var coordinator = new CheckCoordinator(repository, checkService);

        await Assert.Single(coordinator.StartSelected([target.Id], CancellationToken.None));
        var updated = await Assert.Single(
            coordinator.StartSelected([target.Id], CancellationToken.None));

        var entry = Assert.Single(updated.Diff!.Entries);
        Assert.Equal(ChangeKind.Changed, entry.Kind);
        Assert.Equal(expectedPrevious, entry.PreviousText);
        Assert.Equal(expectedCurrent, entry.CurrentText);
    }

    /// <summary>指定した監視条件だけを差し替えたテスト対象を生成</summary>
    private static WatchTarget CreateTarget(WatchMode mode, string? selector) =>
        new(
            Guid.NewGuid(),
            "Sample",
            new Uri("https://example.com/"),
            mode,
            true,
            null,
            null,
            selector);

    /// <summary>テスト専用のメモリRepositoryを構成</summary>
    private static WatchTargetRepository CreateRepository(WatchTarget target) =>
        new(
            new MemoryStore(),
            new TargetStoreDocument(TargetStoreDocument.CurrentSchemaVersion, [target]));

    /// <summary>実際の比較内容抽出器を使うチェックサービスを構成</summary>
    private static WatchCheckService CreateCheckService(SequenceFetcher fetcher) =>
        new(
            fetcher,
            new ComparisonContentExtractor(new HtmlTextNormalizer()),
            new Sha256HashService());

    /// <summary>テストで保存処理を発生させずRepository境界だけを提供</summary>
    private sealed class MemoryStore : ITargetStore
    {
        /// <summary>テストでは初期文書を直接渡すため未使用の読み込み操作</summary>
        public Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(TargetStoreDocument.Empty);

        /// <summary>Repositoryのメモリ更新だけを成功させる保存操作</summary>
        public Task SaveAsync(TargetStoreDocument document, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>呼び出し順に保存済みHTMLを返してチェック間の比較を再現</summary>
    private sealed class SequenceFetcher(params string[] htmls) : IWebPageFetcher
    {
        private int index;

        /// <summary>指定順のHTMLを返却し、最後の値を以降も再利用</summary>
        public Task<string> FetchAsync(Uri uri, CancellationToken cancellationToken)
        {
            var currentIndex = Interlocked.Increment(ref index) - 1;
            return Task.FromResult(htmls[Math.Min(currentIndex, htmls.Length - 1)]);
        }
    }
}
