using System;
using UnityEngine;

namespace GliderStats
{
    static class Logging
    {
        private const string PREFIX = "[GliderStats] ";

        public static void Log(object message)
        {
            Debug.Log(PREFIX + message);
        }

        public static void Warn(object message)
        {
            Debug.LogWarning(PREFIX + message);
        }

        public static void Error(object message)
        {
            Debug.LogError(PREFIX + message);
        }

        public static void Exception(string message, Exception e)
        {
            Error(message + " (" + e.GetType().Name + ") " + e.Message + ": " + e.StackTrace);
        }

        public static void Exception(Exception e)
        {
            Error("(" + e.GetType().Name + ") " + e.Message + ": " + e.StackTrace);
        }

        public static string ToString(Part part)
        {
            return part.partInfo.title;
        }
    }
}