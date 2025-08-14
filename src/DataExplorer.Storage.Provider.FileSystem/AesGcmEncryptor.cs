#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Security.Cryptography;
namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
internal sealed class AesGcmEncryptor : IContentEncryptor
{
    private readonly byte[] _key;
    public AesGcmEncryptor(byte[] key) { if (key is null || key.Length != 32) throw new ArgumentException("AES-GCM requires 32-byte key."); _key = key; }
    public bool IsEncrypted => true;
    public byte[] Encrypt(ReadOnlySpan<byte> plain, ReadOnlySpan<byte> aad, out byte[] nonce, out byte[] tag)
    {
        nonce = RandomNumberGenerator.GetBytes(12); tag = new byte[16]; var cipher = new byte[plain.Length];
        using var gcm = new AesGcm(_key); gcm.Encrypt(nonce, plain, cipher, tag, aad); return cipher;
    }
    public byte[] Decrypt(ReadOnlySpan<byte> cipher, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag)
    {
        var plain = new byte[cipher.Length]; using var gcm = new AesGcm(_key); gcm.Decrypt(nonce, cipher, tag, plain, aad); return plain;
    }
}
