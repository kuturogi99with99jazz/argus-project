using System.Text.Json;
using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>監視対象設定のポータブルJSON移行を検証するテスト</summary>
public sealed class SettingsTransferServiceTests
{
    /// <summary>スナップショットや識別子を除いた編集可能な設定だけを出力することを検証</summary>
    [Fact]
    public void Export_WhenTargetHasSnapshot_ExportsOnlyEditableSettings()
    {
        var target = CreateTarget() with
        {
            PreviousSnapshot = new WatchSnapshot(
                new string('a', 64),
                new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero),
                "private comparison content")
        };
        var service = new SettingsTransferService();

        var json = service.Export([target]);
        using var document = JsonDocument.Parse(json);
        var exportedTarget = document.RootElement
            .GetProperty("targets")[0];

        Assert.Equal("argus-settings", document.RootElement.GetProperty("format").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("formatVersion").GetInt32());
        Assert.Equal("Original", exportedTarget.GetProperty("name").GetString());
        Assert.Equal("https://example.com/", exportedTarget.GetProperty("url").GetString());
        Assert.Equal("htmlText", exportedTarget.GetProperty("mode").GetString());
        Assert.False(exportedTarget.TryGetProperty("id", out _));
        Assert.False(exportedTarget.TryGetProperty("previousSnapshot", out _));
        Assert.DoesNotContain("private comparison content", json, StringComparison.Ordinal);
    }

    /// <summary>日本語の設定値をJSON上でも利用者が読める文字として出力することを検証</summary>
    [Fact]
    public void Export_WhenTargetHasJapaneseText_KeepsTextReadable()
    {
        var target = CreateTarget() with
        {
            Name = "悪帝の楽園",
            Memo = "小説家になろう"
        };
        var service = new SettingsTransferService();

        var json = service.Export([target]);

        Assert.Contains("悪帝の楽園", json, StringComparison.Ordinal);
        Assert.Contains("小説家になろう", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u60AA", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u5C0F", json, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>設定を読み込む際に新しい識別子と空の前回データを生成することを検証</summary>
    [Fact]
    public void Import_WhenJsonIsValid_CreatesNewTargetWithoutSnapshot()
    {
        var source = CreateTarget();
        var service = new SettingsTransferService();
        var json = service.Export([source]);

        var imported = service.Import(json);

        var target = Assert.Single(imported);
        Assert.NotEqual(source.Id, target.Id);
        Assert.NotEqual(Guid.Empty, target.Id);
        Assert.Equal(source.Name, target.Name);
        Assert.Equal(source.Url, target.Url);
        Assert.Equal(source.Mode, target.Mode);
        Assert.Equal(source.IsEnabled, target.IsEnabled);
        Assert.Equal(source.Memo, target.Memo);
        Assert.Equal(source.CssSelector, target.CssSelector);
        Assert.Null(target.PreviousSnapshot);
    }

    /// <summary>形式識別子またはバージョンが未対応の場合に読み込みを拒否することを検証</summary>
    [Theory]
    [InlineData("wrong-format", 1)]
    [InlineData("argus-settings", 2)]
    public void Import_WhenFormatIsUnsupported_ThrowsSettingsTransferException(
        string format,
        int formatVersion)
    {
        var service = new SettingsTransferService();
        var json = $$"""
            {
              "format": "{{format}}",
              "formatVersion": {{formatVersion}},
              "targets": []
            }
            """;

        var exception = Assert.Throws<SettingsTransferException>(
            () => service.Import(json));

        Assert.Contains("形式", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>不正JSONを既存設定へ影響させず拒否することを検証</summary>
    [Fact]
    public void Import_WhenJsonIsMalformed_ThrowsSettingsTransferException()
    {
        var service = new SettingsTransferService();

        var exception = Assert.Throws<SettingsTransferException>(
            () => service.Import("{ invalid"));

        Assert.Contains("JSON", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>入力規則違反を含む文書全体を反映せず拒否することを検証</summary>
    [Fact]
    public void Import_WhenOneTargetIsInvalid_RejectsTheWholeDocument()
    {
        var service = new SettingsTransferService();
        var json = """
            {
              "format": "argus-settings",
              "formatVersion": 1,
              "targets": [
                {
                  "name": "Valid",
                  "url": "https://example.com/",
                  "mode": "htmlText",
                  "isEnabled": true,
                  "memo": null,
                  "cssSelector": null
                },
                {
                  "name": " ",
                  "url": "ftp://example.com/",
                  "mode": "cssSelector",
                  "isEnabled": true,
                  "memo": null,
                  "cssSelector": " "
                }
              ]
            }
            """;

        var exception = Assert.Throws<SettingsTransferException>(
            () => service.Import(json));

        Assert.Contains("正しくありません", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>設定移行に使う監視対象の共通入力を生成</summary>
    private static WatchTarget CreateTarget() =>
        new(
            Guid.NewGuid(),
            "Original",
            new Uri("https://example.com/"),
            WatchMode.HtmlText,
            true,
            "memo",
            null,
            null);
}
