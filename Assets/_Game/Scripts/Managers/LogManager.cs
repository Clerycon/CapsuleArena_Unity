using System;
using UnityEngine;

public static class LogManager
{
    public static bool IsEnabled { get; set; } = true;

    public static Func<string> ContextProvider { get; set; }
    private static string GetContext() => ContextProvider?.Invoke() ?? "Local";

    private static string BuildPrefix(object sender) => $"[{DateTime.Now.ToString("HH:mm:ss.fff")}][{sender?.GetType().Name ?? "Unknown"}][{GetContext()}] ";

    [HideInCallstack]
    public static void Log(object sender, string message)
    {
        if(!IsEnabled || !Debug.isDebugBuild) { return; }
        
        string prefix = BuildPrefix(sender);
        Debug.Log(prefix + message, sender as UnityEngine.Object);
    }

    [HideInCallstack]
    public static void LogWarning(object sender, string message)
    {
        if(!IsEnabled || !Debug.isDebugBuild) { return; }

        string prefix = BuildPrefix(sender);
        Debug.LogWarning(prefix + message, sender as UnityEngine.Object);
    }

    [HideInCallstack]
    public static void LogError(object sender, string message)
    {
        string prefix = BuildPrefix(sender);
        Debug.LogError(prefix + message, sender as UnityEngine.Object);
    }
}
