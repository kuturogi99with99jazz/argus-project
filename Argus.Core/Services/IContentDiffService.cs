using Argus.Core.Models;

namespace Argus.Core.Services;

/// <summary>比較対象文字列からUI非依存の差分結果を生成する契約</summary>
public interface IContentDiffService
{
    /// <summary>前回と今回の比較対象を文書順の行差分へ変換</summary>
    ContentDiff Generate(string previousContent, string currentContent);
}

/// <summary>差分生成を完了できなかったことを表す例外</summary>
public sealed class ContentDiffException : Exception
{
    /// <summary>差分生成エラーの利用者向け境界を生成</summary>
    public ContentDiffException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
