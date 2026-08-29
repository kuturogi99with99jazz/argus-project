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
            });
    }
}
