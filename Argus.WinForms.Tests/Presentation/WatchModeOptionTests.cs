using Argus.Core.Models;
using Argus.WinForms.Presentation;

namespace Argus.WinForms.Tests.Presentation;

/// <summary>監視モード選択肢のドメイン値と表示名の対応を検証するテスト</summary>
public sealed class WatchModeOptionTests
{
    /// <summary>すべての対応モードを重複なく表示できることを検証</summary>
    [Fact]
    public void All_ContainsEverySupportedModeOnce()
    {
        Assert.Equal(Enum.GetValues<WatchMode>(), WatchModeOption.All.Select(option => option.Value));
        Assert.Equal(WatchModeOption.All.Count, WatchModeOption.All.Select(option => option.DisplayName).Distinct().Count());
    }
}
