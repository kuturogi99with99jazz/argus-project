using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Argus.Services;
using Argus.Views;
using Argus.ViewModels;
using Argus.Core.Persistence;
using Argus.Core.Services;
using System.Text;

namespace Argus;

/// <summary>Argusのライフサイクルとルート画面を構成するアプリケーション</summary>
public sealed partial class App : Application
{
    private CancellationTokenSource? applicationCancellation;
    private HttpClientHandler? httpClientHandler;
    private HttpClient? httpClient;

    /// <summary>XAMLで定義したFluent Themeとアプリケーション資源を読み込み</summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>正式版のデスクトップ環境でメインウィンドウを起動</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            applicationCancellation = new CancellationTokenSource();
            httpClientHandler = new HttpClientHandler { AllowAutoRedirect = true };
            httpClient = new HttpClient(httpClientHandler)
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
                exception is TargetStoreException or IOException or UnauthorizedAccessException)
            {
                initialDocument = TargetStoreDocument.Empty;
                startupError =
                    "監視対象データを読み込めませんでした。保存ファイルは変更していません。" +
                    "ファイルを確認してアプリケーションを再起動してください。";
            }

            var repository = new WatchTargetRepository(targetStore, initialDocument);
            var fetcher = new WebPageFetcher(httpClient);
            var extractor = new ComparisonContentExtractor(new HtmlTextNormalizer());
            var checkService = new WatchCheckService(fetcher, extractor, new Sha256HashService());
            var coordinator = new CheckCoordinator(repository, checkService);
            var managementService = new WatchTargetManagementService(repository, coordinator);
            MainWindow? mainWindow = null;
            var dialogService = new DialogService(() => mainWindow);
            var viewModel = new MainWindowViewModel(
                repository,
                managementService,
                coordinator,
                new BrowserService(),
                dialogService,
                new AvaloniaUiDispatcher(),
                new ApplicationInfoProvider().Get(),
                applicationCancellation,
                startupError);
            mainWindow = new MainWindow(viewModel);
            desktop.MainWindow = mainWindow;
            desktop.Exit += Desktop_Exit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>デスクトップ終了後にHTTPとキャンセル資源を確実に解放</summary>
    private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs eventArgs)
    {
        httpClient?.Dispose();
        httpClientHandler?.Dispose();
        applicationCancellation?.Dispose();
    }
}
