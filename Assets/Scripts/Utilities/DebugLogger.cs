using UnityEngine;

namespace HorrorGame
{
    public static class DebugLogger
    {
        public static bool IsDebugEnabled = true;

        public static void Log(string message)
        {
            if (IsDebugEnabled)
            {
                Debug.Log(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (IsDebugEnabled)
            {
                Debug.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (IsDebugEnabled)
            {
                Debug.LogError(message);
            }
        }
    }
}