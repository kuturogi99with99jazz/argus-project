using System.Security.Cryptography;
using System.Text;

namespace Argus.Core.Services;

/// <summary>SHA-256でコンテンツの安定した比較値を生成する実装</summary>
public sealed class Sha256HashService : IHashService
{
    /// <summary>比較用ハッシュの生成方式をSHA-256へ統一</summary>
    public string Compute(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
