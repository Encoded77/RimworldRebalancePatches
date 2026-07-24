using System;
using System.Collections.Generic;
using UnityEngine;

namespace RebalancePatches.Tests
{
    internal static class HarnessLogTap
    {
        private const int MaxEntries = 400;

        private const int MaxTextChars = 2000;
        private const int MaxFrames = 6;

        private static readonly object gate = new object();
        private static readonly List<Entry> ordered = new List<Entry>();
        private static readonly Dictionary<string, Entry> byKey = new Dictionary<string, Entry>();
        private static bool registered;
        private static bool truncated;

        public static bool Truncated
        {
            get { lock (gate) { return truncated; } }
        }

        /// <summary>A copy, so the report can be written while the tap is still receiving.</summary>
        public static List<Entry> Entries
        {
            get { lock (gate) { return new List<Entry>(ordered); } }
        }

        internal sealed class Entry
        {
            public string level;
            public string text;
            public List<string> frames = new List<string>();
            public int count = 1;
        }

        public static void Register()
        {
            if (registered)
                return;
            registered = true;
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        private static void OnLogMessage(string text, string stackTrace, LogType type)
        {
            string level = LevelOf(type);
            if (level == null)
                return;
            try
            {
                Record(level, text ?? "", stackTrace);
            }
            catch
            {
            }
        }

        private static string LevelOf(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return "error";
                case LogType.Exception: return "exception";
                case LogType.Assert: return "assert";
                case LogType.Warning: return "warning";
                default: return null;     // LogType.Log - ordinary messages are not worth carrying
            }
        }

        private static void Record(string level, string text, string stackTrace)
        {
            if (text.Length > MaxTextChars)
                text = text.Substring(0, MaxTextChars) + " ...[truncated]";
            string key = level + " " + text;
            lock (gate)
            {
                RecordLocked(key, level, text, stackTrace);
            }
        }

        private static void RecordLocked(string key, string level, string text, string stackTrace)
        {
            if (byKey.TryGetValue(key, out Entry existing))
            {
                existing.count++;
                return;
            }
            if (ordered.Count >= MaxEntries)
            {
                truncated = true;
                return;
            }

            var entry = new Entry { level = level, text = text };
            if (level != "warning" && !string.IsNullOrEmpty(stackTrace))
            {
                string[] lines = stackTrace.Split('\n');
                for (int i = 0; i < lines.Length && entry.frames.Count < MaxFrames; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length > 0)
                        entry.frames.Add(line);
                }
            }
            byKey[key] = entry;
            ordered.Add(entry);
        }
    }
}
