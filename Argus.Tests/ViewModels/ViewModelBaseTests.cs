using Argus.ViewModels;

namespace Argus.Tests.ViewModels;

/// <summary>Avalonia ViewModel共通基盤の通知契約を検証するテスト</summary>
public sealed class ViewModelBaseTests
{
    /// <summary>値が変化した場合だけプロパティ変更が通知されることを検証</summary>
    [Fact]
    public void SetValue_WhenValueChanges_RaisesPropertyChangedOnce()
    {
        var viewModel = new TestViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, eventArgs) =>
            changedProperties.Add(eventArgs.PropertyName);

        viewModel.Value = "changed";
        viewModel.Value = "changed";

        Assert.Equal([nameof(TestViewModel.Value)], changedProperties);
    }

    /// <summary>テスト対象の保護APIを外部動作として観測できるようにする補助型</summary>
    private sealed class TestViewModel : ViewModelBase
    {
        private string value = string.Empty;

        /// <summary>共通基盤へ値更新を委譲する検証用プロパティ</summary>
        public string Value
        {
            get => value;
            set => SetField(ref this.value, value);
        }
    }
}
