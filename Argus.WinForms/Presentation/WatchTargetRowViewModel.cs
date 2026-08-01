using System.ComponentModel;
using System.Runtime.CompilerServices;
using Argus.Core.Models;

namespace Argus.WinForms.Presentation;

/// <summary>監視対象一覧の1行と実行状態を画面向けに保持するモデル</summary>
public sealed class WatchTargetRowViewModel : INotifyPropertyChanged
{
    private CheckStatus status = CheckStatus.Unchecked;
    private DateTimeOffset? lastCheckedAtUtc;
    private string? errorMessage;
    private int runningCount;

    /// <summary>ドメインモデルを画面表示用の初期状態へ変換</summary>
    public WatchTargetRowViewModel(WatchTarget target)
    {
        UpdateTarget(target);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>監視対象の識別子</summary>
    public Guid TargetId { get; private set; }
    /// <summary>EnabledTextを一覧のデータバインディングへ通知するための表示状態</summary>
    public string EnabledText => IsEnabled ? "はい" : "いいえ";

    /// <summary>監視対象が有効かどうか</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>監視対象名</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>監視対象URLの表示文字列</summary>
    public string Url { get; private set; } = string.Empty;
    /// <summary>Statusを一覧のデータバインディングへ通知するための表示状態</summary>
    public CheckStatus Status
    {
        get => status;
        private set => SetField(ref status, value);
    }

    /// <summary>StatusTextを一覧のデータバインディングへ通知するための表示状態</summary>
    public string StatusText
    {
        get
        {
            var baseStatus = CheckStatusAppearance.Get(Status).DisplayName;
            return RunningCount > 0
                ? $"{baseStatus} / チェック中 ×{RunningCount}"
                : baseStatus;
        }
    }

    /// <summary>LastCheckedTextを一覧のデータバインディングへ通知するための表示状態</summary>
    public string LastCheckedText =>
        LastCheckedAtUtc?.ToLocalTime().ToString("yyyy/MM/dd HH:mm") ?? "—";
    /// <summary>LastCheckedAtUtcを一覧のデータバインディングへ通知するための表示状態</summary>
    public DateTimeOffset? LastCheckedAtUtc
    {
        get => lastCheckedAtUtc;
        private set => SetField(ref lastCheckedAtUtc, value);
    }

    /// <summary>ErrorTextを一覧のデータバインディングへ通知するための表示状態</summary>
    public string ErrorText => ErrorMessage ?? "—";
    /// <summary>ErrorMessageを一覧のデータバインディングへ通知するための表示状態</summary>
    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetField(ref errorMessage, value);
    }

    /// <summary>RunningCountを一覧のデータバインディングへ通知するための表示状態</summary>
    public int RunningCount
    {
        get => runningCount;
        private set
        {
            if (SetField(ref runningCount, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }


    /// <summary>監視対象の編集内容を一覧行へ反映</summary>
    public void UpdateTarget(WatchTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        TargetId = target.Id;
        IsEnabled = target.IsEnabled;
        Name = target.Name;
        Url = target.Url.AbsoluteUri;
        OnPropertyChanged(nameof(TargetId));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(EnabledText));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Url));
    }


    /// <summary>確認結果を一覧行の状態へ反映</summary>
    public void ApplyCheckResult(CheckResult result)
    {
        if (result.TargetId != TargetId)
        {
            return;
        }

        Status = result.Status;
        LastCheckedAtUtc = result.CompletedAtUtc;
        ErrorMessage = result.ErrorMessage;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(LastCheckedText));
        OnPropertyChanged(nameof(ErrorText));
    }


    /// <summary>実行中確認数を更新し、確認中表示を制御</summary>
    public void SetRunningCount(int count)
    {
        RunningCount = Math.Max(0, count);
    }

    private bool SetField<T>(
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

    /// <summary>変更された表示プロパティだけをデータバインディングへ通知</summary>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
