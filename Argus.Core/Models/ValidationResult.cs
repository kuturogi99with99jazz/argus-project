namespace Argus.Core.Models;

/// <summary>入力項目に対する検証エラー</summary>
public sealed record ValidationError(string Field, string Message);

/// <summary>検証結果と検証済みの値</summary>
public sealed class ValidationResult<T>
    where T : class
{
    /// <summary>値と検証エラーの組み合わせを不変な結果として保持</summary>
    private ValidationResult(T? value, IReadOnlyList<ValidationError> errors)
    {
        Value = value;
        Errors = errors;
    }


    /// <summary>検証に成功した値</summary>
    public T? Value { get; }

    /// <summary>入力検証で検出したエラー一覧</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>入力がすべての検証規則を満たしているかどうか</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>検証成功時の値を保持する結果を生成</summary>
    public static ValidationResult<T> Success(T value) =>
        new(value, Array.Empty<ValidationError>());

    /// <summary>検証エラーを保持する結果を生成</summary>
    public static ValidationResult<T> Failure(IEnumerable<ValidationError> errors) =>
        new(null, errors.ToArray());
}
