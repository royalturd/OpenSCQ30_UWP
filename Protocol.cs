using System;
using System.Collections.Generic;

namespace SCQ30Mobile
{
    // Layouts for model A3959 (Soundcore P30i / R50i NC), taken from Oppzippy/OpenSCQ30 (GPL-3.0).
    // Offsets are within the state-update body, after the 9-byte header and before the checksum.
    public class DualDevice
    {
        public bool Connected;
        public byte[] Mac = new byte[6];
        public string Name;
        public string MacText { get { return BitConverter.ToString(Mac).Replace('-', ':'); } }
    }

    public class DeviceState
    {
        public byte HostDevice;
        public bool TwsConnected;
        public int BatteryLeft, BatteryRight;
        public bool ChargingLeft, ChargingRight;
        public string FirmwareLeft, FirmwareRight, Serial;
        public ushort EqPresetId;
        public int[] EqBands = new int[10];            // tenths of a dB
        public byte[] Buttons = new byte[8];           // high nibble: TWS off, low nibble: TWS on, 0xF: disabled
        public byte AmbientCycle;                      // bit0 NC, bit1 transparency, bit2 normal
        public byte[] SoundModes = new byte[7];
        public bool TouchTone, DualConnections, Surround, AutoPowerOff, LowBatteryPrompt, GamingMode;
        public int AutoPowerOffIndex;
        public bool GamingModeSupported;

        public int AmbientMode { get { return SoundModes[0]; } }       // 0 NC, 1 transparency, 2 normal
        public int NcMode { get { return SoundModes[3]; } }            // 0 manual, 1 adaptive, 2 multi-scene
        public int ManualLevel { get { return SoundModes[1] >> 4; } }
        public int AdaptiveLevel { get { return SoundModes[1] & 0x0F; } }
        public bool WindSuppression { get { return (SoundModes[4] & 1) != 0; } }
        public bool WindDetected { get { return (SoundModes[4] & 2) != 0; } }
        public int Sensitivity { get { return SoundModes[5]; } }
        public int MultiScene { get { return SoundModes[6]; } }
    }

    public class EqPreset
    {
        public ushort Id;
        public string Name;
        public int[] Bands;
    }

    public static class Protocol
    {
        public const ushort CustomEqId = 0xFEFE;
        public static readonly int[] BandHz = { 100, 200, 400, 800, 1600, 3200, 6400, 12800 };

        public static readonly string[] AmbientModes = { "Noise canceling", "Transparency", "Normal" };
        public static readonly string[] NcModes = { "Manual", "Adaptive", "Multi-scene" };
        public static readonly string[] Scenes = { "Transport", "Outdoor", "Indoor" };
        public static readonly string[] AutoOffNames = { "10 min", "20 min", "30 min", "60 min" };
        public static readonly string[] ButtonActions =
        {
            "Volume up", "Volume down", "Previous song", "Next song", "Ambient sound mode", "Voice assistant", "Play / pause"
        };
        public static readonly byte[] ButtonActionIds = { 0, 1, 2, 3, 4, 5, 6 };

        // Order matches the state-update layout. Ids: single = 2, double = 0, triple = 5, long = 1.
        public static readonly string[] ButtonNames =
        {
            "Left single", "Right single", "Left double", "Right double",
            "Left triple", "Right triple", "Left long", "Right long"
        };
        public static readonly byte[] ButtonIds = { 2, 2, 0, 0, 5, 5, 1, 1 };
        public static readonly byte[] ButtonSides = { 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly EqPreset[] EqPresets = MakePresets();

        static EqPreset P(ushort id, string name, params int[] b)
        {
            return new EqPreset { Id = id, Name = name, Bands = b };
        }

        static EqPreset[] MakePresets()
        {
            return new[]
            {
                P(0, "Soundcore Signature", 0, 0, 0, 0, 0, 0, 0, 0, 0, -120),
                P(1, "Acoustic", 40, 10, 20, 20, 40, 40, 40, 20, 0, -120),
                P(2, "Bass Booster", 40, 30, 10, 0, 0, 0, 0, 0, 0, -120),
                P(3, "Bass Reducer", -40, -30, -10, 0, 0, 0, 0, 0, 0, -120),
                P(4, "Classical", 30, 30, -20, -20, 0, 20, 30, 40, 0, -120),
                P(5, "Podcast", -30, 20, 40, 40, 30, 20, 0, -20, 0, -120),
                P(6, "Dance", 20, -30, -10, 10, 20, 20, 10, -30, 0, -120),
                P(7, "Deep", 20, 10, 30, 30, 20, -20, -40, -50, 0, -120),
                P(8, "Electronic", 30, 20, -20, 20, 10, 20, 30, 30, 0, -120),
                P(9, "Flat", -20, -20, -10, 0, 0, 0, -20, -20, 0, -120),
                P(10, "Hip-Hop", 20, 30, -10, -10, 20, -10, 20, 30, 0, -120),
                P(11, "Jazz", 20, 20, -20, -20, 0, 20, 30, 40, 0, -120),
                P(12, "Latin", 0, 0, -20, -20, -20, 0, 30, 50, 0, -120),
                P(13, "Lounge", -10, 20, 40, 30, 0, -20, 20, 10, 0, -120),
                P(14, "Piano", 0, 30, 30, 20, 40, 50, 30, 40, 0, -120),
                P(15, "Pop", -10, 10, 30, 30, 10, -10, -20, -30, 0, -120),
                P(16, "R&B", 60, 20, -20, -20, 20, 30, 30, 40, 0, -120),
                P(17, "Rock", 30, 20, -10, -10, 10, 30, 40, 50, 0, -120),
                P(18, "Small speakers", 40, 30, 10, 0, -20, -30, -40, -40, 0, -120),
                P(19, "Spoken word", -30, -20, 10, 20, 20, 10, 0, -30, 0, -120),
                P(20, "Treble Booster", -20, -20, -20, -10, 10, 20, 20, 40, 0, -120),
                P(21, "Treble Reducer", 0, 0, 0, -20, -30, -40, -40, -60, 0, -120)
            };
        }

        static string Ascii(byte[] b, int start, int count)
        {
            var c = new char[count];
            for (int i = 0; i < count; i++) c[i] = (char)b[start + i];
            return new string(c);
        }

        static bool FirmwareAtLeast(string fw, int major, int minor)
        {
            // "01.64"
            int dot = fw.IndexOf('.');
            int a, c;
            if (dot < 0 || !int.TryParse(fw.Substring(0, dot), out a) || !int.TryParse(fw.Substring(dot + 1), out c)) return false;
            return a > major || (a == major && c >= minor);
        }

        public static DeviceState ParseState(byte[] b)
        {
            if (b.Length < 79) return null;
            var s = new DeviceState();
            s.HostDevice = b[0];
            s.TwsConnected = b[1] != 0;
            s.BatteryLeft = b[2];
            s.BatteryRight = b[3];
            s.ChargingLeft = b[4] == 1;
            s.ChargingRight = b[5] == 1;
            s.FirmwareLeft = Ascii(b, 6, 5);
            s.FirmwareRight = Ascii(b, 11, 5);
            s.Serial = Ascii(b, 16, 16);
            s.EqPresetId = (ushort)(b[32] | (b[33] << 8));
            for (int i = 0; i < 10; i++) s.EqBands[i] = b[34 + i] - 120;
            Array.Copy(b, 55, s.Buttons, 0, 8);
            s.AmbientCycle = b[63];
            Array.Copy(b, 64, s.SoundModes, 0, 7);
            s.TouchTone = b[72] != 0;
            s.DualConnections = b[73] != 0;
            s.Surround = b[74] != 0;
            s.AutoPowerOff = b[75] != 0;
            s.AutoPowerOffIndex = b[76];
            s.LowBatteryPrompt = b[77] != 0;
            s.GamingMode = b[78] != 0;
            s.GamingModeSupported = FirmwareAtLeast(s.FirmwareLeft, 1, 60) && FirmwareAtLeast(s.FirmwareRight, 1, 60);
            return s;
        }

        // Inbound 0B 01: [total, index, (len, connected, mac[6], name[len-8])...]
        public static List<DualDevice> ParseDualDevices(byte[] b, out int total, out int index)
        {
            total = b.Length > 0 ? b[0] : 0;
            index = b.Length > 1 ? b[1] : 0;
            var list = new List<DualDevice>();
            int p = 2;
            while (p + 8 <= b.Length)
            {
                int len = b[p];
                var d = new DualDevice { Connected = b[p + 1] != 0 };
                Array.Copy(b, p + 2, d.Mac, 0, 6);
                int nameLen = Math.Min(Math.Max(len - 8, 0), b.Length - (p + 8));
                int end = 0;
                while (end < nameLen && b[p + 8 + end] != 0) end++;
                d.Name = Ascii(b, p + 8, end);
                list.Add(d);
                p += 8 + nameLen;
            }
            return list;
        }

        // ---- bodies ----

        const double Smaller = 0.85, Larger = 0.95;

        static readonly double[][] DrcCoefficients =
        {
            new[] { 1.26, -0.71 * Smaller, 0.177, -0.0494, 0.0345, -0.0197, 0.0075, -0.00217 },
            new[] { -0.71 * Smaller, 1.73 * Larger, 0.0, 0.204, -0.068, 0.045, -0.0235, 0.0075 },
            new[] { 0.177, -0.81 * Smaller, 1.73 * Larger, -0.81 * Smaller, 0.208, -0.07, 0.045, -0.0197 },
            new[] { -0.0494, 0.204, 0.0, 1.73 * Larger, -0.82 * Smaller, 0.208, -0.068, 0.0345 },
            new[] { 0.0345, -0.068, 0.208, -0.82 * Smaller, 1.73 * Larger, 0.0, 0.204, -0.0494 },
            new[] { -0.0197, 0.045, -0.07, 0.208, -0.81 * Smaller, 1.73 * Larger, -0.81 * Smaller, 0.177 },
            new[] { 0.0075, -0.0235, 0.045, -0.068, 0.204, 0.0, 1.83 * Larger, -0.71 * Smaller },
            new[] { -0.00217, 0.0075, -0.0197, 0.0345, -0.0494, 0.177, -0.71 * Smaller, 1.5 }
        };

        static int[] ApplyDrc(int[] tenths)
        {
            var adj = new double[10];
            for (int i = 0; i < 10; i++) adj[i] = tenths[i] / 10.0;
            double lows = adj[2] * 0.81 * Smaller;
            double highs = adj[5] * 0.81 * Smaller;
            var result = new int[10];
            for (int r = 0; r < 8; r++)
            {
                double acc = 0;
                for (int i = 0; i < 8; i++)
                {
                    if ((r == 1 || r == 3) && i == 2) acc -= lows;
                    else if ((r == 4 || r == 6) && i == 5) acc -= highs;
                    else acc += adj[i] * DrcCoefficients[r][i];
                }
                result[r] = (int)Math.Round(acc / 10.0 * 10.0, MidpointRounding.AwayFromZero);
            }
            result[8] = 0;
            result[9] = -120;
            return result;
        }

        static byte Eq(int tenths)
        {
            int v = tenths + 120;
            return (byte)Math.Max(0, Math.Min(255, v));
        }

        // Command 02 83: preset id (LE), 10 bands, 10 DRC bands.
        public static byte[] EqBody(ushort presetId, int[] bands10)
        {
            var drc = ApplyDrc(bands10);
            var body = new byte[22];
            body[0] = (byte)(presetId & 0xFF);
            body[1] = (byte)(presetId >> 8);
            for (int i = 0; i < 10; i++) body[2 + i] = Eq(bands10[i]);
            for (int i = 0; i < 10; i++) body[12 + i] = Eq(drc[i]);
            return body;
        }

        public static int[] CustomBands10(int[] visible8)
        {
            var r = new int[10];
            Array.Copy(visible8, r, 8);
            return r; // bands 9 and 10 fixed at 0 dB
        }

        public static byte[] AutoPowerOffBody(bool enabled, int index)
        {
            return new[] { (byte)(enabled ? 1 : 0), (byte)index };
        }
    }
}
