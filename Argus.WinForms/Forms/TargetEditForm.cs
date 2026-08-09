using Argus.Core.Models;
using Argus.Core.Services;
using Argus.WinForms.Presentation;

namespace Argus.WinForms.Forms;

/// <summary>監視対象の登録・編集を行う画面</summary>
public partial class TargetEditForm : Form
{
    private readonly Func<
        WatchTargetInput,
        CancellationToken,
        Task<WatchTargetChangeResult>> saveAsync;
    private readonly CancellationToken cancellationToken;

    /// <summary>追加と編集で共有する入力画面を対象状態に合わせて初期化</summary>
    public TargetEditForm(
        WatchTarget? target,
        Func<
            WatchTargetInput,
            CancellationToken,
            Task<WatchTargetChangeResult>> saveAsync,
        CancellationToken cancellationToken)
    {
        this.saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
        this.cancellationToken = cancellationToken;

        InitializeComponent();
        titleLabel.Text = target is null ? "監視対象を追加" : "監視対象を編集";
        Text = titleLabel.Text;
        modeComboBox.DisplayMember = nameof(WatchModeOption.DisplayName);
        modeComboBox.ValueMember = nameof(WatchModeOption.Value);
        modeComboBox.DataSource = WatchModeOption.All.ToArray();
        modeComboBox.SelectedValue = target?.Mode ?? WatchMode.HtmlText;
        modeComboBox.SelectedValueChanged += (_, _) => UpdateCssSelectorVisibility();

        if (target is not null)
        {
            nameTextBox.Text = target.Name;
            urlTextBox.Text = target.Url.AbsoluteUri;
            enabledCheckBox.Checked = target.IsEnabled;
            memoTextBox.Text = target.Memo ?? string.Empty;
            cssSelectorTextBox.Text = target.CssSelector ?? string.Empty;
        }

        UpdateCssSelectorVisibility();

        ApplyTheme();
        saveButton.Click += SaveButton_Click;
    }


    /// <summary>保存に成功した監視対象</summary>
    public WatchTarget? SavedTarget { get; private set; }
    /// <summary>検証と保存が成功した場合だけ編集結果を確定して画面を閉じる</summary>
    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        errorProvider.Clear();
        operationErrorLabel.Visible = false;
        saveButton.Enabled = false;
        cancelButton.Enabled = false;

        try
        {
            var input = new WatchTargetInput(
                nameTextBox.Text,
                urlTextBox.Text,
                SelectedMode,
                enabledCheckBox.Checked,
                memoTextBox.Text,
                cssSelectorTextBox.Text);
            var result = await saveAsync(input, cancellationToken);

            if (result.IsSuccess && result.Target is not null)
            {
                SavedTarget = result.Target;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            foreach (var error in result.ValidationErrors)
            {
                if (error.Field == nameof(WatchTargetInput.Name))
                {
                    errorProvider.SetError(nameTextBox, error.Message);
                }
                else if (error.Field == nameof(WatchTargetInput.Url))
                {
                    errorProvider.SetError(urlTextBox, error.Message);
                }
                else if (error.Field == nameof(WatchTargetInput.CssSelector))
                {
                    errorProvider.SetError(cssSelectorTextBox, error.Message);
                }
                else
                {
                    operationErrorLabel.Text = error.Message;
                    operationErrorLabel.Visible = true;
                }
            }

            if (result.ErrorMessage is not null)
            {
                operationErrorLabel.Text = result.ErrorMessage;
                operationErrorLabel.Visible = true;
            }

            if (result.ValidationErrors.Any(error =>
                    error.Field == nameof(WatchTargetInput.Name)))
            {
                nameTextBox.Focus();
            }
            else if (result.ValidationErrors.Any(error =>
                         error.Field == nameof(WatchTargetInput.Url)))
            {
                urlTextBox.Focus();
            }
            else if (result.ValidationErrors.Any(error =>
                         error.Field == nameof(WatchTargetInput.CssSelector)))
            {
                cssSelectorTextBox.Focus();
            }
        }
        catch (OperationCanceledException)
        {
            Close();
        }
        finally
        {
            if (!IsDisposed)
            {
                saveButton.Enabled = true;
                cancelButton.Enabled = true;
            }
        }
    }

    /// <summary>選択項目が未確定な初期化中も既定モードへ安全にフォールバック</summary>
    private WatchMode SelectedMode =>
        modeComboBox.SelectedValue is WatchMode mode ? mode : WatchMode.HtmlText;

    /// <summary>CSSセレクタ固有の入力欄を必要な場合だけ表示して画面密度を維持</summary>
    private void UpdateCssSelectorVisibility()
    {
        var visible = SelectedMode == WatchMode.CssSelector;
        cssSelectorLabel.Visible = visible;
        cssSelectorTextBox.Visible = visible;
        fieldsLayout.RowStyles[3].Height = visible ? 48F : 0F;
        if (!visible)
        {
            errorProvider.SetError(cssSelectorTextBox, string.Empty);
        }
    }

    /// <summary>画面間で一貫した配色と操作状態を再利用するための表示処理</summary>
    private void ApplyTheme()
    {
        if (SystemInformation.HighContrast)
        {
            return;
        }

        BackColor = SummerPalette.Surface;
        ForeColor = SummerPalette.TextPrimary;
        rootLayout.BackColor = SummerPalette.Surface;
        headerPanel.BackColor = SummerPalette.Background;
        titleLabel.ForeColor = SummerPalette.Primary;
        descriptionLabel.ForeColor = SummerPalette.TextSecondary;
        fieldsLayout.BackColor = SummerPalette.Surface;
        actionPanel.BackColor = SummerPalette.Background;
        operationErrorLabel.BackColor = SummerPalette.ErrorBackground;
        operationErrorLabel.ForeColor = SummerPalette.ErrorText;

        ConfigureButton(
            saveButton,
            SummerPalette.Primary,
            Color.White,
            SummerPalette.Primary);
        ConfigureButton(
            cancelButton,
            SummerPalette.Surface,
            SummerPalette.Primary,
            SummerPalette.Primary);
    }

    /// <summary>画面間で一貫した配色と操作状態を再利用するための表示処理</summary>
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
}
