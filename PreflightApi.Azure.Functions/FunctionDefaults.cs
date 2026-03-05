namespace PreflightApi.Azure.Functions;

public static class FunctionDefaults
{
#if DEBUG
    public const bool RunOnStartup = true;
#else
    public const bool RunOnStartup = false;
#endif
}
