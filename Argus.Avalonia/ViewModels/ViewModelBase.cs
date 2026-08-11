using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Argus.Avalonia.ViewModels;

/// <summary>画面状態の変更をAvaloniaバインディングへ通知する共通基盤</summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>値が変化した場合だけフィールドとバインディング通知を更新</summary>
    protected bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>派生ViewModelの計算プロパティを含む変更通知を発行</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
