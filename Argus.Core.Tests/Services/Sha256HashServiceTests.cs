using Argus.Core.Services;

namespace Argus.Core.Tests.Services;

/// <summary>SHA-256ハッシュ生成を検証するテスト</summary>
public sealed class Sha256HashServiceTests
{
    /// <summary>メソッド名で示す前提、操作、期待結果を一つの振る舞いとして検証</summary>
    [Fact]
    public void Compute_ReturnsLowercaseSha256Hex()
    {
        var service = new Sha256HashService();

        var result = service.Compute("abc");

        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            result);
    }
}
