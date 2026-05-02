using System.Security.Cryptography;
using System.Text;

namespace PreflightApi.Tools.ApiKeyMigration;

internal static class KeyHashing
{
    private const string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    public const string PfaPrefix = "pfa_sk_";

    public static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string GeneratePfaKey()
    {
        const int randomLength = 32;
        Span<byte> buf = stackalloc byte[randomLength];
        RandomNumberGenerator.Fill(buf);

        var sb = new StringBuilder(PfaPrefix, PfaPrefix.Length + randomLength);
        for (int i = 0; i < randomLength; i++)
            sb.Append(Base62Chars[buf[i] % Base62Chars.Length]);

        return sb.ToString();
    }
}
