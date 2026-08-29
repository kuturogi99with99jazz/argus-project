using System.Collections.ObjectModel;
using Avalonia.Styling;
using Application = Avalonia.Application;
using Argus.Services;
using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;

namespace Argus.ViewModels;

/// <summary>監視対象一覧、選択、Core操作、画面集計を管理するメインViewModel</summary>
public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly WatchTargetRepository repository;
    private readonly WatchTargetManagementService managementService;
    private readonly CheckCoordinator coordinator;
    private readonly SettingsTransferService settingsTransferService;
    private readonly IBrowserService browserService;
    private readonly IManualService manualService;
    private readonly IDialogService dialogService;
    private readonly ISettingsFileService settingsFileService;
    private readonly IUiDispatcher dispatcher;
    private readonly CancellationTokenSource applicationCancellation;
    private readonly bool isOperational;
    private readonly List<WatchTargetRowViewModel> selectedRows = [];
    private string statusMessage;
    private bool disposed;
    private bool isDarkThemeEnabled;
    private bool isSettingsTransferRunning;

    /// <summary>CoreとUI固有サービスを手動構築で受け取り一覧状態を初期化</summary>
    public MainWindowViewModel(
        WatchTargetRepository repository,
        WatchTargetManagementService managementService,
        CheckCoordinator coordinator,
        SettingsTransferService settingsTransferService,
        IBrowserService browserService,
        IManualService manualService,
        IDialogService dialogService,
        ISettingsFileService settingsFileService,
        IUiDispatcher dispatcher,
        ApplicationInfo applicationInfo,
        CancellationTokenSource applicationCancellation,
        string? startupError = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        this.settingsTransferService = settingsTransferService
            ?? throw new ArgumentNullException(nameof(settingsTransferService));
        this.browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        this.manualService = manualService ?? throw new ArgumentNullException(nameof(manualService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.settingsFileService = settingsFileService
            ?? throw new ArgumentNullException(nameof(settingsFileService));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.applicationCancellation = applicationCancellation ??
            throw new ArgumentNullException(nameof(applicationCancellation));
        ArgumentNullException.ThrowIfNull(applicationInfo);

        isOperational = startupError is null;
        statusMessage = startupError ?? "準備完了";
        Copyright = applicationInfo.Copyright;
        Version = applicationInfo.Version;
        IsDebug = applicationInfo.IsDebug;
        IsDarkThemeEnabled = false;

        CheckAllCommand = new AsyncCommand(
            (_, _) => ObserveChecksAsync(coordinator.StartAll(applicationCancellation.Token)),
            _ => CanUseOperationalCommands() && Rows.Any(row => row.IsEnabled),
            ReportUnexpectedError,
            applicationCancellation.Token);
        CheckSelectedCommand = new AsyncCommand(
            (_, _) => ObserveChecksAsync(coordinator.StartSelected(
                selectedRows.Where(row => row.IsEnabled).Select(row => row.TargetId),
                applicationCancellation.Token)),
            _ => CanUseOperationalCommands() && selectedRows.Any(row => row.IsEnabled),
            ReportUnexpectedError,
            applicationCancellation.Token);
        OpenBrowserCommand = new RelayCommand(_ => OpenBrowser(),
            _ => CanUseOperationalCommands() && selectedRows.Count == 1);
        ShowDiffCommand = new AsyncCommand(
            (_, _) => ShowDiffAsync(),
            _ => CanUseOperationalCommands() && GetSingleSelected() is
            {
                Status: CheckStatus.Updated,
                RunningCount: 0
            },
            ReportUnexpectedError,
            applicationCancellation.Token);
        OpenManualCommand = new RelayCommand(_ => OpenManual());
        AddCommand = new AsyncCommand(
            (_, _) => AddAsync(),
            _ => CanUseOperationalCommands(),
            ReportUnexpectedError,
            applicationCancellation.Token);
        EditCommand = new AsyncCommand(
            (_, _) => EditAsync(),
            _ => CanUseOperationalCommands() && GetSingleSelected() is { RunningCount: 0 },
            ReportUnexpectedError,
            applicationCancellation.Token);
        DeleteCommand = new AsyncCommand(
            (_, _) => DeleteAsync(),
            _ => CanUseOperationalCommands() && GetSingleSelected() is { RunningCount: 0 },
            ReportUnexpectedError,
            applicationCancellation.Token);
        ImportSettingsCommand = new AsyncCommand(
            (_, _) => ImportSettingsAsync(),
            _ => CanUseOperationalCommands() && !HasRunningChecks(),
            ReportUnexpectedError,
            applicationCancellation.Token);
        ExportSettingsCommand = new AsyncCommand(
            (_, _) => ExportSettingsAsync(),
            _ => CanUseOperationalCommands(),
            ReportUnexpectedError,
            applicationCancellation.Token);

        coordinator.ExecutionChanged += Coordinator_ExecutionChanged;
        coordinator.CheckCompleted += Coordinator_CheckCompleted;
        ReloadRows();
    }

    /// <summary>一覧表示用の監視対象行</summary>
    public ObservableCollection<WatchTargetRowViewModel> Rows { get; } = [];

    /// <summary>有効な全監視対象を確認するコマンド</summary>
    public AsyncCommand CheckAllCommand { get; }

    /// <summary>選択中の有効な監視対象を確認するコマンド</summary>
    public AsyncCommand CheckSelectedCommand { get; }

    /// <summary>単一選択した監視対象を既定ブラウザで開くコマンド</summary>
    public RelayCommand OpenBrowserCommand { get; }

    /// <summary>単一選択した更新あり監視対象の差分を表示するコマンド</summary>
    public AsyncCommand ShowDiffCommand { get; }

    /// <summary>選択状態やデータ読込結果に依存せず正式ユーザーマニュアルを開くコマンド</summary>
    public RelayCommand OpenManualCommand { get; }

    /// <summary>監視対象を追加する編集画面を開くコマンド</summary>
    public AsyncCommand AddCommand { get; }

    /// <summary>単一選択した監視対象を編集するコマンド</summary>
    public AsyncCommand EditCommand { get; }

    /// <summary>単一選択した監視対象を確認後に削除するコマンド</summary>
    public AsyncCommand DeleteCommand { get; }

    /// <summary>ポータブルJSONから監視対象設定を読み込むコマンド</summary>
    public AsyncCommand ImportSettingsCommand { get; }

    /// <summary>監視対象設定をポータブルJSONへ書き出すコマンド</summary>
    public AsyncCommand ExportSettingsCommand { get; }

    /// <summary>監視対象総数を示す集計文字列</summary>
    public string TargetSummary => $"監視対象 {Rows.Count}件";

    /// <summary>実行中の監視対象数を示す集計文字列</summary>
    public string CheckingSummary => $"チェック中 {Rows.Count(row => row.RunningCount > 0)}件";

    /// <summary>現在の複数選択件数を示す集計文字列</summary>
    public string SelectionSummary => $"選択 {selectedRows.Count}件";

    /// <summary>直近操作または起動エラーを示す利用者向けメッセージ</summary>
    public string StatusMessage
    {
        get => statusMessage;
        private set => SetField(ref statusMessage, value);
    }

    /// <summary>アセンブリに設定されたコピーライト</summary>
    public string? Copyright { get; }

    /// <summary>ビルドメタデータを除いたアプリバージョン</summary>
    public string Version { get; }

    /// <summary>Debugビルドであることを画面へ明示する状態</summary>
    public bool IsDebug { get; }

    /// <summary>画面全体の配色をダークに切り替えるかどうか</summary>
    public bool IsDarkThemeEnabled
    {
        get => isDarkThemeEnabled;
        set
        {
            if (!SetField(ref isDarkThemeEnabled, value))
            {
                return;
            }

            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
    }

    /// <summary>ListBoxの複数選択をViewModelの操作可否と集計へ反映</summary>
    public void SetSelection(IEnumerable<WatchTargetRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        selectedRows.Clear();
        selectedRows.AddRange(rows.Distinct());
        OnPropertyChanged(nameof(SelectionSummary));
        RaiseCommandStates();
    }

    /// <summary>終了時にCoreイベントを解除し実行中処理へキャンセルを通知</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        coordinator.ExecutionChanged -= Coordinator_ExecutionChanged;
        coordinator.CheckCompleted -= Coordinator_CheckCompleted;
        applicationCancellation.Cancel();
        coordinator.Dispose();
    }

    /// <summary>Coreの実行数変更をUIスレッド上の該当行と集計へ反映</summary>
    private void Coordinator_ExecutionChanged(object? sender, CheckExecutionChangedEventArgs eventArgs) =>
        dispatcher.Dispatch(() =>
        {
            if (disposed)
            {
                return;
            }

            Rows.FirstOrDefault(row => row.TargetId == eventArgs.TargetId)
                ?.SetRunningCount(eventArgs.RunningCount);
            OnPropertyChanged(nameof(CheckingSummary));
            RaiseCommandStates();
        });

    /// <summary>Coreの完了結果をUIスレッド上の一覧状態とメッセージへ反映</summary>
    private void Coordinator_CheckCompleted(object? sender, CheckCompletedEventArgs eventArgs) =>
        dispatcher.Dispatch(() =>
        {
            if (disposed)
            {
                return;
            }

            var row = Rows.FirstOrDefault(item => item.TargetId == eventArgs.Result.TargetId);
            row?.ApplyCheckResult(eventArgs.Result);
            var targetName = row?.Name ?? "監視対象";
            StatusMessage = eventArgs.Result.Status == CheckStatus.Error
                ? $"{targetName}: {eventArgs.Result.ErrorMessage}"
                : $"{targetName} のチェックが完了しました。";
            RaiseCommandStates();
        });

    /// <summary>Coreリポジトリから一覧を再構築し選択と集計を整合</summary>
    private void ReloadRows(Guid? selectedTargetId = null)
    {
        Rows.Clear();
        foreach (var target in repository.GetAll())
        {
            var row = new WatchTargetRowViewModel(target);
            row.SetRunningCount(coordinator.GetRunningCount(target.Id));
            Rows.Add(row);
        }

        selectedRows.Clear();
        if (selectedTargetId.HasValue)
        {
            var selected = Rows.FirstOrDefault(row => row.TargetId == selectedTargetId.Value);
            if (selected is not null)
            {
                selectedRows.Add(selected);
            }
        }

        OnPropertyChanged(nameof(TargetSummary));
        OnPropertyChanged(nameof(CheckingSummary));
        OnPropertyChanged(nameof(SelectionSummary));
        RaiseCommandStates();
    }

    /// <summary>複数のCoreチェックを待機し開始件数と空選択を利用者へ通知</summary>
    private async Task ObserveChecksAsync(IReadOnlyList<Task<CheckResult>> tasks)
    {
        if (tasks.Count == 0)
        {
            await ShowErrorAsync("チェックできる有効な監視対象が選択されていません。");
            return;
        }

        StatusMessage = $"{tasks.Count}件のチェックを開始しました。";
        await Task.WhenAll(tasks);
    }

    /// <summary>単一選択した対象を検証済みブラウザサービスへ渡す操作</summary>
    private void OpenBrowser()
    {
        var row = GetSingleSelected();
        var target = row is null ? null : repository.Find(row.TargetId);
        if (target is null)
        {
            _ = ShowErrorAsync("選択した監視対象が見つかりません。");
            return;
        }

        var result = browserService.Open(target);
        if (result.IsSuccess)
        {
            StatusMessage = $"{target.Name} を既定ブラウザで開きました。";
        }
        else
        {
            _ = ShowErrorAsync(result.ErrorMessage ?? "ブラウザを開けませんでした。");
        }
    }

    /// <summary>更新あり結果の差分を検証してダイアログサービスへ委譲</summary>
    private async Task ShowDiffAsync()
    {
        var row = GetSingleSelected();
        if (row is null ||
            row.Status != CheckStatus.Updated ||
            row.RunningCount > 0)
        {
            return;
        }

        var target = repository.Find(row.TargetId);
        if (target is null)
        {
            await ShowErrorAsync("差分表示対象が見つかりません。");
            return;
        }

        if (row.Diff is null)
        {
            await ShowErrorAsync(
                "既存の保存データに比較内容がないため、今回の差分を表示できません。" +
                "今回の正常チェックで比較内容を保存したため、次回以降の更新から表示できます。");
            return;
        }

        await dialogService.ShowContentDiffAsync(
            target,
            row.Diff,
            applicationCancellation.Token);
        StatusMessage = $"{target.Name} の差分を表示しました。";
    }

    /// <summary>埋め込みマニュアルの展開と既定ブラウザ起動をUIサービスへ委譲</summary>
    private void OpenManual()
    {
        var result = manualService.Open();
        if (result.IsSuccess)
        {
            StatusMessage = "ユーザーマニュアルを既定ブラウザで開きました。";
        }
        else
        {
            _ = ShowErrorAsync(result.ErrorMessage ?? "ユーザーマニュアルを開けませんでした。");
        }
    }

    /// <summary>追加画面の保存処理をCore管理サービスへ接続</summary>
    private async Task AddAsync()
    {
        var saved = await dialogService.ShowTargetEditorAsync(
            null,
            managementService.AddAsync,
            applicationCancellation.Token);
        if (saved is not null)
        {
            ReloadRows();
            StatusMessage = $"{saved.Name} を追加しました。";
        }
    }

    /// <summary>選択対象の編集画面をCore管理サービスへ接続</summary>
    private async Task EditAsync()
    {
        var row = GetSingleSelected();
        var target = row is null ? null : repository.Find(row.TargetId);
        if (target is null)
        {
            await ShowErrorAsync("編集対象が見つかりません。");
            return;
        }

        var saved = await dialogService.ShowTargetEditorAsync(
            target,
            (input, token) => managementService.EditAsync(target.Id, input, token),
            applicationCancellation.Token);
        if (saved is not null)
        {
            ReloadRows();
            StatusMessage = $"{saved.Name} を更新しました。";
        }
    }

    /// <summary>選択対象の削除確認後だけCore管理サービスへ変更を依頼</summary>
    private async Task DeleteAsync()
    {
        var row = GetSingleSelected();
        var target = row is null ? null : repository.Find(row.TargetId);
        if (target is null)
        {
            await ShowErrorAsync("削除対象が見つかりません。");
            return;
        }

        if (!await dialogService.ConfirmDeleteAsync(target, applicationCancellation.Token))
        {
            return;
        }

        var result = await managementService.DeleteAsync(target.Id, applicationCancellation.Token);
        if (!result.IsSuccess)
        {
            await ShowErrorAsync(result.ErrorMessage ?? "監視対象を削除できませんでした。");
            return;
        }

        ReloadRows();
        StatusMessage = $"{target.Name} を削除しました。";
    }

    /// <summary>確認済みポータブルJSONを検証し現在の監視対象設定へ置き換える</summary>
    private async Task ImportSettingsAsync()
    {
        if (HasRunningChecks())
        {
            await ShowErrorAsync("チェック中は設定をインポートできません。");
            return;
        }

        isSettingsTransferRunning = true;
        RaiseCommandStates();
        try
        {
            var path = await settingsFileService
                .PickImportPathAsync(applicationCancellation.Token);
            if (path is null)
            {
                return;
            }

            string json;
            try
            {
                json = await settingsFileService
                    .ReadAsync(path, applicationCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                await ShowErrorAsync(
                    "設定ファイルを読み込めませんでした。現在の設定を維持しています。");
                return;
            }

            IReadOnlyList<WatchTarget> imported;
            try
            {
                imported = settingsTransferService.Import(json);
            }
            catch (SettingsTransferException exception)
            {
                await ShowErrorAsync(exception.Message);
                return;
            }

            if (!await dialogService.ConfirmImportSettingsAsync(
                    repository.GetAll().Count,
                    imported.Count,
                    applicationCancellation.Token))
            {
                return;
            }

            var result = await managementService.ReplaceAllAsync(
                imported,
                applicationCancellation.Token);
            if (!result.IsSuccess)
            {
                await ShowErrorAsync(
                    result.ErrorMessage ??
                    "設定をインポートできませんでした。現在の設定を維持しています。");
                return;
            }

            ReloadRows();
            StatusMessage = "設定をインポートしました。";
        }
        finally
        {
            isSettingsTransferRunning = false;
            RaiseCommandStates();
        }
    }

    /// <summary>現在の監視対象設定を選択したポータブルJSONへ書き出す</summary>
    private async Task ExportSettingsAsync()
    {
        isSettingsTransferRunning = true;
        RaiseCommandStates();
        try
        {
            var path = await settingsFileService
                .PickExportPathAsync(applicationCancellation.Token);
            if (path is null)
            {
                return;
            }

            var json = settingsTransferService.Export(repository.GetAll());
            try
            {
                await settingsFileService.WriteAsync(
                    path,
                    json,
                    applicationCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                await ShowErrorAsync(
                    "設定ファイルを保存できませんでした。既存の設定は変更していません。");
                return;
            }

            StatusMessage = "設定をエクスポートしました。";
        }
        finally
        {
            isSettingsTransferRunning = false;
            RaiseCommandStates();
        }
    }

    /// <summary>選択件数が一件の場合だけ対象行を返却</summary>
    private WatchTargetRowViewModel? GetSingleSelected() =>
        selectedRows.Count == 1 ? selectedRows[0] : null;

    /// <summary>設定移行中ではない通常操作の利用可否を返却</summary>
    private bool CanUseOperationalCommands() =>
        isOperational && !isSettingsTransferRunning;

    /// <summary>一覧上の監視対象に実行中チェックが存在するかを返却</summary>
    private bool HasRunningChecks() =>
        Rows.Any(row => row.RunningCount > 0);

    /// <summary>エラー表示とステータスメッセージを一貫して更新</summary>
    private async Task ShowErrorAsync(string message)
    {
        StatusMessage = message;
        await dialogService.ShowErrorAsync(message, applicationCancellation.Token);
    }

    /// <summary>予期しない非同期例外を利用者向けの共通メッセージへ変換</summary>
    private void ReportUnexpectedError(Exception exception) =>
        _ = ShowErrorAsync("操作を完了できませんでした。");

    /// <summary>選択と実行状態に依存するすべてのコマンド可否を再評価</summary>
    private void RaiseCommandStates()
    {
        CheckAllCommand.RaiseCanExecuteChanged();
        CheckSelectedCommand.RaiseCanExecuteChanged();
        OpenBrowserCommand.RaiseCanExecuteChanged();
        ShowDiffCommand.RaiseCanExecuteChanged();
        AddCommand.RaiseCanExecuteChanged();
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        ImportSettingsCommand.RaiseCanExecuteChanged();
        ExportSettingsCommand.RaiseCanExecuteChanged();
    }
}
