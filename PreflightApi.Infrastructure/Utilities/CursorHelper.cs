using System.Text;

namespace PreflightApi.Infrastructure.Utilities;

public enum CursorDirection { Forward, Backward }

public record DecodedCursor<T>(T Value, CursorDirection Direction);

public static class CursorHelper
{
    private const string ForwardPrefix = "f:";
    private const string BackwardPrefix = "b:";

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

    public static string EncodeNext(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(ForwardPrefix + value));
    }

    public static string EncodeNext(int value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(ForwardPrefix + value));
    }

    public static string EncodeNext(Guid value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(ForwardPrefix + value));
    }

    public static string EncodePrevious(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(BackwardPrefix + value));
    }

    public static string EncodePrevious(int value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(BackwardPrefix + value));
    }

    public static string EncodePrevious(Guid value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(BackwardPrefix + value));
    }

    public static string? DecodeString(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            // Strip directional prefix if present (for backward compat with callers using old Decode methods)
            if (decoded.StartsWith(ForwardPrefix) || decoded.StartsWith(BackwardPrefix))
                return decoded[2..];
            return decoded;
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
            if (decoded.StartsWith(ForwardPrefix) || decoded.StartsWith(BackwardPrefix))
                decoded = decoded[2..];
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
            if (decoded.StartsWith(ForwardPrefix) || decoded.StartsWith(BackwardPrefix))
                decoded = decoded[2..];
            return int.TryParse(decoded, out var value) ? value : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static DecodedCursor<string>? DecodeStringWithDirection(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var (value, direction) = ExtractDirection(decoded);
            return new DecodedCursor<string>(value, direction);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static DecodedCursor<Guid>? DecodeGuidWithDirection(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var (value, direction) = ExtractDirection(decoded);
            return Guid.TryParse(value, out var guid) ? new DecodedCursor<Guid>(guid, direction) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static DecodedCursor<int>? DecodeIntWithDirection(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var (value, direction) = ExtractDirection(decoded);
            return int.TryParse(value, out var intVal) ? new DecodedCursor<int>(intVal, direction) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static (string Value, CursorDirection Direction) ExtractDirection(string decoded)
    {
        if (decoded.StartsWith(ForwardPrefix))
            return (decoded[2..], CursorDirection.Forward);
        if (decoded.StartsWith(BackwardPrefix))
            return (decoded[2..], CursorDirection.Backward);
        // Legacy cursors without prefix default to forward
        return (decoded, CursorDirection.Forward);
    }
}
