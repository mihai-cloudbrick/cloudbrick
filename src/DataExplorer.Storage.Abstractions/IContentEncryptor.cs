#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable


namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public interface IContentEncryptor
{
    bool IsEncrypted { get; }
    byte[] Encrypt(ReadOnlySpan<byte> plain, ReadOnlySpan<byte> aad, out byte[] nonce, out byte[] tag);
    byte[] Decrypt(ReadOnlySpan<byte> cipher, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag);
}
public sealed class NoopEncryptor : IContentEncryptor
{
    public static readonly NoopEncryptor Instance = new();
    public bool IsEncrypted => false;
    public byte[] Encrypt(ReadOnlySpan<byte> plain, ReadOnlySpan<byte> aad, out byte[] nonce, out byte[] tag)
    { nonce = Array.Empty<byte>(); tag = Array.Empty<byte>(); return plain.ToArray(); }
    public byte[] Decrypt(ReadOnlySpan<byte> cipher, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag)
        => cipher.ToArray();
}
