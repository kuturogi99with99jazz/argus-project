using Argus.Core.Models;
using Argus.Core.Services;

namespace Argus.ViewModels;

/// <summary>監視対象の追加と編集で共有する入力、検証、保存状態を管理するViewModel</summary>
public sealed class TargetEditViewModel : ViewModelBase
{
    private readonly Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync;
    private readonly Func<string, CancellationToken, Task>? showCssSelectorPromptAsync;
    private string name;
    private string url;
    private WatchMode selectedMode;
    private bool isEnabled;
    private string memo;
    private string cssSelector;
    private string? nameError;
    private string? urlError;
    private string? cssSelectorError;
    private string? operationError;
    private WatchTarget? savedTarget;

    /// <summary>追加と編集で共有する初期値と保存境界を構成</summary>
    public TargetEditViewModel(
        WatchTarget? target,
        Func<WatchTargetInput, CancellationToken, Task<WatchTargetChangeResult>> saveAsync,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, Task>? showCssSelectorPromptAsync = null)
    {
        this.saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
        this.showCssSelectorPromptAsync = showCssSelectorPromptAsync;
        name = target?.Name ?? string.Empty;
        url = target?.Url.AbsoluteUri ?? string.Empty;
        selectedMode = target?.Mode ?? WatchMode.HtmlText;
        isEnabled = target?.IsEnabled ?? true;
        memo = target?.Memo ?? string.Empty;
        cssSelector = target?.CssSelector ?? string.Empty;
        Title = target is null ? "監視対象を追加" : "監視対象を編集";
        SaveCommand = new AsyncCommand(
            (_, token) => SaveAsync(token),
            onException: exception => OperationError = exception.Message,
            cancellationToken: cancellationToken);
        CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke(this, EventArgs.Empty));
        ShowCssSelectorPromptCommand = new AsyncCommand(
            (_, token) => ShowCssSelectorPromptAsync(token),
            _ => IsCssSelectorVisible && this.showCssSelectorPromptAsync is not null,
            onException: exception => OperationError = exception.Message,
            cancellationToken: cancellationToken);
    }

    public event EventHandler? Saved;
    public event EventHandler? CancelRequested;

    /// <summary>追加と編集を区別する画面タイトル</summary>
    public string Title { get; }

    /// <summary>監視対象名の未検証入力値</summary>
    public string Name { get => name; set => SetField(ref name, value); }

    /// <summary>監視対象URLの未検証入力値</summary>
    public string Url { get => url; set => SetField(ref url, value); }

    /// <summary>選択中のCore監視モード</summary>
    public WatchMode SelectedMode
    {
        get => selectedMode;
        set
        {
            if (SetField(ref selectedMode, value))
            {
                OnPropertyChanged(nameof(IsCssSelectorVisible));
                OnPropertyChanged(nameof(SelectedModeOption));
                ShowCssSelectorPromptCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>ComboBoxの選択項目とCore監視モードを相互変換する表示用選択肢</summary>
    public WatchModeOption SelectedModeOption
    {
        get => WatchModeOption.All.First(option => option.Value == SelectedMode);
        set => SelectedMode = value.Value;
    }

    /// <summary>監視対象をチェック対象として有効にするかどうか</summary>
    public bool IsEnabled { get => isEnabled; set => SetField(ref isEnabled, value); }

    /// <summary>利用者向けメモの未検証入力値</summary>
    public string Memo { get => memo; set => SetField(ref memo, value); }

    /// <summary>CSSセレクタの未検証入力値</summary>
    public string CssSelector { get => cssSelector; set => SetField(ref cssSelector, value); }

    /// <summary>CSSセレクタ比較のときだけ固有入力欄を表示する状態</summary>
    public bool IsCssSelectorVisible => SelectedMode == WatchMode.CssSelector;

    /// <summary>CSSセレクタ設定をAIへ相談する小窓を表示する非同期コマンド</summary>
    public AsyncCommand ShowCssSelectorPromptCommand { get; }

    /// <summary>選択可能な監視モード一覧</summary>
    public IReadOnlyList<WatchModeOption> ModeOptions => WatchModeOption.All;

    /// <summary>監視対象名に対応するCore入力検証メッセージ</summary>
    public string? NameError { get => nameError; private set => SetField(ref nameError, value); }

    /// <summary>URLに対応するCore入力検証メッセージ</summary>
    public string? UrlError { get => urlError; private set => SetField(ref urlError, value); }

    /// <summary>CSSセレクタに対応するCore入力検証メッセージ</summary>
    public string? CssSelectorError { get => cssSelectorError; private set => SetField(ref cssSelectorError, value); }

    /// <summary>項目へ対応しない保存エラーの利用者向けメッセージ</summary>
    public string? OperationError { get => operationError; private set => SetField(ref operationError, value); }

    /// <summary>Coreへの保存が成功した監視対象</summary>
    public WatchTarget? SavedTarget { get => savedTarget; private set => SetField(ref savedTarget, value); }

    /// <summary>入力値をCoreへ保存する非同期コマンド</summary>
    public AsyncCommand SaveCommand { get; }

    /// <summary>保存せず編集画面を閉じる同期コマンド</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>Coreの入力検証結果を項目別表示へ反映し成功時だけ完了を通知</summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearErrors();
        var result = await saveAsync(
            new WatchTargetInput(Name, Url, SelectedMode, IsEnabled, Memo, CssSelector),
            cancellationToken);

        if (result.IsSuccess && result.Target is not null)
        {
            SavedTarget = result.Target;
            Saved?.Invoke(this, EventArgs.Empty);
            return;
        }

        foreach (var error in result.ValidationErrors)
        {
            if (error.Field == nameof(WatchTargetInput.Name))
            {
                NameError = error.Message;
            }
            else if (error.Field == nameof(WatchTargetInput.Url))
            {
                UrlError = error.Message;
            }
            else if (error.Field == nameof(WatchTargetInput.CssSelector))
            {
                CssSelectorError = error.Message;
            }
            else
            {
                OperationError = error.Message;
            }
        }

        OperationError ??= result.ErrorMessage;
    }

    /// <summary>現在入力中のURLをUIサービス境界へ渡してAI相談小窓を表示</summary>
    private Task ShowCssSelectorPromptAsync(CancellationToken cancellationToken) =>
        showCssSelectorPromptAsync is null
            ? Task.CompletedTask
            : showCssSelectorPromptAsync(Url, cancellationToken);

    /// <summary>再保存時に古い検証メッセージが残らないよう入力エラーを初期化</summary>
    private void ClearErrors()
    {
        NameError = null;
        UrlError = null;
        CssSelectorError = null;
        OperationError = null;
    }
}
