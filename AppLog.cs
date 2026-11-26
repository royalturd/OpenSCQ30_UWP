using System;
using System.Collections.Generic;
using Windows.Security.ExchangeActiveSyncProvisioning;

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
                // Windows Phone 8.1 does not expose the OS build number, only the OS and phone model.
                string os = "?";
                try
                {
                    var d = new EasClientDeviceInformation();
                    os = d.OperatingSystem + " on " + d.SystemManufacturer + " " + d.SystemProductName;
                }
                catch (Exception) { }
                return "OpenSCQ30 log\nOS " + os + "\n"
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
