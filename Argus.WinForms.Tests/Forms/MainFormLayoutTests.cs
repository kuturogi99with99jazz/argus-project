using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Argus.Core.Persistence;
using Argus.Core.Services;
using Argus.WinForms.Forms;
using Argus.WinForms.Presentation;
using Argus.WinForms.Services;

namespace Argus.WinForms.Tests.Forms;

/// <summary>メイン画面のレイアウトと初期表示を検証するテスト</summary>
public sealed class MainFormLayoutTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Constructor_CreatesApprovedMainLayout()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var cancellation = new CancellationTokenSource();
                var repository = new WatchTargetRepository(
                    new InMemoryTargetStore(),
                    TargetStoreDocument.Empty);
                var checkService = new WatchCheckService(
                    new StubWebPageFetcher(),
                    new HtmlTextNormalizer(),
                    new Sha256HashService());
                using var coordinator = new CheckCoordinator(repository, checkService);
                var managementService =
                    new WatchTargetManagementService(repository, coordinator);
                using var form = new MainForm(
                    repository,
                    managementService,
                    coordinator,
                    new BrowserService(),
                    new ApplicationInfo(
                        "Copyright © 2026 SIA-ACT",
                        "v0.1.0",
                        true),
                    cancellation);

                form.Show();
                Application.DoEvents();

                Assert.Equal(new Size(1100, 700), form.ClientSize);
                Assert.Equal(new Size(960, 600), form.MinimumSize);
                Assert.Equal("Argus - Webページ更新チェッカー", form.Text);

                var grid = FindControl<DataGridView>(form);
                Assert.Equal(6, grid.Columns.Count);
                Assert.Equal(
                    ["有効", "名前", "URL", "状態", "最終チェック", "エラー"],
                    grid.Columns
                        .Cast<DataGridViewColumn>()
                        .Select(column => column.HeaderText)
                        .ToArray());

                var buttons = FindControls<Button>(form)
                    .Select(button => button.Text)
                    .ToHashSet(StringComparer.Ordinal);
                Assert.Contains("全件チェック", buttons);
                Assert.Contains("選択をチェック", buttons);
                Assert.Contains("ブラウザで開く", buttons);
                Assert.Contains("追加", buttons);
                Assert.Contains("編集", buttons);
                Assert.Contains("削除", buttons);

                var statusStrip = FindControl<StatusStrip>(form);
                Assert.Contains(
                    statusStrip.Items.Cast<ToolStripItem>(),
                    item => item.Text == "Copyright © 2026 SIA-ACT");
                Assert.Contains(
                    statusStrip.Items.Cast<ToolStripItem>(),
                    item => item.Text == "DEBUG" && item.Visible);
                Assert.Contains(
                    statusStrip.Items.Cast<ToolStripItem>(),
                    item => item.Text == "v0.1.0");

                form.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(
            thread.Join(TimeSpan.FromSeconds(10)),
            "MainFormの生成がタイムアウトしました。");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Constructor_WithStartupError_DisablesAllMajorActions()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var cancellation = new CancellationTokenSource();
                var repository = new WatchTargetRepository(
                    new InMemoryTargetStore(),
                    TargetStoreDocument.Empty);
                var checkService = new WatchCheckService(
                    new StubWebPageFetcher(),
                    new HtmlTextNormalizer(),
                    new Sha256HashService());
                using var coordinator = new CheckCoordinator(repository, checkService);
                var managementService =
                    new WatchTargetManagementService(repository, coordinator);
                using var form = new MainForm(
                    repository,
                    managementService,
                    coordinator,
                    new BrowserService(),
                    new ApplicationInfo(
                        "Copyright © 2026 SIA-ACT",
                        "v0.1.0",
                        true),
                    cancellation,
                    "読み込みエラー");

                _ = form.Handle;

                Assert.All(
                    FindControls<Button>(form),
                    button => Assert.False(button.Enabled));
                form.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(
            thread.Join(TimeSpan.FromSeconds(10)),
            "MainFormの生成がタイムアウトしました。");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static T FindControl<T>(Control root)
        where T : Control =>
        FindControls<T>(root).Single();

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T matching)
            {
                yield return matching;
            }

            foreach (var descendant in FindControls<T>(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class InMemoryTargetStore : ITargetStore
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<TargetStoreDocument> LoadAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(TargetStoreDocument.Empty);
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task SaveAsync(
            TargetStoreDocument document,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>外部依存や並行状態を決定的に制御するためのテスト補助型</summary>
    private sealed class StubWebPageFetcher : IWebPageFetcher
    {
        /// <summary>外部依存の応答をテストシナリオから制御可能にするための実装</summary>
        public Task<string> FetchAsync(
            Uri uri,
            CancellationToken cancellationToken) =>
            Task.FromResult("<html><body>sample</body></html>");
    }
}
