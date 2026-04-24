using UnityEngine;

namespace Checkers.Utilities
{
    /// <summary>
    /// Custom logger that wraps Debug.Log with log levels and color-coded output.
    /// Compiled out in release builds to eliminate logging overhead.
    /// </summary>
    public static class GameLogger
    {
        /// <summary>
        /// Log level enumeration for filtering and color-coding.
        /// </summary>
        public enum LogLevel
        {
            INFO,
            WARN,
            ERROR,
            DESYNC
        }

        /// <summary>
        /// Master toggle for enabling/disabling all logging at runtime.
        /// </summary>
        public static bool EnableLogging = true;

        /// <summary>
        /// Logs a message with the specified log level.
        /// Color-coded in Editor and Development builds:
        ///   INFO  = White
        ///   WARN  = Yellow/Orange
        ///   ERROR = Red
        ///   DESYNC = Magenta
        /// Compiled out entirely in release builds.
        /// </summary>
        /// <param name="level">Severity level.</param>
        /// <param name="message">Log message.</param>
        /// <param name="context">Optional Unity object context for click-to-focus in console.</param>
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

        /// <summary>
        /// Logs a desync error comparing expected and actual state values.
        /// </summary>
        /// <param name="expected">Description of the expected state.</param>
        /// <param name="actual">Description of the actual state.</param>
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
