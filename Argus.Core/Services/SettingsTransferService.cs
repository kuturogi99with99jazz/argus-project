using System.Text.Json;
using System.Text.Json.Serialization;
using Argus.Core.Models;

namespace Argus.Core.Services;

/// <summary>設定移行用JSONの形式が利用できないことを示す例外</summary>
public sealed class SettingsTransferException : Exception
{
    /// <summary>設定移行エラーの説明を保持する例外を生成</summary>
    public SettingsTransferException(string message)
        : base(message)
    {
    }

    /// <summary>原因例外を失わず設定移行エラーとして境界を明確化</summary>
    public SettingsTransferException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>監視対象の編集可能な設定をポータブルJSONへ移行</summary>
public sealed class SettingsTransferService
{
    /// <summary>ポータブル設定JSONを識別する固定文字列</summary>
    public const string CurrentFormat = "argus-settings";

    /// <summary>現在サポートしているポータブル設定JSONのバージョン</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>監視対象の編集可能な設定だけをUTF-8 JSON文字列へ変換</summary>
    public string Export(IReadOnlyList<WatchTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        var document = new PortableSettingsDocumentDto
        {
            Format = CurrentFormat,
            FormatVersion = CurrentFormatVersion,
            Targets = targets.Select(target => new PortableTargetDto
            {
                Name = target.Name,
                Url = target.Url.AbsoluteUri,
                Mode = target.Mode,
                IsEnabled = target.IsEnabled,
                Memo = target.Memo,
                CssSelector = target.CssSelector
            }).ToList()
        };

        return JsonSerializer.Serialize(document, CreateJsonOptions());
    }

    /// <summary>ポータブルJSONを全件検証してスナップショットなしの監視対象へ変換</summary>
    public IReadOnlyList<WatchTarget> Import(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new SettingsTransferException("設定ファイルが空です。");
        }

        PortableSettingsDocumentDto? document;
        try
        {
            document = JsonSerializer.Deserialize<PortableSettingsDocumentDto>(
                json,
                CreateJsonOptions());
        }
        catch (JsonException exception)
        {
            throw new SettingsTransferException(
                "設定ファイルのJSONが正しくありません。",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new SettingsTransferException(
                "設定ファイルのJSON形式に対応していません。",
                exception);
        }

        if (document is null)
        {
            throw new SettingsTransferException("設定ファイルが空です。");
        }

        if (!string.Equals(document.Format, CurrentFormat, StringComparison.Ordinal) ||
            document.FormatVersion != CurrentFormatVersion)
        {
            throw new SettingsTransferException(
                "設定ファイルの形式またはバージョンに対応していません。");
        }

        if (document.Targets is null)
        {
            throw new SettingsTransferException(
                "設定ファイルに監視対象の一覧がありません。");
        }

        var importedTargets = new List<WatchTarget>(document.Targets.Count);
        foreach (var targetDto in document.Targets)
        {
            if (targetDto is null)
            {
                throw new SettingsTransferException(
                    "設定ファイルに空の監視対象があります。");
            }

            if (targetDto.Mode is null || targetDto.IsEnabled is null)
            {
                throw new SettingsTransferException(
                    $"監視対象「{targetDto.Name ?? "不明"}」の必須項目がありません。");
            }

            var input = new WatchTargetInput(
                targetDto.Name ?? string.Empty,
                targetDto.Url ?? string.Empty,
                targetDto.Mode.Value,
                targetDto.IsEnabled.Value,
                targetDto.Memo,
                targetDto.CssSelector);
            var validation = WatchTargetValidator.Create(
                Guid.NewGuid(),
                input,
                null);
            if (!validation.IsValid || validation.Value is null)
            {
                var details = string.Join(
                    " ",
                    validation.Errors.Select(error => error.Message));
                throw new SettingsTransferException(
                    $"監視対象「{targetDto.Name ?? "不明"}」が正しくありません。{details}");
            }

            importedTargets.Add(validation.Value);
        }

        return importedTargets;
    }

    /// <summary>ポータブルJSONの文字コード、名前、列挙値の契約を構成</summary>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    /// <summary>ポータブル設定文書のJSON契約をドメインモデルから分離</summary>
    private sealed class PortableSettingsDocumentDto
    {
        /// <summary>形式識別子を受け渡すJSON値</summary>
        public string? Format { get; set; }

        /// <summary>形式バージョンを受け渡すJSON値</summary>
        public int? FormatVersion { get; set; }

        /// <summary>監視対象設定を受け渡すJSON値</summary>
        public List<PortableTargetDto>? Targets { get; set; }
    }

    /// <summary>監視対象設定のJSON契約をドメインモデルから分離</summary>
    private sealed class PortableTargetDto
    {
        /// <summary>監視対象名を受け渡すJSON値</summary>
        public string? Name { get; set; }

        /// <summary>取得先URLを受け渡すJSON値</summary>
        public string? Url { get; set; }

        /// <summary>比較方式を受け渡すJSON値</summary>
        public WatchMode? Mode { get; set; }

        /// <summary>有効状態を受け渡すJSON値</summary>
        public bool? IsEnabled { get; set; }

        /// <summary>利用者メモを受け渡すJSON値</summary>
        public string? Memo { get; set; }

        /// <summary>CSSセレクタを受け渡すJSON値</summary>
        public string? CssSelector { get; set; }
    }
}
