using Argus.Core.Models;
using Argus.ViewModels;

namespace Argus.Tests.ViewModels;

/// <summary>差分結果からダイアログ表示値への変換を検証するテスト</summary>
public sealed class ContentDiffDialogViewModelTests
{
    /// <summary>差分種別、前回値、今回値、件数を利用者向け表示へ変換することを検証</summary>
    [Fact]
    public void Constructor_MapsEntryLabelsAndSummary()
    {
        var target = new WatchTarget(
            Guid.NewGuid(),
            "Example",
            new Uri("https://example.com/"),
            WatchMode.HtmlWhole,
            true,
            null,
            null);
        var diff = new ContentDiff([
            new ContentDiffEntry(ChangeKind.Added, null, "added"),
            new ContentDiffEntry(ChangeKind.Removed, "removed", null),
            new ContentDiffEntry(ChangeKind.Changed, "old", "new")]);

        var viewModel = new ContentDiffDialogViewModel(target, diff);

        Assert.Equal("「Example」の差分", viewModel.Title);
        Assert.Equal("比較方式: HTML全体比較", viewModel.ModeText);
        Assert.Equal("追加 1件 / 削除 1件 / 変更 1件", viewModel.Summary);
        Assert.Collection(
            viewModel.Entries,
            added =>
            {
                Assert.True(added.IsAdded);
                Assert.Equal("（なし）", added.PreviousText);
                Assert.Equal("added", added.CurrentText);
            },
            removed =>
            {
                Assert.True(removed.IsRemoved);
                Assert.Equal("removed", removed.PreviousText);
                Assert.Equal("（なし）", removed.CurrentText);
            },
            changed =>
            {
                Assert.True(changed.IsChanged);
                Assert.Equal("old", changed.PreviousText);
                Assert.Equal("new", changed.CurrentText);
                Assert.Equal(
                    [new ContentDiffSegmentViewModel("old", true)],
                    changed.PreviousSegments);
                Assert.Equal(
                    [new ContentDiffSegmentViewModel("new", true)],
                    changed.CurrentSegments);
            });
    }

    /// <summary>Coreが生成した文字列内セグメントを表示用値へ変換することを検証。</summary>
    [Fact]
    public void Constructor_MapsInlineSegmentsWithoutChangingText()
    {
        var target = new WatchTarget(
            Guid.NewGuid(),
            "Example",
            new Uri("https://example.com/"),
            WatchMode.HtmlText,
            true,
            null,
            null);
        var diff = new ContentDiff([
            new ContentDiffEntry(
                ChangeKind.Changed,
                "prefix-old",
                "prefix-new",
                [
                    new ContentDiffSegment("prefix-", false),
                    new ContentDiffSegment("old", true)
                ],
                [
                    new ContentDiffSegment("prefix-", false),
                    new ContentDiffSegment("new", true)
                ])]);

        var entry = Assert.Single(new ContentDiffDialogViewModel(target, diff).Entries);

        Assert.Equal("prefix-old", string.Concat(entry.PreviousSegments.Select(segment => segment.Text)));
        Assert.Equal("prefix-new", string.Concat(entry.CurrentSegments.Select(segment => segment.Text)));
        Assert.Collection(
            entry.PreviousSegments,
            unchanged => Assert.False(unchanged.IsChanged),
            changed => Assert.True(changed.IsChanged));
        Assert.Collection(
            entry.CurrentSegments,
            unchanged => Assert.False(unchanged.IsChanged),
            changed => Assert.True(changed.IsChanged));
    }

    /// <summary>セグメントを持たない既存の差分は行全体の変更として表示することを検証。</summary>
    [Fact]
    public void Constructor_WhenInlineSegmentsAreMissing_FallsBackToWholeText()
    {
        var target = new WatchTarget(
            Guid.NewGuid(),
            "Example",
            new Uri("https://example.com/"),
            WatchMode.HtmlWhole,
            true,
            null,
            null);
        var diff = new ContentDiff([
            new ContentDiffEntry(ChangeKind.Changed, "old", "new")]);

        var entry = Assert.Single(new ContentDiffDialogViewModel(target, diff).Entries);

        Assert.Equal([new ContentDiffSegmentViewModel("old", true)], entry.PreviousSegments);
        Assert.Equal([new ContentDiffSegmentViewModel("new", true)], entry.CurrentSegments);
    }

    /// <summary>追加削除の空側プレースホルダーを変更部分として扱わないことを検証。</summary>
    [Fact]
    public void Constructor_WhenTextIsMissing_DoesNotHighlightPlaceholder()
    {
        var target = new WatchTarget(
            Guid.NewGuid(),
            "Example",
            new Uri("https://example.com/"),
            WatchMode.HtmlWhole,
            true,
            null,
            null);
        var diff = new ContentDiff([
            new ContentDiffEntry(ChangeKind.Added, null, "added")]);

        var entry = Assert.Single(new ContentDiffDialogViewModel(target, diff).Entries);

        var previous = Assert.Single(entry.PreviousSegments);
        Assert.Equal("（なし）", previous.Text);
        Assert.False(previous.IsChanged);
    }
}
