using System;
using System.Collections.Generic;
using Windows.System.Profile;

namespace SCQ30Mobile
{
    public static class AppLog
    {
        const int MaxLines = 3000;
        static readonly object Gate = new object();
        static readonly List<string> Lines = new List<string>();

        public static event Action Changed;

        public static void Add(string message)
        {
            lock (Gate)
            {
                Lines.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " " + message);
                if (Lines.Count > MaxLines) Lines.RemoveAt(0);
            }
            var h = Changed;
            if (h != null) h();
        }

        public static string Text
        {
            get { lock (Gate) { return string.Join("\n", Lines); } }
        }

        // Header plus log, used for copy and export.
        public static string Report
        {
            get
            {
                string os = "?";
                try
                {
                    ulong v = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
                    os = ((v >> 48) & 0xFFFF) + "." + ((v >> 32) & 0xFFFF) + "." + ((v >> 16) & 0xFFFF) + "." + (v & 0xFFFF);
                }
                catch (Exception) { }
                return "OpenSCQ30 log\nOS " + os + " (" + AnalyticsInfo.VersionInfo.DeviceFamily + ")\n"
                     + "Exported " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n" + Text + "\n";
            }
        }

        public static void Clear()
        {
            lock (Gate) { Lines.Clear(); }
            var h = Changed;
            if (h != null) h();
        }
    }
}
