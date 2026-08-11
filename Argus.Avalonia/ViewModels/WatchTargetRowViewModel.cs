using Argus.Core.Models;

namespace Argus.Avalonia.ViewModels;

/// <summary>Coreの監視対象と直近チェック状態を一覧表示用に保持するViewModel</summary>
public sealed class WatchTargetRowViewModel : ViewModelBase
{
    private CheckStatus status = CheckStatus.Unchecked;
    private DateTimeOffset? lastCheckedAtUtc;
    private string? errorMessage;
    private int runningCount;

    /// <summary>Coreモデルを画面表示用の初期状態へ変換</summary>
    public WatchTargetRowViewModel(WatchTarget target) => UpdateTarget(target);

    /// <summary>Core上の監視対象識別子</summary>
    public Guid TargetId { get; private set; }

    /// <summary>監視対象名</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>監視対象URLの表示文字列</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>監視方式の利用者向け表示名</summary>
    public string ModeText { get; private set; } = string.Empty;

    /// <summary>監視対象が有効かどうか</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>有効状態を色に依存せず示す表示文字列</summary>
    public string EnabledText => IsEnabled ? "はい" : "いいえ";

    /// <summary>直近の確認結果</summary>
    public CheckStatus Status
    {
        get => status;
        private set => SetField(ref status, value);
    }

    /// <summary>確認結果と実行中件数をまとめた表示文字列</summary>
    public string StatusText
    {
        get
        {
            var baseStatus = GetStatusText(Status);
            return RunningCount > 0
                ? $"{baseStatus} / チェック中 ×{RunningCount}"
                : baseStatus;
        }
    }

    /// <summary>直近の確認日時を利用者のローカル時刻で示す表示文字列</summary>
    public string LastCheckedText =>
        LastCheckedAtUtc?.ToLocalTime().ToString("yyyy/MM/dd HH:mm") ?? "—";

    /// <summary>直近の確認日時</summary>
    public DateTimeOffset? LastCheckedAtUtc
    {
        get => lastCheckedAtUtc;
        private set => SetField(ref lastCheckedAtUtc, value);
    }

    /// <summary>直近のエラーを空欄と区別して示す表示文字列</summary>
    public string ErrorText => ErrorMessage ?? "—";

    /// <summary>直近のチェックエラー</summary>
    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetField(ref errorMessage, value);
    }

    /// <summary>同じ対象で待機または実行中のチェック件数</summary>
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

    /// <summary>編集後のCoreモデルを識別子を維持した一覧行へ反映</summary>
    public void UpdateTarget(WatchTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        TargetId = target.Id;
        Name = target.Name;
        Url = target.Url.AbsoluteUri;
        ModeText = WatchModeOption.GetDisplayName(target.Mode);
        IsEnabled = target.IsEnabled;
        OnPropertyChanged(nameof(TargetId));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Url));
        OnPropertyChanged(nameof(ModeText));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(EnabledText));
    }

    /// <summary>Coreのチェック完了結果を対象識別子が一致する一覧行へ反映</summary>
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

    /// <summary>Coreの実行状態を負数にならない一覧状態へ反映</summary>
    public void SetRunningCount(int count) => RunningCount = Math.Max(0, count);

    /// <summary>Coreの状態値を色に依存しない日本語表示へ変換</summary>
    private static string GetStatusText(CheckStatus value) =>
        value switch
        {
            CheckStatus.Unchecked => "未確認",
            CheckStatus.FirstFetch => "初回取得",
            CheckStatus.Unchanged => "更新なし",
            CheckStatus.Updated => "更新あり",
            CheckStatus.Error => "エラー",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
