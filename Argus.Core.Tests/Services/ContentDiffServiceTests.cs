using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>比較対象の行差分をUI非依存の結果へ変換する処理を検証するテスト</summary>
public sealed class ContentDiffServiceTests
{
    /// <summary>追加された行を追加種別として返すことを検証</summary>
    [Fact]
    public void Generate_WhenLineIsAdded_ReturnsAddedEntry()
    {
        var diff = new ContentDiffService().Generate("before", "before\nafter");

        var entry = Assert.Single(diff.Entries);
        Assert.Equal(ChangeKind.Added, entry.Kind);
        Assert.Null(entry.PreviousText);
        Assert.Equal("after", entry.CurrentText);
    }

    /// <summary>削除された行を削除種別として返すことを検証</summary>
    [Fact]
    public void Generate_WhenLineIsRemoved_ReturnsRemovedEntry()
    {
        var diff = new ContentDiffService().Generate("before\nold", "before");

        var entry = Assert.Single(diff.Entries);
        Assert.Equal(ChangeKind.Removed, entry.Kind);
        Assert.Equal("old", entry.PreviousText);
        Assert.Null(entry.CurrentText);
    }

    /// <summary>対応する行の内容が変わった場合に旧値と新値を一組で返すことを検証</summary>
    [Fact]
    public void Generate_WhenLineIsChanged_ReturnsChangedEntry()
    {
        var diff = new ContentDiffService().Generate("before\nold\nafter", "before\nnew\nafter");

        var entry = Assert.Single(diff.Entries);
        Assert.Equal(ChangeKind.Changed, entry.Kind);
        Assert.Equal("old", entry.PreviousText);
        Assert.Equal("new", entry.CurrentText);
    }

    /// <summary>複数行の追加削除変更を元の文書順で保持することを検証</summary>
    [Fact]
    public void Generate_WhenMultipleChangesExist_PreservesChangeOrderAndEmptyLines()
    {
        var diff = new ContentDiffService().Generate(
            "same\nremoved\nold\n\nend",
            "same\nnew\n\nadded\nend");

        Assert.Collection(
            diff.Entries,
            changed =>
            {
                Assert.Equal(ChangeKind.Changed, changed.Kind);
                Assert.Equal("removed", changed.PreviousText);
                Assert.Equal("new", changed.CurrentText);
            },
            removed =>
            {
                Assert.Equal(ChangeKind.Removed, removed.Kind);
                Assert.Equal("old", removed.PreviousText);
            },
            added =>
            {
                Assert.Equal(ChangeKind.Added, added.Kind);
                Assert.Equal("added", added.CurrentText);
            });
    }

    /// <summary>同一内容では表示対象となる差分を返さないことを検証</summary>
    [Fact]
    public void Generate_WhenContentsAreEqual_ReturnsNoEntries()
    {
        var diff = new ContentDiffService().Generate("same", "same");

        Assert.Empty(diff.Entries);
    }

    /// <summary>空の比較対象への全削除と全追加を正しく分類することを検証</summary>
    [Fact]
    public void Generate_WhenOneContentIsEmpty_ReturnsOnlyRemovalOrAddition()
    {
        var removed = new ContentDiffService().Generate("content", string.Empty);
        var added = new ContentDiffService().Generate(string.Empty, "content");

        var removedEntry = Assert.Single(removed.Entries);
        Assert.Equal(ChangeKind.Removed, removedEntry.Kind);
        Assert.Equal("content", removedEntry.PreviousText);
        Assert.Null(removedEntry.CurrentText);

        var addedEntry = Assert.Single(added.Entries);
        Assert.Equal(ChangeKind.Added, addedEntry.Kind);
        Assert.Null(addedEntry.PreviousText);
        Assert.Equal("content", addedEntry.CurrentText);
    }
}
