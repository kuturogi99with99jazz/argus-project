namespace Argus.Services;

/// <summary>CSSセレクタをAIへ相談するための定型プロンプトを生成する機能</summary>
public sealed class CssSelectorPromptBuilder
{
    /// <summary>ページURLと監視箇所の説明を、利用者が確認・コピーできる日本語プロンプトへ変換</summary>
    public string Build(string? url, string? targetDescription)
    {
        var displayUrl = FormatValue(url);
        var displayDescription = FormatValue(targetDescription);

        return string.Join(
            Environment.NewLine,
            "あなたはWebページのCSSセレクタ設定を支援するアシスタントです。",
            "以下のWebページで、指定した箇所だけをArgusで監視するためのCSSセレクタを提案してください。",
            string.Empty,
            $"ページURL: {displayUrl}",
            $"監視したい箇所: {displayDescription}",
            string.Empty,
            "次の内容を日本語で回答してください。",
            "1. 推奨するCSSセレクタをコードブロックで示してください。",
            "2. そのCSSセレクタが選択する範囲を説明してください。",
            "3. より安定した代替候補があれば示してください。",
            "4. ページを確認できない場合は、ブラウザの開発者ツールで確認する手順を説明してください。",
            string.Empty,
            "Argusでは、提案されたCSSセレクタに一致する要素だけを比較します。"
        );
    }

    /// <summary>未入力値を明示し、プロンプトへそのまま表示できる文字列へ整形</summary>
    private static string FormatValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "（未入力）" : value.Trim();
}
