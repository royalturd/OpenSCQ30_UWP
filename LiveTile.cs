using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SCQ30Mobile
{
    // Pushes the connected device, battery and sound mode to the Start tile (medium, wide, large).
    public static class LiveTile
    {
        public static void Update(DeviceState s, string deviceName)
        {
            try
            {
                string name = string.IsNullOrEmpty(deviceName) ? s.Profile.Name : deviceName;
                string battery = "L " + Percent(s.BatteryLeft) + (s.ChargingLeft ? "⚡" : "") +
                                 "  R " + Percent(s.BatteryRight) + (s.ChargingRight ? "⚡" : "");
                string mode = ModeText(s);
                string low = LowText(s);
                string warn = low == null ? "" : "⚠ " + low;

                // Windows Phone 8.1 has no adaptive tiles, so this uses the 8.1 text templates
                // (medium and wide; phones have no large tile).
                string xml =
                    "<tile><visual version='2' branding='name'>" +
                    Binding("TileSquare150x150Text01", name, battery, mode, warn) +
                    Binding("TileWide310x150Text01", name, battery, mode, warn) +
                    "</visual></tile>";

                var doc = new XmlDocument();
                doc.LoadXml(xml);
                TileUpdateManager.CreateTileUpdaterForApplication().Update(new TileNotification(doc));

                NotifyLow(s, name, low);
            }
            catch (Exception ex) { AppLog.Add("tile update failed: " + ex.Message); }
        }

        // Battery is 0 to 5, 255 and similar mean "no earbud". Low is 20% or less while not charging.
        const int LowLevel = 1;

        static bool IsLow(int level, bool charging)
        {
            return level >= 0 && level <= LowLevel && !charging;
        }

        static string LowText(DeviceState s)
        {
            bool left = IsLow(s.BatteryLeft, s.ChargingLeft);
            bool right = IsLow(s.BatteryRight, s.ChargingRight);
            if (left && right) return "low battery: both earbuds";
            if (left) return "low battery: left earbud";
            if (right) return "low battery: right earbud";
            return null;
        }

        static string lastNotified;

        // One toast per low episode. It re-arms once the battery recovers or charges.
        static void NotifyLow(DeviceState s, string name, string low)
        {
            if (low == null) { lastNotified = null; return; }
            if (low == lastNotified) return;
            lastNotified = low;

            string xml =
                "<toast><visual><binding template='ToastText02'>" +
                "<text id='1'>Low battery</text>" +
                "<text id='2'>" + Esc(name) + ": " + Esc(low.Substring("low battery: ".Length)) + "</text>" +
                "</binding></visual></toast>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var toast = new ToastNotification(doc) { Tag = "lowbattery", Group = "battery" };
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void Clear()
        {
            try { TileUpdateManager.CreateTileUpdaterForApplication().Clear(); }
            catch (Exception) { }
        }

        // Battery is reported as 0 to 5 steps by the earbuds.
        static string Percent(int level)
        {
            return level < 0 || level > 5 ? "/" : level * 20 + "%";
        }

        static string ModeText(DeviceState s)
        {
            if (!s.Profile.SoundModes) return s.TwsConnected ? "earbuds paired" : "single earbud";
            string[] ambient = Protocol.AmbientModes;
            string m = s.AmbientMode >= 0 && s.AmbientMode < ambient.Length ? ambient[s.AmbientMode] : "?";
            if (s.AmbientMode == 0 && s.NcMode >= 0 && s.NcMode < Protocol.NcModes.Length)
                m += " · " + Protocol.NcModes[s.NcMode].ToLowerInvariant();
            return m;
        }

        // Text01 templates: one heading line (id 1) and body lines (id 2 and up).
        static string Binding(string template, string name, string battery, string mode, string warn)
        {
            return "<binding template='" + template + "'>" +
                   "<text id='1'>" + Esc(name) + "</text>" +
                   "<text id='2'>" + Esc(battery) + "</text>" +
                   "<text id='3'>" + Esc(mode) + "</text>" +
                   "<text id='4'>" + Esc(warn) + "</text>" +
                   "</binding>";
        }

        static string Esc(string t)
        {
            return t.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;");
        }
    }
}
