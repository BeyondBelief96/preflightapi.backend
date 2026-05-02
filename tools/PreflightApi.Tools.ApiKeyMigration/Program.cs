using PreflightApi.Tools.ApiKeyMigration;

if (args.Length == 0)
{
    Usage.Print();
    return 1;
}

try
{
    return args[0] switch
    {
        "migrate" => await MigrateCommand.RunAsync(OptionParser.Parse(args[1..])),
        "seed"    => await SeedCommand.RunAsync(OptionParser.Parse(args[1..])),
        "--help" or "-h" or "help" => RunUsage(),
        _ => UnknownCommand(args[0])
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}

static int RunUsage()
{
    Usage.Print();
    return 0;
}

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"unknown command: {cmd}");
    Usage.Print();
    return 1;
}
