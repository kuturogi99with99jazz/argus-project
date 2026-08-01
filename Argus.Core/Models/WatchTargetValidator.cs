namespace Argus.Core.Models;

/// <summary>監視対象の入力値を業務ルールに従って検証・生成する機能</summary>
public static class WatchTargetValidator
{
    /// <summary>入力値を正規化し、監視対象登録に利用できる状態へ検証</summary>
    public static ValidationResult<WatchTargetInput> Validate(WatchTargetInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new List<ValidationError>();
        var name = input.Name?.Trim() ?? string.Empty;
        var urlText = input.Url?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            errors.Add(new ValidationError(
                nameof(WatchTargetInput.Name),
                "名前を入力してください。"));
        }

        if (!TryCreateHttpUri(urlText, out _))
        {
            errors.Add(new ValidationError(
                nameof(WatchTargetInput.Url),
                "http:// または https:// で始まる正しいURLを入力してください。"));
        }

        if (input.Mode != WatchMode.HtmlText)
        {
            errors.Add(new ValidationError(
                nameof(WatchTargetInput.Mode),
                "選択された監視モードには対応していません。"));
        }

        return errors.Count == 0
            ? ValidationResult<WatchTargetInput>.Success(
                input with
                {
                    Name = name,
                    Url = urlText,
                    Memo = NormalizeOptional(input.Memo)
                })
            : ValidationResult<WatchTargetInput>.Failure(errors);
    }


    /// <summary>検証済み入力から監視対象を生成</summary>
    public static ValidationResult<WatchTarget> Create(
        Guid id,
        WatchTargetInput input,
        WatchSnapshot? previousSnapshot)
    {
        var validation = Validate(input);
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationResult<WatchTarget>.Failure(validation.Errors);
        }

        var normalized = validation.Value;
        _ = TryCreateHttpUri(normalized.Url, out var uri);

        return ValidationResult<WatchTarget>.Success(
            new WatchTarget(
                id,
                normalized.Name,
                uri!,
                normalized.Mode,
                normalized.IsEnabled,
                normalized.Memo,
                previousSnapshot));
    }


    /// <summary>HTTPまたはHTTPSの絶対URIを安全に生成</summary>
    public static bool TryCreateHttpUri(string? value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate))
        {
            return false;
        }

        if (candidate.Scheme != Uri.UriSchemeHttp &&
            candidate.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        uri = candidate;
        return true;
    }


    /// <summary>任意入力をトリミングし、空文字をnullへ統一</summary>
    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
