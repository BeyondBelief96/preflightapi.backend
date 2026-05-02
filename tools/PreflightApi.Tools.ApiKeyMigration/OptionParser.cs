namespace PreflightApi.Tools.ApiKeyMigration;

internal static class OptionParser
{
    public static Dictionary<string, string> Parse(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (!token.StartsWith("--"))
                throw new ArgumentException($"unexpected positional argument '{token}'");

            var key = token[2..];

            // Boolean flag (no value)
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
            {
                result[key] = "true";
                continue;
            }

            result[key] = args[++i];
        }
        return result;
    }

    public static string Required(this Dictionary<string, string> opts, string key)
    {
        if (!opts.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"--{key} is required");
        return value;
    }

    public static string? Optional(this Dictionary<string, string> opts, string key)
    {
        return opts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    public static bool Flag(this Dictionary<string, string> opts, string key)
    {
        return opts.TryGetValue(key, out var value) && value == "true";
    }
}
