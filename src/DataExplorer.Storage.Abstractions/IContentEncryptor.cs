#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable


namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Performs encryption and decryption of content payloads.
/// </summary>
/// <remarks>
/// Implementations may wrap cryptographic libraries. Errors during encryption or decryption should result in exceptions.
/// </remarks>
public interface IContentEncryptor
{
    /// <summary>
    /// Gets a value indicating whether content is actually encrypted.
    /// </summary>
    /// <remarks>
    /// If <c>false</c>, the encryptor acts as a pass-through.
    /// </remarks>
    bool IsEncrypted { get; }

    /// <summary>
    /// Encrypts the specified plain text.
    /// </summary>
    /// <param name="plain">The plain text bytes.</param>
    /// <param name="aad">Additional authenticated data that will not be encrypted.</param>
    /// <param name="nonce">Outputs the generated nonce.</param>
    /// <param name="tag">Outputs the authentication tag.</param>
    /// <returns>The encrypted bytes.</returns>
    /// <remarks>
    /// Throws if encryption fails or the inputs are invalid.
    /// </remarks>
    byte[] Encrypt(ReadOnlySpan<byte> plain, ReadOnlySpan<byte> aad, out byte[] nonce, out byte[] tag);

    /// <summary>
    /// Decrypts the provided cipher text.
    /// </summary>
    /// <param name="cipher">The encrypted bytes.</param>
    /// <param name="aad">The associated data used during encryption.</param>
    /// <param name="nonce">The nonce that was generated during encryption.</param>
    /// <param name="tag">The authentication tag produced during encryption.</param>
    /// <returns>The decrypted bytes.</returns>
    /// <remarks>
    /// Throws if authentication fails or the inputs are invalid.
    /// </remarks>
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
