using Argus.Core.Models;

namespace Argus.Core.Services;

/// <summary>LCSを使って比較対象の追加・削除・変更を抽出する実装</summary>
public sealed class ContentDiffService : IContentDiffService
{
    private const long MaximumLcsTableCells = 4_000_000;

    /// <summary>比較対象を改変せず行単位の差分結果へ変換</summary>
    public ContentDiff Generate(string previousContent, string currentContent)
    {
        ArgumentNullException.ThrowIfNull(previousContent);
        ArgumentNullException.ThrowIfNull(currentContent);

        var previousLines = SplitLines(previousContent);
        var currentLines = SplitLines(currentContent);
        var lcsTableCells = (long)(previousLines.Length + 1) * (currentLines.Length + 1);
        if (lcsTableCells > MaximumLcsTableCells)
        {
            throw new ContentDiffException(
                "差分対象が大きすぎます。",
                new InvalidOperationException("差分計算用の表が上限を超えました。"));
        }

        var lcs = BuildLcsTable(previousLines, currentLines);
        var operations = BuildOperations(previousLines, currentLines, lcs);
        var entries = BuildEntries(operations);
        return new ContentDiff(entries);
    }

    /// <summary>改行コード以外の文字列を保持したまま比較単位を分割</summary>
    private static string[] SplitLines(string content) =>
        content.Length == 0
            ? []
            : content.Split('\n', StringSplitOptions.None);

    /// <summary>二つの行列に対する最長共通部分列の長さを計算</summary>
    private static int[,] BuildLcsTable(
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> currentLines)
    {
        var lcs = new int[previousLines.Count + 1, currentLines.Count + 1];
        for (var previousIndex = previousLines.Count - 1; previousIndex >= 0; previousIndex--)
        {
            for (var currentIndex = currentLines.Count - 1; currentIndex >= 0; currentIndex--)
            {
                lcs[previousIndex, currentIndex] =
                    string.Equals(
                        previousLines[previousIndex],
                        currentLines[currentIndex],
                        StringComparison.Ordinal)
                        ? lcs[previousIndex + 1, currentIndex + 1] + 1
                        : Math.Max(
                            lcs[previousIndex + 1, currentIndex],
                            lcs[previousIndex, currentIndex + 1]);
            }
        }

        return lcs;
    }

    /// <summary>LCSの一致行をアンカーにして未一致の追加削除操作を列挙</summary>
    private static List<DiffOperation> BuildOperations(
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> currentLines,
        int[,] lcs)
    {
        var operations = new List<DiffOperation>();
        var previousIndex = 0;
        var currentIndex = 0;

        while (previousIndex < previousLines.Count || currentIndex < currentLines.Count)
        {
            if (previousIndex < previousLines.Count &&
                currentIndex < currentLines.Count &&
                string.Equals(
                    previousLines[previousIndex],
                    currentLines[currentIndex],
                    StringComparison.Ordinal))
            {
                operations.Add(DiffOperation.Match(previousLines[previousIndex]));
                previousIndex++;
                currentIndex++;
                continue;
            }

            if (currentIndex < currentLines.Count &&
                (previousIndex == previousLines.Count ||
                 lcs[previousIndex, currentIndex + 1] > lcs[previousIndex + 1, currentIndex]))
            {
                operations.Add(DiffOperation.Added(currentLines[currentIndex]));
                currentIndex++;
                continue;
            }

            operations.Add(DiffOperation.Removed(previousLines[previousIndex]));
            previousIndex++;
        }

        return operations;
    }

    /// <summary>一致区間ごとの追加削除を対応付けて変更行へ集約</summary>
    private static IReadOnlyList<ContentDiffEntry> BuildEntries(
        IReadOnlyList<DiffOperation> operations)
    {
        var entries = new List<ContentDiffEntry>();
        var unmatched = new List<DiffOperation>();

        foreach (var operation in operations)
        {
            if (operation.Kind is DiffOperationKind.Added or DiffOperationKind.Removed)
            {
                unmatched.Add(operation);
                continue;
            }

            AppendUnmatchedEntries(unmatched, entries);
            unmatched.Clear();
        }

        AppendUnmatchedEntries(unmatched, entries);
        return entries;
    }

    /// <summary>隣接する未一致行を旧値と新値の変更組または単独差分へ変換</summary>
    private static void AppendUnmatchedEntries(
        IReadOnlyList<DiffOperation> unmatched,
        ICollection<ContentDiffEntry> entries)
    {
        var removed = unmatched
            .Where(operation => operation.Kind == DiffOperationKind.Removed)
            .Select(operation => operation.Text)
            .ToArray();
        var added = unmatched
            .Where(operation => operation.Kind == DiffOperationKind.Added)
            .Select(operation => operation.Text)
            .ToArray();
        var changedCount = Math.Min(removed.Length, added.Length);

        for (var index = 0; index < changedCount; index++)
        {
            entries.Add(new ContentDiffEntry(
                ChangeKind.Changed,
                removed[index],
                added[index]));
        }

        for (var index = changedCount; index < removed.Length; index++)
        {
            entries.Add(new ContentDiffEntry(
                ChangeKind.Removed,
                removed[index],
                null));
        }

        for (var index = changedCount; index < added.Length; index++)
        {
            entries.Add(new ContentDiffEntry(
                ChangeKind.Added,
                null,
                added[index]));
        }
    }

    /// <summary>LCS復元中の一致・追加・削除を内部値として表現</summary>
    private sealed record DiffOperation(DiffOperationKind Kind, string Text)
    {
        /// <summary>一致行の内部操作を生成</summary>
        public static DiffOperation Match(string text) =>
            new(DiffOperationKind.Match, text);

        /// <summary>追加行の内部操作を生成</summary>
        public static DiffOperation Added(string text) =>
            new(DiffOperationKind.Added, text);

        /// <summary>削除行の内部操作を生成</summary>
        public static DiffOperation Removed(string text) =>
            new(DiffOperationKind.Removed, text);
    }

    /// <summary>LCS復元で扱う内部操作の種別</summary>
    private enum DiffOperationKind
    {
        Match,
        Added,
        Removed
    }
}
