using System.ComponentModel;
using Argus.Core.Models;
using Argus.Core.Persistence;
using Argus.Core.Services;
using Argus.WinForms.Presentation;
using Argus.WinForms.Services;

namespace Argus.WinForms.Forms;

/// <summary>監視対象一覧とチェック操作を提供するメイン画面</summary>
public partial class MainForm : Form
{
    private readonly WatchTargetRepository repository;
    private readonly WatchTargetManagementService managementService;
    private readonly CheckCoordinator checkCoordinator;
    private readonly BrowserService browserService;
    private readonly CancellationTokenSource applicationCancellation;
    private readonly BindingList<WatchTargetRowViewModel> rows = [];
    private readonly BindingSource rowSource = new();
    private readonly Font boldGridFont;
    private readonly Font boldGridHeaderFont;
    private readonly string? startupError;
    private readonly bool isOperational;

    /// <summary>依存サービスを受け取り、監視対象一覧画面を初期化</summary>
    public MainForm(
        WatchTargetRepository repository,
        WatchTargetManagementService managementService,
        CheckCoordinator checkCoordinator,
        BrowserService browserService,
        ApplicationInfo applicationInfo,
        CancellationTokenSource applicationCancellation,
        string? startupError = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.managementService = managementService
            ?? throw new ArgumentNullException(nameof(managementService));
        this.checkCoordinator = checkCoordinator
            ?? throw new ArgumentNullException(nameof(checkCoordinator));
        this.browserService = browserService
            ?? throw new ArgumentNullException(nameof(browserService));
        this.applicationCancellation = applicationCancellation
            ?? throw new ArgumentNullException(nameof(applicationCancellation));
        this.startupError = startupError;
        isOperational = startupError is null;

        InitializeComponent();
        boldGridFont = new Font(targetGrid.Font, FontStyle.Bold);
        boldGridHeaderFont = new Font(targetGrid.Font, FontStyle.Bold);
        ConfigureApplicationInfo(applicationInfo);
        ApplyTheme();
        ConfigureBindings();
        WireEvents();
        ReloadRows();
        UpdateActionState();
    }


    /// <summary>アプリケーション情報をステータス表示へ反映</summary>
    private void ConfigureApplicationInfo(ApplicationInfo applicationInfo)
    {
        copyrightStatusLabel.Text = applicationInfo.Copyright ?? string.Empty;
        copyrightStatusLabel.Visible = applicationInfo.Copyright is not null;
        versionStatusLabel.Text = applicationInfo.Version;
        debugStatusLabel.Visible = applicationInfo.IsDebug;
    }


    /// <summary>一覧データとグリッドのデータバインディングを設定</summary>
    private void ConfigureBindings()
    {
        rowSource.DataSource = rows;
        targetGrid.DataSource = rowSource;
    }


    /// <summary>画面イベントと操作イベントの購読を設定</summary>
    private void WireEvents()
    {
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
        targetGrid.SelectionChanged += (_, _) => UpdateActionState();
        targetGrid.CellFormatting += TargetGrid_CellFormatting;
        checkAllButton.Click += CheckAllButton_Click;
        checkSelectedButton.Click += CheckSelectedButton_Click;
        openBrowserButton.Click += OpenBrowserButton_Click;
        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;
        checkCoordinator.ExecutionChanged += CheckCoordinator_ExecutionChanged;
        checkCoordinator.CheckCompleted += CheckCoordinator_CheckCompleted;
    }


    /// <summary>画面表示時に起動エラーを通知</summary>
    private void MainForm_Shown(object? sender, EventArgs e)
    {
        if (startupError is null)
        {
            return;
        }

        messageStatusLabel.Text = startupError;
        MessageBox.Show(
            this,
            startupError,
            "監視対象データの読み込みエラー",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }


    /// <summary>画面終了開始時に実行中処理のキャンセルを通知</summary>
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        applicationCancellation.Cancel();
    }


    /// <summary>画面終了後に購読解除とリソース解放を実行</summary>
    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        checkCoordinator.ExecutionChanged -= CheckCoordinator_ExecutionChanged;
        checkCoordinator.CheckCompleted -= CheckCoordinator_CheckCompleted;
        boldGridFont.Dispose();
        boldGridHeaderFont.Dispose();
        rowSource.Dispose();
    }


    /// <summary>有効な全監視対象の確認を開始</summary>
    private async void CheckAllButton_Click(object? sender, EventArgs e)
    {
        var tasks = checkCoordinator.StartAll(applicationCancellation.Token);
        await ObserveChecksAsync(tasks);
    }


    /// <summary>選択された監視対象の確認を開始</summary>
    private async void CheckSelectedButton_Click(object? sender, EventArgs e)
    {
        var targetIds = GetSelectedRows()
            .Where(row => row.IsEnabled)
            .Select(row => row.TargetId)
            .ToArray();
        var tasks = checkCoordinator.StartSelected(
            targetIds,
            applicationCancellation.Token);
        await ObserveChecksAsync(tasks);
    }


    /// <summary>選択された監視対象を既定ブラウザーで開く</summary>
    private void OpenBrowserButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedRows();
        if (selected.Count != 1)
        {
            ShowOperationError("ブラウザで開く監視対象を1件選択してください。");
            return;
        }

        var target = repository.Find(selected[0].TargetId);
        if (target is null)
        {
            ShowOperationError("選択した監視対象が見つかりません。");
            return;
        }

        var result = browserService.Open(target);
        messageStatusLabel.Text = result.IsSuccess
            ? $"{target.Name} を既定ブラウザで開きました。"
            : result.ErrorMessage;
        if (!result.IsSuccess)
        {
            ShowOperationError(result.ErrorMessage ?? "ブラウザを開けませんでした。");
        }
    }


    /// <summary>監視対象の新規登録画面を開く</summary>
    private void AddButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new TargetEditForm(
            null,
            (input, token) => managementService.AddAsync(input, token),
            applicationCancellation.Token);

        if (dialog.ShowDialog(this) == DialogResult.OK &&
            dialog.SavedTarget is not null)
        {
            ReloadRows(dialog.SavedTarget.Id);
            messageStatusLabel.Text = $"{dialog.SavedTarget.Name} を追加しました。";
        }
    }


    /// <summary>選択された監視対象の編集画面を開く</summary>
    private void EditButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedRows();
        if (selected.Count != 1)
        {
            return;
        }

        var target = repository.Find(selected[0].TargetId);
        if (target is null)
        {
            ShowOperationError("編集対象が見つかりません。");
            return;
        }

        using var dialog = new TargetEditForm(
            target,
            (input, token) => managementService.EditAsync(target.Id, input, token),
            applicationCancellation.Token);

        if (dialog.ShowDialog(this) == DialogResult.OK &&
            dialog.SavedTarget is not null)
        {
            ReloadRows(dialog.SavedTarget.Id);
            messageStatusLabel.Text = $"{dialog.SavedTarget.Name} を更新しました。";
        }
    }


    /// <summary>選択された監視対象を確認後に削除</summary>
    private async void DeleteButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedRows();
        if (selected.Count != 1)
        {
            return;
        }

        var target = repository.Find(selected[0].TargetId);
        if (target is null)
        {
            ShowOperationError("削除対象が見つかりません。");
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"「{target.Name}」を削除しますか？\r\n保存済みの前回チェックデータも削除されます。",
            "監視対象の削除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        var result = await managementService.DeleteAsync(
            target.Id,
            applicationCancellation.Token);
        if (!result.IsSuccess)
        {
            ShowOperationError(result.ErrorMessage ?? "監視対象を削除できませんでした。");
            return;
        }

        ReloadRows();
        messageStatusLabel.Text = $"{target.Name} を削除しました。";
    }


    /// <summary>開始した確認処理の完了を待ち、画面エラーを通知</summary>
    private async Task ObserveChecksAsync(IReadOnlyList<Task<CheckResult>> tasks)
    {
        if (tasks.Count == 0)
        {
            ShowOperationError("チェックできる有効な監視対象が選択されていません。");
            return;
        }

        messageStatusLabel.Text =
            $"{tasks.Count}件のチェックを開始しました。チェック中も他の操作を行えます。";

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Application shutdown intentionally cancels outstanding checks.
        }
        catch
        {
            if (!IsDisposed)
            {
                ShowOperationError("チェック処理を完了できませんでした。");
            }
        }
    }


    /// <summary>確認実行数の変化を一覧行へ反映</summary>
    private void CheckCoordinator_ExecutionChanged(
        object? sender,
        CheckExecutionChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            var row = rows.FirstOrDefault(item => item.TargetId == e.TargetId);
            row?.SetRunningCount(e.RunningCount);
            targetGrid.Invalidate();
            UpdateSummary();
            UpdateActionState();
        });
    }


    /// <summary>確認完了結果を一覧行とステータスへ反映</summary>
    private void CheckCoordinator_CheckCompleted(
        object? sender,
        CheckCompletedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            var row = rows.FirstOrDefault(item => item.TargetId == e.Result.TargetId);
            row?.ApplyCheckResult(e.Result);
            targetGrid.Invalidate();
            var target = repository.Find(e.Result.TargetId);
            messageStatusLabel.Text = e.Result.Status == CheckStatus.Error
                ? $"{target?.Name ?? "監視対象"}: {e.Result.ErrorMessage}"
                : $"{target?.Name ?? "監視対象"} のチェックが完了しました。";
        });
    }


    /// <summary>画面状態をUIスレッド上で安全に更新</summary>
    private void RunOnUiThread(Action action)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // The form handle was destroyed while a background check completed.
            }
        }
        else
        {
            action();
        }
    }


    /// <summary>リポジトリの監視対象で一覧行を再構築</summary>
    private void ReloadRows(Guid? selectTargetId = null)
    {
        rows.RaiseListChangedEvents = false;
        rows.Clear();
        foreach (var target in repository.GetAll())
        {
            var row = new WatchTargetRowViewModel(target);
            row.SetRunningCount(checkCoordinator.GetRunningCount(target.Id));
            rows.Add(row);
        }

        rows.RaiseListChangedEvents = true;
        rowSource.ResetBindings(false);

        targetGrid.ClearSelection();
        if (selectTargetId.HasValue)
        {
            foreach (DataGridViewRow gridRow in targetGrid.Rows)
            {
                if (gridRow.DataBoundItem is WatchTargetRowViewModel row &&
                    row.TargetId == selectTargetId.Value)
                {
                    gridRow.Selected = true;
                    targetGrid.CurrentCell = gridRow.Cells[nameColumn.Index];
                    break;
                }
            }
        }

        UpdateSummary();
        UpdateActionState();
    }


    /// <summary>グリッドで選択されている監視対象行を取得</summary>
    private List<WatchTargetRowViewModel> GetSelectedRows() =>
        targetGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<WatchTargetRowViewModel>()
            .ToList();

    /// <summary>監視対象数と確認中件数の表示を更新</summary>
    private void UpdateSummary()
    {
        targetCountLabel.Text = $"監視対象 {rows.Count}件";
        var checkingTargets = rows.Count(row => row.RunningCount > 0);
        checkingCountLabel.Text = $"チェック中 {checkingTargets}件";
        selectionCountLabel.Text = $"選択 {targetGrid.SelectedRows.Count}件";
    }


    /// <summary>選択状態と実行状態に応じて操作ボタンを更新</summary>
    private void UpdateActionState()
    {
        var selected = GetSelectedRows();
        var single = selected.Count == 1 ? selected[0] : null;

        checkAllButton.Enabled = isOperational && rows.Any(row => row.IsEnabled);
        checkSelectedButton.Enabled =
            isOperational && selected.Any(row => row.IsEnabled);
        openBrowserButton.Enabled = isOperational && single is not null;
        addButton.Enabled = isOperational;
        editButton.Enabled =
            isOperational && single is not null && single.RunningCount == 0;
        deleteButton.Enabled =
            isOperational && single is not null && single.RunningCount == 0;
        UpdateSummary();
    }


    /// <summary>監視状態に応じたグリッドセルの表示属性を設定</summary>
    private void TargetGrid_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 ||
            targetGrid.Rows[e.RowIndex].DataBoundItem is not WatchTargetRowViewModel row)
        {
            return;
        }

        if (SystemInformation.HighContrast)
        {
            return;
        }

        var cellStyle = e.CellStyle ?? targetGrid.DefaultCellStyle;
        if (!row.IsEnabled)
        {
            cellStyle.BackColor = SummerPalette.DisabledBackground;
            cellStyle.ForeColor = SummerPalette.DisabledText;
        }

        if (targetGrid.Columns[e.ColumnIndex] == statusColumn)
        {
            var appearance = row.RunningCount > 0
                ? CheckStatusAppearance.Checking
                : CheckStatusAppearance.Get(row.Status);
            cellStyle.BackColor = appearance.Background;
            cellStyle.ForeColor = appearance.Foreground;
            cellStyle.Font = boldGridFont;
        }
        else if (targetGrid.Columns[e.ColumnIndex] == errorColumn &&
                 row.ErrorMessage is not null)
        {
            cellStyle.ForeColor = SummerPalette.Danger;
            cellStyle.Font = boldGridFont;
        }
    }


    /// <summary>アプリケーション配色を画面コントロールへ適用</summary>
    private void ApplyTheme()
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        BackColor = SummerPalette.Background;
        ForeColor = SummerPalette.TextPrimary;
        rootLayout.BackColor = SummerPalette.Background;
        headerLayout.BackColor = SummerPalette.Surface;
        titleLabel.ForeColor = SummerPalette.Primary;
        subtitleLabel.ForeColor = SummerPalette.TextSecondary;
        summaryPanel.BackColor = SummerPalette.Surface;
        targetCountLabel.BackColor = SummerPalette.Background;
        checkingCountLabel.BackColor = SummerPalette.Background;
        toolbarPanel.BackColor = SummerPalette.Background;
        listPanel.BackColor = SummerPalette.Border;
        listHeaderLayout.BackColor = SummerPalette.Surface;
        listHintLabel.ForeColor = SummerPalette.TextSecondary;
        selectionCountLabel.BackColor = SummerPalette.Surface;
        selectionCountLabel.ForeColor = SummerPalette.TextSecondary;
        statusStrip.BackColor = SummerPalette.GridHeader;
        statusStrip.ForeColor = SummerPalette.TextPrimary;
        copyrightStatusLabel.ForeColor = SummerPalette.TextSecondary;
        debugStatusLabel.BackColor = SummerPalette.UpdatedBackground;
        debugStatusLabel.ForeColor = SummerPalette.UpdatedText;

        ConfigureButton(
            checkAllButton,
            SummerPalette.Primary,
            Color.White,
            SummerPalette.Primary);
        ConfigureButton(
            checkSelectedButton,
            SummerPalette.Primary,
            Color.White,
            SummerPalette.Primary);
        ConfigureButton(
            openBrowserButton,
            SummerPalette.Surface,
            SummerPalette.Primary,
            SummerPalette.Primary);
        ConfigureButton(
            addButton,
            SummerPalette.Surface,
            SummerPalette.Primary,
            SummerPalette.Primary);
        ConfigureButton(
            editButton,
            SummerPalette.Surface,
            SummerPalette.Primary,
            SummerPalette.Primary);
        ConfigureButton(
            deleteButton,
            SummerPalette.Surface,
            SummerPalette.Danger,
            SummerPalette.Danger);

        targetGrid.EnableHeadersVisualStyles = false;
        targetGrid.BackgroundColor = SummerPalette.Surface;
        targetGrid.GridColor = SummerPalette.Border;
        targetGrid.ColumnHeadersDefaultCellStyle.BackColor = SummerPalette.GridHeader;
        targetGrid.ColumnHeadersDefaultCellStyle.ForeColor = SummerPalette.TextPrimary;
        targetGrid.ColumnHeadersDefaultCellStyle.Font = boldGridHeaderFont;
        targetGrid.DefaultCellStyle.BackColor = SummerPalette.Surface;
        targetGrid.DefaultCellStyle.ForeColor = SummerPalette.TextPrimary;
        targetGrid.DefaultCellStyle.SelectionBackColor = SummerPalette.Selection;
        targetGrid.DefaultCellStyle.SelectionForeColor = SummerPalette.SelectionText;
        targetGrid.AlternatingRowsDefaultCellStyle.BackColor =
            SummerPalette.GridAlternate;
    }


    /// <summary>ボタンの配色と状態変化時の表示を設定</summary>
    private static void ConfigureButton(
        Button button,
        Color background,
        Color foreground,
        Color border)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor =
            background == SummerPalette.Primary
                ? SummerPalette.PrimaryHover
                : SummerPalette.Background;
        button.FlatAppearance.MouseDownBackColor =
            background == SummerPalette.Primary
                ? SummerPalette.PrimaryHover
                : SummerPalette.Selection;
        button.BackColor = background;
        button.ForeColor = foreground;
        button.EnabledChanged += (_, _) =>
        {
            button.BackColor = button.Enabled
                ? background
                : SummerPalette.DisabledBackground;
            button.ForeColor = button.Enabled
                ? foreground
                : SummerPalette.DisabledText;
            button.FlatAppearance.BorderColor = button.Enabled
                ? border
                : SummerPalette.Border;
        };
        button.Enter += (_, _) =>
        {
            button.FlatAppearance.BorderColor = SummerPalette.Focus;
            button.FlatAppearance.BorderSize = 2;
        };
        button.Leave += (_, _) =>
        {
            button.FlatAppearance.BorderColor = button.Enabled
                ? border
                : SummerPalette.Border;
            button.FlatAppearance.BorderSize = 1;
        };
    }


    /// <summary>操作エラーをステータス表示とダイアログへ通知</summary>
    private void ShowOperationError(string message)
    {
        messageStatusLabel.Text = message;
        MessageBox.Show(
            this,
            message,
            "Argus",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
