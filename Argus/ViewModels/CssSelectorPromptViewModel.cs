using Argus.Services;

namespace Argus.ViewModels;

/// <summary>CSSセレクタ相談用小窓の入力、プロンプト、コピー状態を管理するViewModel</summary>
public sealed class CssSelectorPromptViewModel : ViewModelBase
{
    private readonly string url;
    private readonly IClipboardService clipboardService;
    private readonly CssSelectorPromptBuilder promptBuilder;
    private string targetDescription;
    private string promptText;
    private string? copyStatus;
    private string? operationError;

    /// <summary>現在の監視対象編集画面から引き継いだURLとコピー境界を設定</summary>
    public CssSelectorPromptViewModel(
        string? url,
        IClipboardService clipboardService,
        CancellationToken cancellationToken)
    {
        this.url = url ?? string.Empty;
        this.clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        promptBuilder = new CssSelectorPromptBuilder();
        targetDescription = string.Empty;
        promptText = promptBuilder.Build(this.url, targetDescription);
        CopyCommand = new AsyncCommand(
            (_, token) => CopyAsync(token),
            onException: exception => OperationError = GetOperationErrorMessage(exception),
            cancellationToken: cancellationToken);
    }

    /// <summary>現在の監視対象編集画面から引き継いだURL</summary>
    public string Url => url;

    /// <summary>AIへ伝える監視箇所の説明</summary>
    public string TargetDescription
    {
        get => targetDescription;
        set
        {
            if (SetField(ref targetDescription, value))
            {
                PromptText = promptBuilder.Build(Url, TargetDescription);
                CopyStatus = null;
                OperationError = null;
            }
        }
    }

    /// <summary>AIへ貼り付ける読み取り専用プロンプト</summary>
    public string PromptText
    {
        get => promptText;
        private set => SetField(ref promptText, value);
    }

    /// <summary>コピー成功時に表示する利用者向け状態</summary>
    public string? CopyStatus
    {
        get => copyStatus;
        private set => SetField(ref copyStatus, value);
    }

    /// <summary>コピーに失敗したときに表示する利用者向けエラー</summary>
    public string? OperationError
    {
        get => operationError;
        private set => SetField(ref operationError, value);
    }

    /// <summary>生成済みプロンプトをクリップボードへコピーする非同期コマンド</summary>
    public AsyncCommand CopyCommand { get; }

    /// <summary>コピー状態を初期化してOSクリップボードへプロンプトを渡す</summary>
    private async Task CopyAsync(CancellationToken cancellationToken)
    {
        CopyStatus = null;
        OperationError = null;
        await clipboardService.CopyTextAsync(PromptText, cancellationToken);
        CopyStatus = "プロンプトをクリップボードへコピーしました。";
    }

    /// <summary>例外メッセージが空でも利用者へ原因を示せる文言へ変換</summary>
    private static string GetOperationErrorMessage(Exception exception) =>
        string.IsNullOrWhiteSpace(exception.Message)
            ? "プロンプトをコピーできませんでした。"
            : exception.Message;
}
