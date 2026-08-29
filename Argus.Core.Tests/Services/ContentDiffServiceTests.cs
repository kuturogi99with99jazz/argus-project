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

    /// <summary>変更行の文字列内で変更部分だけを区間化することを検証。</summary>
    [Fact]
    public void Generate_WhenChangedLineContainsReplacement_HighlightsOnlyChangedText()
    {
        var diff = new ContentDiffService().Generate(
            "prefix-old-suffix",
            "prefix-new-suffix");

        var entry = Assert.Single(diff.Entries);
        Assert.Collection(
            entry.PreviousSegments!,
            unchanged =>
            {
                Assert.Equal("prefix-", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            },
            changed =>
            {
                Assert.Equal("old", changed.Text);
                Assert.True(changed.IsChanged);
            },
            unchanged =>
            {
                Assert.Equal("-suffix", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            });
        Assert.Collection(
            entry.CurrentSegments!,
            unchanged =>
            {
                Assert.Equal("prefix-", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            },
            changed =>
            {
                Assert.Equal("new", changed.Text);
                Assert.True(changed.IsChanged);
            },
            unchanged =>
            {
                Assert.Equal("-suffix", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            });
        Assert.Equal(
            entry.PreviousText,
            string.Concat(entry.PreviousSegments!.Select(segment => segment.Text)));
        Assert.Equal(
            entry.CurrentText,
            string.Concat(entry.CurrentSegments!.Select(segment => segment.Text)));
    }

    /// <summary>文字列中央への追加を変更部分として表現することを検証</summary>
    [Fact]
    public void Generate_WhenChangedLineContainsInsertion_HighlightsInsertedText()
    {
        var diff = new ContentDiffService().Generate("abc", "aXbc");

        var entry = Assert.Single(diff.Entries);
        Assert.Collection(
            entry.PreviousSegments!,
            unchanged =>
            {
                Assert.Equal("abc", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            });
        Assert.Collection(
            entry.CurrentSegments!,
            unchanged =>
            {
                Assert.Equal("a", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            },
            changed =>
            {
                Assert.Equal("X", changed.Text);
                Assert.True(changed.IsChanged);
            },
            unchanged =>
            {
                Assert.Equal("bc", unchanged.Text);
                Assert.False(unchanged.IsChanged);
            });
    }

    /// <summary>サロゲートペアを分割せずUnicode文字要素単位で変更を表現することを検証。</summary>
    [Fact]
    public void Generate_WhenGraphemeChanges_DoesNotSplitUnicodeTextElement()
    {
        var diff = new ContentDiffService().Generate("A😀B", "A😃B");

        var entry = Assert.Single(diff.Entries);
        Assert.Equal(
            [
                new ContentDiffSegment("A", false),
                new ContentDiffSegment("😀", true),
                new ContentDiffSegment("B", false)
            ],
            entry.PreviousSegments);
        Assert.Equal(
            [
                new ContentDiffSegment("A", false),
                new ContentDiffSegment("😃", true),
                new ContentDiffSegment("B", false)
            ],
            entry.CurrentSegments);
    }

    /// <summary>結合文字を分割せず一つのUnicode文字要素として扱うことを検証。</summary>
    [Fact]
    public void Generate_WhenCombiningMarkChanges_DoesNotSplitGrapheme()
    {
        var diff = new ContentDiffService().Generate("Cafe\u0301", "Cafe\u0302");

        var entry = Assert.Single(diff.Entries);

        Assert.Equal(
            [
                new ContentDiffSegment("Caf", false),
                new ContentDiffSegment("e\u0301", true)
            ],
            entry.PreviousSegments);
        Assert.Equal(
            [
                new ContentDiffSegment("Caf", false),
                new ContentDiffSegment("e\u0302", true)
            ],
            entry.CurrentSegments);
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
        Assert.Equal([new ContentDiffSegment("content", true)], removedEntry.PreviousSegments);

        var addedEntry = Assert.Single(added.Entries);
        Assert.Equal(ChangeKind.Added, addedEntry.Kind);
        Assert.Null(addedEntry.PreviousText);
        Assert.Equal("content", addedEntry.CurrentText);
        Assert.Equal([new ContentDiffSegment("content", true)], addedEntry.CurrentSegments);
    }

    /// <summary>差分表が過大になる入力を明示的な差分生成エラーとして拒否することを検証</summary>
    [Fact]
    public void Generate_WhenLcsTableWouldBeTooLarge_ThrowsContentDiffException()
    {
        var previous = string.Join('\n', Enumerable.Repeat("previous", 2_000));
        var current = string.Join('\n', Enumerable.Repeat("current", 2_000));

        Assert.Throws<ContentDiffException>(
            () => new ContentDiffService().Generate(previous, current));
    }

    /// <summary>一行のインラインLCS表が過大な場合も差分生成エラーにすることを検証。</summary>
    [Fact]
    public void Generate_WhenInlineLcsTableWouldBeTooLarge_ThrowsContentDiffException()
    {
        var previous = new string('a', 2_000);
        var current = new string('b', 2_000);

        Assert.Throws<ContentDiffException>(
            () => new ContentDiffService().Generate(previous, current));
    }
}
