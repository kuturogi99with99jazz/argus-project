using System.Text;
using Argus.Core.Persistence;
using Argus.Core.Services;
using Argus.WinForms.Forms;
using Argus.WinForms.Presentation;
using Argus.WinForms.Services;

namespace Argus.WinForms;

/// <summary>アプリケーションのエントリーポイント</summary>
internal static class Program
{
    /// <summary>UIとCoreの依存関係を構築しアプリケーションの生存期間を管理</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) =>
        {
            MessageBox.Show(
                "予期しないエラーが発生しました。処理を中断し、アプリケーションを継続します。",
                "Argus",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        using var applicationCancellation = new CancellationTokenSource();
        using var httpClientHandler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };
        using var httpClient = new HttpClient(httpClientHandler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var targetStore = new JsonTargetStore(JsonTargetStore.ResolveDefaultPath());
        TargetStoreDocument initialDocument;
        string? startupError = null;

        try
        {
            initialDocument = targetStore
                .LoadAsync(applicationCancellation.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception exception) when (
            exception is TargetStoreException or
            IOException or
            UnauthorizedAccessException)
        {
            initialDocument = TargetStoreDocument.Empty;
            startupError =
                "監視対象データを読み込めませんでした。既存ファイルは変更していません。" +
                "ファイルを確認してアプリケーションを再起動してください。";
        }

        var repository = new WatchTargetRepository(targetStore, initialDocument);
        var fetcher = new WebPageFetcher(httpClient);
        var normalizer = new HtmlTextNormalizer();
        var contentExtractor = new ComparisonContentExtractor(normalizer);
        var hashService = new Sha256HashService();
        var checkService = new WatchCheckService(fetcher, contentExtractor, hashService);
        using var coordinator = new CheckCoordinator(repository, checkService);
        var managementService = new WatchTargetManagementService(repository, coordinator);
        var browserService = new BrowserService();
        var applicationInfo = new ApplicationInfoProvider().Get();

        using var mainForm = new MainForm(
            repository,
            managementService,
            coordinator,
            browserService,
            applicationInfo,
            applicationCancellation,
            startupError);
        Application.Run(mainForm);
    }
}
