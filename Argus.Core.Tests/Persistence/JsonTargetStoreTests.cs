using System.Text;
using Argus.Core.Models;
using Argus.Core.Persistence;

namespace Argus.Core.Tests.Persistence;

/// <summary>JSON形式の監視対象ストアを検証するテスト</summary>
public sealed class JsonTargetStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"ArgusTests-{Guid.NewGuid():N}");
    private readonly string filePath;

    /// <summary>各テストで共通する一時保存領域を分離して初期化</summary>
    public JsonTargetStoreTests()
    {
        filePath = Path.Combine(directory, "targets.json");
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_CreatesEmptyDocument()
    {
        var store = new JsonTargetStore(filePath);

        var document = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(TargetStoreDocument.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Empty(document.Targets);
        Assert.True(File.Exists(filePath));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsMultipleTargetsWithoutUtf8Bom()
    {
        var firstId = Guid.NewGuid();
        var checkedAt = new DateTimeOffset(2026, 7, 31, 3, 4, 5, TimeSpan.Zero);
        var document = new TargetStoreDocument(
            TargetStoreDocument.CurrentSchemaVersion,
            [
                new WatchTarget(
                    firstId,
                    "サンプル",
                    new Uri("https://example.com/"),
                    WatchMode.HtmlText,
                    true,
                    "メモ",
                    new WatchSnapshot(new string('a', 64), checkedAt)),
                new WatchTarget(
                    Guid.NewGuid(),
                    "Second",
                    new Uri("http://example.net/"),
                    WatchMode.HtmlText,
                    false,
                    null,
                    null)
            ]);
        var store = new JsonTargetStore(filePath);

        await store.SaveAsync(document, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);
        var bytes = await File.ReadAllBytesAsync(filePath);

        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        Assert.Equal(2, loaded.Targets.Count);
        Assert.Equal(firstId, loaded.Targets[0].Id);
        Assert.Equal(checkedAt, loaded.Targets[0].PreviousSnapshot?.CheckedAtUtc);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData("")]
    [InlineData("{ invalid")]
    [InlineData("""{"schemaVersion":99,"targets":[]}""")]
    public async Task LoadAsync_WhenContentIsInvalid_ThrowsAndDoesNotOverwriteFile(string content)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(filePath, content);
        var store = new JsonTargetStore(filePath);

        await Assert.ThrowsAsync<TargetStoreException>(
            () => store.LoadAsync(CancellationToken.None));

        Assert.Equal(content, await File.ReadAllTextAsync(filePath));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task LoadAsync_WhenTargetIdsAreDuplicated_Throws()
    {
        var id = Guid.NewGuid();
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            filePath,
            $$"""
              {
                "schemaVersion": 1,
                "targets": [
                  { "id": "{{id}}", "name": "A", "url": "https://example.com/a", "mode": "htmlText", "isEnabled": true },
                  { "id": "{{id}}", "name": "B", "url": "https://example.com/b", "mode": "htmlText", "isEnabled": true }
                ]
              }
              """);
        var store = new JsonTargetStore(filePath);

        await Assert.ThrowsAsync<TargetStoreException>(
            () => store.LoadAsync(CancellationToken.None));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task LoadAsync_IgnoresUnknownProperties()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            filePath,
            """
            {
              "schemaVersion": 1,
              "futureProperty": true,
              "targets": []
            }
            """);
        var store = new JsonTargetStore(filePath);

        var document = await store.LoadAsync(CancellationToken.None);

        Assert.Empty(document.Targets);
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Theory]
    [InlineData("", "https://example.com/")]
    [InlineData("Sample", "ftp://example.com/")]
    public async Task LoadAsync_WhenTargetFieldsAreInvalid_ThrowsWithoutChangingFile(
        string name,
        string url)
    {
        Directory.CreateDirectory(directory);
        var id = Guid.NewGuid();
        var content =
            $$"""
              {
                "schemaVersion": 1,
                "targets": [
                  {
                    "id": "{{id}}",
                    "name": "{{name}}",
                    "url": "{{url}}",
                    "mode": "htmlText",
                    "isEnabled": true
                  }
                ]
              }
              """;
        await File.WriteAllTextAsync(filePath, content);
        var store = new JsonTargetStore(filePath);

        await Assert.ThrowsAsync<TargetStoreException>(
            () => store.LoadAsync(CancellationToken.None));

        Assert.Equal(content, await File.ReadAllTextAsync(filePath));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task SaveAsync_WhenFileExists_ReplacesItAndLeavesNoTemporaryFile()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(filePath, "old content");
        var store = new JsonTargetStore(filePath);

        await store.SaveAsync(TargetStoreDocument.Empty, CancellationToken.None);

        var loaded = await store.LoadAsync(CancellationToken.None);
        Assert.Empty(loaded.Targets);
        Assert.DoesNotContain(
            Directory.EnumerateFiles(directory),
            path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public async Task LoadAsync_RemovesOnlyMatchingLeftoverTemporaryFiles()
    {
        var store = new JsonTargetStore(filePath);
        await store.SaveAsync(TargetStoreDocument.Empty, CancellationToken.None);
        var matchingTemporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        var unrelatedTemporaryPath = Path.Combine(directory, "other.tmp");
        await File.WriteAllTextAsync(matchingTemporaryPath, "partial");
        await File.WriteAllTextAsync(unrelatedTemporaryPath, "keep");

        await store.LoadAsync(CancellationToken.None);

        Assert.False(File.Exists(matchingTemporaryPath));
        Assert.True(File.Exists(unrelatedTemporaryPath));
        Assert.True(File.Exists(filePath));
    }

    /// <summary>テストで使用した一時リソースを確実に解放するための後処理</summary>
    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
