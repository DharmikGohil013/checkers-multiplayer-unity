using UnityEngine;
namespace Checkers.Utilities
{
    public static class GameLogger
    {
        public enum LogLevel
        {
            INFO,
            WARN,
            ERROR,
            DESYNC
        }
        public static bool EnableLogging = true;
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(LogLevel level, string message, Object context = null)
        {
            if (!EnableLogging)
                return;
            string prefix;
            string colorTag;
            switch (level)
            {
                case LogLevel.INFO:
                    prefix = "[INFO]";
                    colorTag = "#FFFFFF";
                    break;
                case LogLevel.WARN:
                    prefix = "[WARN]";
                    colorTag = "#FFA500";
                    break;
                case LogLevel.ERROR:
                    prefix = "[ERROR]";
                    colorTag = "#FF4444";
                    break;
                case LogLevel.DESYNC:
                    prefix = "[DESYNC]";
                    colorTag = "#FF00FF";
                    break;
                default:
                    prefix = "[LOG]";
                    colorTag = "#CCCCCC";
                    break;
            }
            string formattedMessage = $"<color={colorTag}>{prefix} [Checkers] {message}</color>";
            switch (level)
            {
                case LogLevel.INFO:
                    Debug.Log(formattedMessage, context);
                    break;
                case LogLevel.WARN:
                    Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.ERROR:
                case LogLevel.DESYNC:
                    Debug.LogError(formattedMessage, context);
                    break;
            }
        }
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogDesync(string expected, string actual)
        {
            if (!EnableLogging)
                return;
            string message = $"DESYNC DETECTED!\n  Expected: {expected}\n  Actual:   {actual}";
            Log(LogLevel.DESYNC, message);
        }
    }
}
