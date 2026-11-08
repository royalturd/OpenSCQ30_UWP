using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace SCQ30Mobile
{
    // Small wrapper around LocalSettings so a storage failure never takes the app down.
    public static class Prefs
    {
        public static string GetString(string key)
        {
            try
            {
                object v;
                return ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out v) ? v as string : null;
            }
            catch (Exception) { return null; }
        }

        public static void SetString(string key, string value)
        {
            try { ApplicationData.Current.LocalSettings.Values[key] = value; }
            catch (Exception) { }
        }

        public static bool GetBool(string key, bool fallback)
        {
            var s = GetString(key);
            return s == null ? fallback : s == "1";
        }

        public static void SetBool(string key, bool value)
        {
            SetString(key, value ? "1" : "0");
        }

        public static int GetInt(string key, int fallback)
        {
            int v;
            return int.TryParse(GetString(key), out v) ? v : fallback;
        }
    }

    // Short generated tones: a high beep when a change was accepted, a low one when it failed.
    public static class Beep
    {
        static readonly byte[] Ok = MakeWav(880, 90);
        static readonly byte[] Fail = MakeWav(300, 220);

        public static async Task PlayAsync(MediaElement player, bool ok)
        {
            try
            {
                var stream = new InMemoryRandomAccessStream();
                var w = new DataWriter(stream);
                w.WriteBytes(ok ? Ok : Fail);
                await w.StoreAsync();
                stream.Seek(0);
                player.SetSource(stream, "audio/wav");
                player.Play();
            }
            catch (Exception) { }
        }

        static byte[] MakeWav(double hz, int ms)
        {
            const int rate = 22050;
            int n = rate * ms / 1000;
            int dataLen = n * 2;
            var wav = new byte[44 + dataLen];

            Action<int, string> tag = (pos, s) => { for (int i = 0; i < 4; i++) wav[pos + i] = (byte)s[i]; };
            Action<int, int> i32 = (pos, v) => { for (int i = 0; i < 4; i++) wav[pos + i] = (byte)(v >> (8 * i)); };
            Action<int, int> i16 = (pos, v) => { wav[pos] = (byte)v; wav[pos + 1] = (byte)(v >> 8); };

            tag(0, "RIFF"); i32(4, 36 + dataLen); tag(8, "WAVE");
            tag(12, "fmt "); i32(16, 16); i16(20, 1); i16(22, 1); i32(24, rate); i32(28, rate * 2); i16(32, 2); i16(34, 16);
            tag(36, "data"); i32(40, dataLen);

            double fade = rate * 0.01;
            for (int i = 0; i < n; i++)
            {
                double env = Math.Min(1.0, Math.Min(i, n - i) / fade);
                short v = (short)(Math.Sin(2 * Math.PI * hz * i / rate) * 0.4 * short.MaxValue * env);
                i16(44 + i * 2, v);
            }
            return wav;
        }
    }
}
