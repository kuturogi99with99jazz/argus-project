using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Argus.Core.Models;

namespace Argus.Core.Persistence;

/// <summary>監視対象をUTF-8 JSONとしてローカルに保存する実装</summary>
public sealed class JsonTargetStore : ITargetStore
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly string filePath;
    private readonly SemaphoreSlim saveLock = new(1, 1);

    /// <summary>保存先を固定して一時ファイルによる安全な永続化を準備</summary>
    public JsonTargetStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = Path.GetFullPath(filePath);
    }

    /// <summary>ユーザーごとに一貫したJSON保存先を決定</summary>
    public static string ResolveDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Argus", "targets.json");
    }

    /// <summary>外部I/Oの失敗を呼び出し側へ伝播しつつデータを非同期に取得</summary>
    public async Task<TargetStoreDocument> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CleanupTemporaryFiles();

        if (!File.Exists(filePath))
        {
            await SaveAsync(TargetStoreDocument.Empty, cancellationToken).ConfigureAwait(false);
            return TargetStoreDocument.Empty;
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath, Utf8WithoutBom, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TargetStoreException(
                "監視対象データを読み込めませんでした。",
                exception);
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new TargetStoreException("監視対象データが空です。");
        }

        try
        {
            var dto = JsonSerializer.Deserialize<TargetStoreDocumentDto>(json, JsonOptions)
                ?? throw new TargetStoreException("監視対象データの形式が正しくありません。");
            return ConvertToDomain(dto);
        }
        catch (TargetStoreException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException or FormatException)
        {
            throw new TargetStoreException(
                "監視対象データの形式が正しくありません。",
                exception);
        }
    }

    /// <summary>永続化の成功後だけ状態変更を確定して整合性を維持</summary>
    public async Task SaveAsync(
        TargetStoreDocument document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        await saveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? temporaryPath = null;

        try
        {
            var directory = Path.GetDirectoryName(filePath)
                ?? throw new TargetStoreException("保存先ディレクトリを解決できません。");
            Directory.CreateDirectory(directory);

            temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            var dto = ConvertToDto(document);
            var json = JsonSerializer.Serialize(dto, JsonOptions);
            await File.WriteAllTextAsync(
                    temporaryPath,
                    json,
                    Utf8WithoutBom,
                    cancellationToken)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(filePath))
            {
                File.Replace(temporaryPath, filePath, null);
            }
            else
            {
                File.Move(temporaryPath, filePath);
            }

            temporaryPath = null;
        }
        finally
        {
            if (temporaryPath is not null)
            {
                TryDelete(temporaryPath);
            }

            saveLock.Release();
        }
    }

    /// <summary>JSON契約とドメインモデルの境界を保ち安全な永続化を支援</summary>
    private static TargetStoreDocument ConvertToDomain(TargetStoreDocumentDto dto)
    {
        if (dto.SchemaVersion != TargetStoreDocument.CurrentSchemaVersion)
        {
            throw new TargetStoreException(
                $"未対応のスキーマバージョンです: {dto.SchemaVersion}");
        }

        var targets = new List<WatchTarget>();
        var ids = new HashSet<Guid>();

        foreach (var targetDto in dto.Targets ?? [])
        {
            if (targetDto.Id == Guid.Empty || !ids.Add(targetDto.Id))
            {
                throw new TargetStoreException("監視対象のIDが空、または重複しています。");
            }

            var input = new WatchTargetInput(
                targetDto.Name ?? string.Empty,
                targetDto.Url ?? string.Empty,
                targetDto.Mode,
                targetDto.IsEnabled,
                targetDto.Memo,
                targetDto.CssSelector);

            WatchSnapshot? snapshot = null;
            if (targetDto.PreviousSnapshot is not null)
            {
                var hash = targetDto.PreviousSnapshot.ContentHash ?? string.Empty;
                if (hash.Length != 64 || !hash.All(Uri.IsHexDigit))
                {
                    throw new TargetStoreException(
                        $"監視対象「{targetDto.Name}」のスナップショットが正しくありません。");
                }

                snapshot = new WatchSnapshot(
                    hash.ToLowerInvariant(),
                    targetDto.PreviousSnapshot.CheckedAtUtc.ToUniversalTime(),
                    targetDto.PreviousSnapshot.ComparisonContent);
            }

            var result = WatchTargetValidator.Create(targetDto.Id, input, snapshot);
            if (!result.IsValid || result.Value is null)
            {
                var details = string.Join(
                    " ",
                    result.Errors.Select(error => error.Message));
                throw new TargetStoreException(
                    $"監視対象「{targetDto.Name ?? targetDto.Id.ToString()}」が正しくありません。{details}");
            }

            targets.Add(result.Value);
        }

        return new TargetStoreDocument(dto.SchemaVersion, targets);
    }

    /// <summary>JSON契約とドメインモデルの境界を保ち安全な永続化を支援</summary>
    private static TargetStoreDocumentDto ConvertToDto(TargetStoreDocument document) =>
        new()
        {
            SchemaVersion = document.SchemaVersion,
            Targets = document.Targets.Select(target => new WatchTargetDto
            {
                Id = target.Id,
                Name = target.Name,
                Url = target.Url.AbsoluteUri,
                Mode = target.Mode,
                IsEnabled = target.IsEnabled,
                Memo = target.Memo,
                CssSelector = target.CssSelector,
                PreviousSnapshot = target.PreviousSnapshot is null
                    ? null
                    : new WatchSnapshotDto
                    {
                        ContentHash = target.PreviousSnapshot.ContentHash,
                        CheckedAtUtc = target.PreviousSnapshot.CheckedAtUtc.ToUniversalTime(),
                        ComparisonContent = target.PreviousSnapshot.ComparisonContent
                    }
            }).ToList()
        };
    /// <summary>JSON契約とドメインモデルの境界を保ち安全な永続化を支援</summary>
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

    /// <summary>JSON契約とドメインモデルの境界を保ち安全な永続化を支援</summary>
    private void CleanupTemporaryFiles()
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory is null || !Directory.Exists(directory))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        foreach (var temporaryPath in Directory.EnumerateFiles(
                     directory,
                     $"{fileName}.*.tmp",
                     SearchOption.TopDirectoryOnly))
        {
            TryDelete(temporaryPath);
        }
    }

    /// <summary>JSON契約とドメインモデルの境界を保ち安全な永続化を支援</summary>
    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // A leftover file is harmless; the next startup will retry cleanup.
        }
    }

    /// <summary>保存文書のJSON契約をドメインモデルから分離</summary>
    private sealed class TargetStoreDocumentDto
    {
        /// <summary>SchemaVersionをJSON契約とドメインモデル間で受け渡すための値</summary>
        public int SchemaVersion { get; set; }
        /// <summary>TargetsをJSON契約とドメインモデル間で受け渡すための値</summary>
        public List<WatchTargetDto>? Targets { get; set; }
    }

    /// <summary>監視対象のJSON契約をドメインモデルから分離</summary>
    private sealed class WatchTargetDto
    {
        /// <summary>IdをJSON契約とドメインモデル間で受け渡すための値</summary>
        public Guid Id { get; set; }
        /// <summary>NameをJSON契約とドメインモデル間で受け渡すための値</summary>
        public string? Name { get; set; }
        /// <summary>UrlをJSON契約とドメインモデル間で受け渡すための値</summary>
        public string? Url { get; set; }
        /// <summary>ModeをJSON契約とドメインモデル間で受け渡すための値</summary>
        public WatchMode Mode { get; set; }
        /// <summary>IsEnabledをJSON契約とドメインモデル間で受け渡すための値</summary>
        public bool IsEnabled { get; set; }
        /// <summary>MemoをJSON契約とドメインモデル間で受け渡すための値</summary>
        public string? Memo { get; set; }
        /// <summary>CssSelectorをJSON契約とドメインモデル間で受け渡すための値</summary>
        public string? CssSelector { get; set; }
        /// <summary>PreviousSnapshotをJSON契約とドメインモデル間で受け渡すための値</summary>
        public WatchSnapshotDto? PreviousSnapshot { get; set; }
    }

    /// <summary>正常取得スナップショットのJSON契約をドメインモデルから分離</summary>
    private sealed class WatchSnapshotDto
    {
        /// <summary>ContentHashをJSON契約とドメインモデル間で受け渡すための値</summary>
        public string? ContentHash { get; set; }
        /// <summary>CheckedAtUtcをJSON契約とドメインモデル間で受け渡すための値</summary>
        public DateTimeOffset CheckedAtUtc { get; set; }
        /// <summary>ComparisonContentをJSON契約とドメインモデル間で受け渡すための値</summary>
        public string? ComparisonContent { get; set; }
    }
}
