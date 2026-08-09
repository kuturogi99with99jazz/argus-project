#nullable enable

namespace Argus.WinForms.Forms;

/// <summary>対象編集画面のコントロール初期化部分</summary>
partial class TargetEditForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel rootLayout = null!;
    private Panel headerPanel = null!;
    private Label titleLabel = null!;
    private Label descriptionLabel = null!;
    private TableLayoutPanel fieldsLayout = null!;
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label urlLabel = null!;
    private TextBox urlTextBox = null!;
    private Label modeLabel = null!;
    private ComboBox modeComboBox = null!;
    private Label cssSelectorLabel = null!;
    private TextBox cssSelectorTextBox = null!;
    private CheckBox enabledCheckBox = null!;
    private Label memoLabel = null!;
    private TextBox memoTextBox = null!;
    private Label operationErrorLabel = null!;
    private FlowLayoutPanel actionPanel = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;
    private ErrorProvider errorProvider = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        titleLabel = new Label();
        descriptionLabel = new Label();
        fieldsLayout = new TableLayoutPanel();
        nameLabel = new Label();
        nameTextBox = new TextBox();
        urlLabel = new Label();
        urlTextBox = new TextBox();
        modeLabel = new Label();
        modeComboBox = new ComboBox();
        cssSelectorLabel = new Label();
        cssSelectorTextBox = new TextBox();
        enabledCheckBox = new CheckBox();
        memoLabel = new Label();
        memoTextBox = new TextBox();
        operationErrorLabel = new Label();
        actionPanel = new FlowLayoutPanel();
        saveButton = new Button();
        cancelButton = new Button();
        errorProvider = new ErrorProvider(components);
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        fieldsLayout.SuspendLayout();
        actionPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)errorProvider).BeginInit();
        SuspendLayout();

        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(operationErrorLabel, 0, 1);
        rootLayout.Controls.Add(fieldsLayout, 0, 2);
        rootLayout.Controls.Add(actionPanel, 0, 3);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.RowCount = 4;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(descriptionLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Padding = new Padding(18, 12, 18, 8);

        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        titleLabel.Location = new Point(18, 10);
        titleLabel.Text = "監視対象を追加";

        descriptionLabel.AutoSize = true;
        descriptionLabel.Location = new Point(20, 39);
        descriptionLabel.Text = "更新を確認したいWebページを登録します。";

        operationErrorLabel.AutoSize = true;
        operationErrorLabel.Dock = DockStyle.Fill;
        operationErrorLabel.Margin = new Padding(18, 10, 18, 0);
        operationErrorLabel.Padding = new Padding(10, 8, 10, 8);
        operationErrorLabel.Text = "エラー";
        operationErrorLabel.Visible = false;

        fieldsLayout.ColumnCount = 2;
        fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fieldsLayout.Controls.Add(nameLabel, 0, 0);
        fieldsLayout.Controls.Add(nameTextBox, 1, 0);
        fieldsLayout.Controls.Add(urlLabel, 0, 1);
        fieldsLayout.Controls.Add(urlTextBox, 1, 1);
        fieldsLayout.Controls.Add(modeLabel, 0, 2);
        fieldsLayout.Controls.Add(modeComboBox, 1, 2);
        fieldsLayout.Controls.Add(cssSelectorLabel, 0, 3);
        fieldsLayout.Controls.Add(cssSelectorTextBox, 1, 3);
        fieldsLayout.Controls.Add(new Label(), 0, 4);
        fieldsLayout.Controls.Add(enabledCheckBox, 1, 4);
        fieldsLayout.Controls.Add(memoLabel, 0, 5);
        fieldsLayout.Controls.Add(memoTextBox, 1, 5);
        fieldsLayout.Dock = DockStyle.Fill;
        fieldsLayout.Padding = new Padding(18, 14, 24, 10);
        fieldsLayout.RowCount = 6;
        fieldsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        fieldsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        fieldsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        fieldsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        fieldsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        fieldsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        nameLabel.AutoSize = true;
        nameLabel.Margin = new Padding(0, 8, 8, 0);
        nameLabel.Text = "名前（必須）";

        nameTextBox.AccessibleName = "名前";
        nameTextBox.Dock = DockStyle.Top;
        nameTextBox.Margin = new Padding(0, 3, 0, 8);
        nameTextBox.MaxLength = 100;

        urlLabel.AutoSize = true;
        urlLabel.Margin = new Padding(0, 8, 8, 0);
        urlLabel.Text = "URL（必須）";

        urlTextBox.AccessibleName = "URL";
        urlTextBox.Dock = DockStyle.Top;
        urlTextBox.Margin = new Padding(0, 3, 0, 8);
        urlTextBox.MaxLength = 2048;

        modeLabel.AutoSize = true;
        modeLabel.Margin = new Padding(0, 8, 8, 0);
        modeLabel.Text = "監視モード";

        modeComboBox.AccessibleName = "監視モード";
        modeComboBox.Dock = DockStyle.Top;
        modeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        modeComboBox.Margin = new Padding(0, 3, 0, 8);

        cssSelectorLabel.AutoSize = true;
        cssSelectorLabel.Margin = new Padding(0, 8, 8, 0);
        cssSelectorLabel.Text = "CSSセレクタ（必須）";
        cssSelectorLabel.Visible = false;

        cssSelectorTextBox.AccessibleName = "CSSセレクタ";
        cssSelectorTextBox.Dock = DockStyle.Top;
        cssSelectorTextBox.Margin = new Padding(0, 3, 0, 8);
        cssSelectorTextBox.MaxLength = 2048;
        cssSelectorTextBox.Visible = false;

        enabledCheckBox.AccessibleName = "この監視対象を有効にする";
        enabledCheckBox.AutoSize = true;
        enabledCheckBox.Checked = true;
        enabledCheckBox.CheckState = CheckState.Checked;
        enabledCheckBox.Margin = new Padding(0, 8, 0, 0);
        enabledCheckBox.Text = "この監視対象を有効にする";

        memoLabel.AutoSize = true;
        memoLabel.Margin = new Padding(0, 8, 8, 0);
        memoLabel.Text = "メモ（任意）";

        memoTextBox.AccessibleName = "メモ";
        memoTextBox.Dock = DockStyle.Fill;
        memoTextBox.MaxLength = 500;
        memoTextBox.Multiline = true;
        memoTextBox.ScrollBars = ScrollBars.Vertical;

        actionPanel.Controls.Add(saveButton);
        actionPanel.Controls.Add(cancelButton);
        actionPanel.Dock = DockStyle.Fill;
        actionPanel.FlowDirection = FlowDirection.RightToLeft;
        actionPanel.Padding = new Padding(0, 10, 18, 8);

        saveButton.AccessibleName = "保存";
        saveButton.Margin = new Padding(8, 0, 0, 0);
        saveButton.Size = new Size(88, 36);
        saveButton.Text = "保存";
        saveButton.UseVisualStyleBackColor = false;

        cancelButton.AccessibleName = "キャンセル";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Margin = new Padding(0);
        cancelButton.Size = new Size(88, 36);
        cancelButton.Text = "キャンセル";
        cancelButton.UseVisualStyleBackColor = false;

        errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
        errorProvider.ContainerControl = this;

        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = cancelButton;
        ClientSize = new Size(620, 500);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(620, 500);
        Name = "TargetEditForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "監視対象";
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        fieldsLayout.ResumeLayout(false);
        fieldsLayout.PerformLayout();
        actionPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)errorProvider).EndInit();
        ResumeLayout(false);
    }
}
