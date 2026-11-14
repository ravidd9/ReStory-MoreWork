using BepInEx.Logging;

namespace ReStoryMoreWork;

internal static class Log
{
    internal static ManualLogSource Source;

    internal static void Info(string message)
    {
        Source.LogInfo(message);
    }

    internal static void Warning(string message)
    {
        Source.LogWarning(message);
    }

    internal static void Error(string message)
    {
        Source.LogError(message);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    internal static void Debug(string message)
    {
        Info(message);
    }
}
