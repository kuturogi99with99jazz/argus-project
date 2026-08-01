#nullable enable

namespace Argus.WinForms.Forms;

/// <summary>メイン画面のコントロール初期化部分</summary>
partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel headerLayout = null!;
    private Panel titlePanel = null!;
    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private FlowLayoutPanel summaryPanel = null!;
    private Label targetCountLabel = null!;
    private Label checkingCountLabel = null!;
    private FlowLayoutPanel toolbarPanel = null!;
    private Button checkAllButton = null!;
    private Button checkSelectedButton = null!;
    private Label toolbarSeparator = null!;
    private Button openBrowserButton = null!;
    private Button addButton = null!;
    private Button editButton = null!;
    private Button deleteButton = null!;
    private TableLayoutPanel listPanel = null!;
    private TableLayoutPanel listHeaderLayout = null!;
    private Panel listTitlePanel = null!;
    private Label listTitleLabel = null!;
    private Label listHintLabel = null!;
    private Label selectionCountLabel = null!;
    private DataGridView targetGrid = null!;
    private DataGridViewTextBoxColumn enabledColumn = null!;
    private DataGridViewTextBoxColumn nameColumn = null!;
    private DataGridViewTextBoxColumn urlColumn = null!;
    private DataGridViewTextBoxColumn statusColumn = null!;
    private DataGridViewTextBoxColumn lastCheckedColumn = null!;
    private DataGridViewTextBoxColumn errorColumn = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel messageStatusLabel = null!;
    private ToolStripStatusLabel copyrightStatusLabel = null!;
    private ToolStripStatusLabel debugStatusLabel = null!;
    private ToolStripStatusLabel versionStatusLabel = null!;

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
    rootLayout = new TableLayoutPanel();
    headerLayout = new TableLayoutPanel();
    titlePanel = new Panel();
    titleLabel = new Label();
    subtitleLabel = new Label();
    summaryPanel = new FlowLayoutPanel();
    targetCountLabel = new Label();
    checkingCountLabel = new Label();
    toolbarPanel = new FlowLayoutPanel();
    checkAllButton = new Button();
    checkSelectedButton = new Button();
    toolbarSeparator = new Label();
    openBrowserButton = new Button();
    addButton = new Button();
    editButton = new Button();
    deleteButton = new Button();
    listPanel = new TableLayoutPanel();
    listHeaderLayout = new TableLayoutPanel();
    listTitlePanel = new Panel();
    listTitleLabel = new Label();
    listHintLabel = new Label();
    selectionCountLabel = new Label();
    targetGrid = new DataGridView();
    statusStrip = new StatusStrip();
    messageStatusLabel = new ToolStripStatusLabel();
    copyrightStatusLabel = new ToolStripStatusLabel();
    debugStatusLabel = new ToolStripStatusLabel();
    versionStatusLabel = new ToolStripStatusLabel();
    enabledColumn = new DataGridViewTextBoxColumn();
    nameColumn = new DataGridViewTextBoxColumn();
    urlColumn = new DataGridViewTextBoxColumn();
    statusColumn = new DataGridViewTextBoxColumn();
    lastCheckedColumn = new DataGridViewTextBoxColumn();
    errorColumn = new DataGridViewTextBoxColumn();
    rootLayout.SuspendLayout();
    headerLayout.SuspendLayout();
    titlePanel.SuspendLayout();
    summaryPanel.SuspendLayout();
    toolbarPanel.SuspendLayout();
    listPanel.SuspendLayout();
    listHeaderLayout.SuspendLayout();
    listTitlePanel.SuspendLayout();
    ((System.ComponentModel.ISupportInitialize)targetGrid).BeginInit();
    statusStrip.SuspendLayout();
    SuspendLayout();
    // 
    // rootLayout
    // 
    rootLayout.ColumnCount = 1;
    rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    rootLayout.Controls.Add(headerLayout, 0, 0);
    rootLayout.Controls.Add(toolbarPanel, 0, 1);
    rootLayout.Controls.Add(listPanel, 0, 2);
    rootLayout.Controls.Add(statusStrip, 0, 3);
    rootLayout.Dock = DockStyle.Fill;
    rootLayout.Location = new Point(0, 0);
    rootLayout.Name = "rootLayout";
    rootLayout.RowCount = 4;
    rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
    rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
    rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
    rootLayout.Size = new Size(1100, 700);
    rootLayout.TabIndex = 0;
    // 
    // headerLayout
    // 
    headerLayout.ColumnCount = 2;
    headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    headerLayout.ColumnStyles.Add(new ColumnStyle());
    headerLayout.Controls.Add(titlePanel, 0, 0);
    headerLayout.Controls.Add(summaryPanel, 1, 0);
    headerLayout.Dock = DockStyle.Fill;
    headerLayout.Location = new Point(3, 3);
    headerLayout.Name = "headerLayout";
    headerLayout.Padding = new Padding(18, 10, 14, 8);
    headerLayout.RowCount = 1;
    headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    headerLayout.Size = new Size(1094, 66);
    headerLayout.TabIndex = 0;
    // 
    // titlePanel
    // 
    titlePanel.Controls.Add(titleLabel);
    titlePanel.Controls.Add(subtitleLabel);
    titlePanel.Dock = DockStyle.Fill;
    titlePanel.Location = new Point(21, 13);
    titlePanel.Name = "titlePanel";
    titlePanel.Size = new Size(818, 42);
    titlePanel.TabIndex = 0;
    // 
    // titleLabel
    // 
    titleLabel.AutoSize = true;
    titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
    titleLabel.Location = new Point(0, 0);
    titleLabel.Name = "titleLabel";
    titleLabel.Size = new Size(82, 32);
    titleLabel.TabIndex = 0;
    titleLabel.Text = "Argus";
    // 
    // subtitleLabel
    // 
    subtitleLabel.AutoSize = true;
    subtitleLabel.Location = new Point(83, 24);
    subtitleLabel.Name = "subtitleLabel";
    subtitleLabel.Size = new Size(136, 15);
    subtitleLabel.TabIndex = 1;
    subtitleLabel.Text = "Webページ更新チェッカー";
    // 
    // summaryPanel
    // 
    summaryPanel.AutoSize = true;
    summaryPanel.Controls.Add(targetCountLabel);
    summaryPanel.Controls.Add(checkingCountLabel);
    summaryPanel.Dock = DockStyle.Fill;
    summaryPanel.Location = new Point(845, 13);
    summaryPanel.Name = "summaryPanel";
    summaryPanel.Padding = new Padding(0, 10, 0, 0);
    summaryPanel.Size = new Size(232, 42);
    summaryPanel.TabIndex = 1;
    summaryPanel.WrapContents = false;
    // 
    // targetCountLabel
    // 
    targetCountLabel.BorderStyle = BorderStyle.FixedSingle;
    targetCountLabel.Location = new Point(0, 10);
    targetCountLabel.Margin = new Padding(0, 0, 8, 0);
    targetCountLabel.Name = "targetCountLabel";
    targetCountLabel.Size = new Size(112, 32);
    targetCountLabel.TabIndex = 0;
    targetCountLabel.Text = "監視対象 0件";
    targetCountLabel.TextAlign = ContentAlignment.MiddleCenter;
    // 
    // checkingCountLabel
    // 
    checkingCountLabel.BorderStyle = BorderStyle.FixedSingle;
    checkingCountLabel.Location = new Point(120, 10);
    checkingCountLabel.Margin = new Padding(0);
    checkingCountLabel.Name = "checkingCountLabel";
    checkingCountLabel.Size = new Size(112, 32);
    checkingCountLabel.TabIndex = 1;
    checkingCountLabel.Text = "チェック中 0件";
    checkingCountLabel.TextAlign = ContentAlignment.MiddleCenter;
    // 
    // toolbarPanel
    // 
    toolbarPanel.Controls.Add(checkAllButton);
    toolbarPanel.Controls.Add(checkSelectedButton);
    toolbarPanel.Controls.Add(toolbarSeparator);
    toolbarPanel.Controls.Add(openBrowserButton);
    toolbarPanel.Controls.Add(addButton);
    toolbarPanel.Controls.Add(editButton);
    toolbarPanel.Controls.Add(deleteButton);
    toolbarPanel.Dock = DockStyle.Fill;
    toolbarPanel.Location = new Point(3, 75);
    toolbarPanel.Name = "toolbarPanel";
    toolbarPanel.Padding = new Padding(14, 10, 0, 8);
    toolbarPanel.Size = new Size(1094, 52);
    toolbarPanel.TabIndex = 1;
    toolbarPanel.WrapContents = false;
    // 
    // checkAllButton
    // 
    checkAllButton.AccessibleName = "全件チェック";
    checkAllButton.Location = new Point(14, 10);
    checkAllButton.Margin = new Padding(0, 0, 8, 0);
    checkAllButton.Name = "checkAllButton";
    checkAllButton.Size = new Size(112, 36);
    checkAllButton.TabIndex = 0;
    checkAllButton.Text = "全件チェック";
    checkAllButton.UseVisualStyleBackColor = false;
    // 
    // checkSelectedButton
    // 
    checkSelectedButton.AccessibleName = "選択をチェック";
    checkSelectedButton.Location = new Point(134, 10);
    checkSelectedButton.Margin = new Padding(0, 0, 12, 0);
    checkSelectedButton.Name = "checkSelectedButton";
    checkSelectedButton.Size = new Size(126, 36);
    checkSelectedButton.TabIndex = 1;
    checkSelectedButton.Text = "選択をチェック";
    checkSelectedButton.UseVisualStyleBackColor = false;
    // 
    // toolbarSeparator
    // 
    toolbarSeparator.BorderStyle = BorderStyle.Fixed3D;
    toolbarSeparator.Location = new Point(272, 12);
    toolbarSeparator.Margin = new Padding(0, 2, 12, 2);
    toolbarSeparator.Name = "toolbarSeparator";
    toolbarSeparator.Size = new Size(2, 32);
    toolbarSeparator.TabIndex = 2;
    // 
    // openBrowserButton
    // 
    openBrowserButton.AccessibleName = "ブラウザで開く";
    openBrowserButton.Location = new Point(286, 10);
    openBrowserButton.Margin = new Padding(0, 0, 8, 0);
    openBrowserButton.Name = "openBrowserButton";
    openBrowserButton.Size = new Size(126, 36);
    openBrowserButton.TabIndex = 3;
    openBrowserButton.Text = "ブラウザで開く";
    openBrowserButton.UseVisualStyleBackColor = false;
    // 
    // addButton
    // 
    addButton.AccessibleName = "追加";
    addButton.Location = new Point(420, 10);
    addButton.Margin = new Padding(0, 0, 8, 0);
    addButton.Name = "addButton";
    addButton.Size = new Size(72, 36);
    addButton.TabIndex = 4;
    addButton.Text = "追加";
    addButton.UseVisualStyleBackColor = false;
    // 
    // editButton
    // 
    editButton.AccessibleName = "編集";
    editButton.Location = new Point(500, 10);
    editButton.Margin = new Padding(0, 0, 8, 0);
    editButton.Name = "editButton";
    editButton.Size = new Size(72, 36);
    editButton.TabIndex = 5;
    editButton.Text = "編集";
    editButton.UseVisualStyleBackColor = false;
    // 
    // deleteButton
    // 
    deleteButton.AccessibleName = "削除";
    deleteButton.Location = new Point(580, 10);
    deleteButton.Margin = new Padding(0);
    deleteButton.Name = "deleteButton";
    deleteButton.Size = new Size(72, 36);
    deleteButton.TabIndex = 6;
    deleteButton.Text = "削除";
    deleteButton.UseVisualStyleBackColor = false;
    // 
    // listPanel
    // 
    listPanel.ColumnCount = 1;
    listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    listPanel.Controls.Add(listHeaderLayout, 0, 0);
    listPanel.Controls.Add(targetGrid, 0, 1);
    listPanel.Dock = DockStyle.Fill;
    listPanel.Location = new Point(14, 142);
    listPanel.Margin = new Padding(14, 12, 14, 12);
    listPanel.Name = "listPanel";
    listPanel.RowCount = 2;
    listPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
    listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    listPanel.Size = new Size(1072, 514);
    listPanel.TabIndex = 2;
    // 
    // listHeaderLayout
    // 
    listHeaderLayout.ColumnCount = 2;
    listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    listHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
    listHeaderLayout.Controls.Add(listTitlePanel, 0, 0);
    listHeaderLayout.Controls.Add(selectionCountLabel, 1, 0);
    listHeaderLayout.Dock = DockStyle.Fill;
    listHeaderLayout.Location = new Point(3, 3);
    listHeaderLayout.Name = "listHeaderLayout";
    listHeaderLayout.Padding = new Padding(12, 8, 12, 6);
    listHeaderLayout.RowCount = 1;
    listHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
    listHeaderLayout.Size = new Size(1066, 52);
    listHeaderLayout.TabIndex = 0;
    // 
    // listTitlePanel
    // 
    listTitlePanel.Controls.Add(listTitleLabel);
    listTitlePanel.Controls.Add(listHintLabel);
    listTitlePanel.Dock = DockStyle.Fill;
    listTitlePanel.Location = new Point(15, 11);
    listTitlePanel.Name = "listTitlePanel";
    listTitlePanel.Size = new Size(940, 32);
    listTitlePanel.TabIndex = 0;
    // 
    // listTitleLabel
    // 
    listTitleLabel.AutoSize = true;
    listTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
    listTitleLabel.Location = new Point(0, 0);
    listTitleLabel.Name = "listTitleLabel";
    listTitleLabel.Size = new Size(99, 19);
    listTitleLabel.TabIndex = 0;
    listTitleLabel.Text = "監視対象一覧";
    // 
    // listHintLabel
    // 
    listHintLabel.AutoSize = true;
    listHintLabel.Location = new Point(103, 14);
    listHintLabel.Name = "listHintLabel";
    listHintLabel.Size = new Size(292, 15);
    listHintLabel.TabIndex = 1;
    listHintLabel.Text = "Ctrlキーを押しながら行を選択すると複数選択できます。";
    // 
    // selectionCountLabel
    // 
    selectionCountLabel.BorderStyle = BorderStyle.FixedSingle;
    selectionCountLabel.Dock = DockStyle.Fill;
    selectionCountLabel.Location = new Point(966, 12);
    selectionCountLabel.Margin = new Padding(8, 4, 0, 4);
    selectionCountLabel.Name = "selectionCountLabel";
    selectionCountLabel.Size = new Size(88, 30);
    selectionCountLabel.TabIndex = 1;
    selectionCountLabel.Text = "選択 0件";
    selectionCountLabel.TextAlign = ContentAlignment.MiddleCenter;
    // 
    // targetGrid
    // 
    targetGrid.AllowUserToAddRows = false;
    targetGrid.AllowUserToDeleteRows = false;
    targetGrid.AllowUserToResizeRows = false;
    targetGrid.AutoGenerateColumns = false;
    targetGrid.BackgroundColor = SystemColors.Window;
    targetGrid.BorderStyle = BorderStyle.None;
    targetGrid.ColumnHeadersHeight = 38;
    targetGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
    targetGrid.Dock = DockStyle.Fill;
    targetGrid.Location = new Point(3, 61);
    targetGrid.Name = "targetGrid";
    targetGrid.ReadOnly = true;
    targetGrid.RowHeadersVisible = false;
    targetGrid.RowTemplate.Height = 38;
    targetGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    targetGrid.Size = new Size(1066, 450);
    targetGrid.TabIndex = 1;
    targetGrid.Columns.AddRange(new DataGridViewColumn[]
    {
        enabledColumn,
        nameColumn,
        urlColumn,
        statusColumn,
        lastCheckedColumn,
        errorColumn,
    });
    // 
    // enabledColumn
    // 
    enabledColumn.DataPropertyName = "EnabledText";
    enabledColumn.HeaderText = "有効";
    enabledColumn.Name = "enabledColumn";
    enabledColumn.ReadOnly = true;
    // 
    // nameColumn
    // 
    nameColumn.DataPropertyName = "Name";
    nameColumn.HeaderText = "名前";
    nameColumn.Name = "nameColumn";
    nameColumn.ReadOnly = true;
    // 
    // urlColumn
    // 
    urlColumn.DataPropertyName = "Url";
    urlColumn.HeaderText = "URL";
    urlColumn.Name = "urlColumn";
    urlColumn.ReadOnly = true;
    // 
    // statusColumn
    // 
    statusColumn.DataPropertyName = "StatusText";
    statusColumn.HeaderText = "状態";
    statusColumn.Name = "statusColumn";
    statusColumn.ReadOnly = true;
    // 
    // lastCheckedColumn
    // 
    lastCheckedColumn.DataPropertyName = "LastCheckedText";
    lastCheckedColumn.HeaderText = "最終チェック";
    lastCheckedColumn.Name = "lastCheckedColumn";
    lastCheckedColumn.ReadOnly = true;
    // 
    // errorColumn
    // 
    errorColumn.DataPropertyName = "ErrorText";
    errorColumn.HeaderText = "エラー";
    errorColumn.Name = "errorColumn";
    errorColumn.ReadOnly = true;
    // 
    // statusStrip
    // 
    statusStrip.Dock = DockStyle.Fill;
    statusStrip.Items.AddRange(new ToolStripItem[] { messageStatusLabel, copyrightStatusLabel, debugStatusLabel, versionStatusLabel });
    statusStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
    statusStrip.Location = new Point(0, 668);
    statusStrip.Name = "statusStrip";
    statusStrip.Size = new Size(1100, 32);
    statusStrip.SizingGrip = false;
    statusStrip.TabIndex = 3;
    // 
    // messageStatusLabel
    // 
    messageStatusLabel.Name = "messageStatusLabel";
    messageStatusLabel.Size = new Size(94, 27);
    messageStatusLabel.Spring = true;
    messageStatusLabel.Text = "準備ができました。";
    messageStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
    // 
    // copyrightStatusLabel
    // 
    copyrightStatusLabel.Name = "copyrightStatusLabel";
    copyrightStatusLabel.Size = new Size(59, 27);
    copyrightStatusLabel.Text = "Copyright";
    // 
    // debugStatusLabel
    // 
    debugStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
    debugStatusLabel.Name = "debugStatusLabel";
    debugStatusLabel.Size = new Size(48, 27);
    debugStatusLabel.Text = "DEBUG";
    // 
    // versionStatusLabel
    // 
    versionStatusLabel.Name = "versionStatusLabel";
    versionStatusLabel.Size = new Size(37, 27);
    versionStatusLabel.Text = "v0.1.0";
    // 
    // MainForm
    // 
    AutoScaleDimensions = new SizeF(96F, 96F);
    AutoScaleMode = AutoScaleMode.Dpi;
    ClientSize = new Size(1100, 700);
    Controls.Add(rootLayout);
    Font = new Font("Segoe UI", 9F);
    MinimumSize = new Size(960, 600);
    Name = "MainForm";
    StartPosition = FormStartPosition.CenterScreen;
    Text = "Argus - Webページ更新チェッカー";
    rootLayout.ResumeLayout(false);
    rootLayout.PerformLayout();
    headerLayout.ResumeLayout(false);
    headerLayout.PerformLayout();
    titlePanel.ResumeLayout(false);
    titlePanel.PerformLayout();
    summaryPanel.ResumeLayout(false);
    toolbarPanel.ResumeLayout(false);
    listPanel.ResumeLayout(false);
    listHeaderLayout.ResumeLayout(false);
    listTitlePanel.ResumeLayout(false);
    listTitlePanel.PerformLayout();
    ((System.ComponentModel.ISupportInitialize)targetGrid).EndInit();
    statusStrip.ResumeLayout(false);
    statusStrip.PerformLayout();
    ResumeLayout(false);
  }
}
