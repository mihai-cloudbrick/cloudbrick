#nullable enable
using Cloudbrick;
using System.Security.Cryptography;
using System.Text;

namespace Cloudbrick.Orleans.Reminders.DataExplorer;
internal static class StorageIdHasher
{
    public static string Sha256Hex(string s){ using var sha=SHA256.Create(); var bytes=sha.ComputeHash(Encoding.UTF8.GetBytes(s)); return string.Concat(bytes.Select(b=>b.ToString("x2"))); }
}
