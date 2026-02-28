using System.Text;

namespace PreflightApi.Infrastructure.Utilities;

public static class CursorHelper
{
    public static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static string Encode(int value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString()));
    }

    public static string Encode(Guid value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString()));
    }

    public static string? DecodeString(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static Guid? DecodeGuid(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return Guid.TryParse(decoded, out var value) ? value : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static int? DecodeInt(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(decoded, out var value) ? value : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
