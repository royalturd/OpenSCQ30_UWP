using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SCQ30Mobile
{
    public class DeviceEntry
    {
        public DeviceInformation Info;
        // Every RFCOMM service found for this device, in the order they are tried.
        public readonly List<DeviceInformation> Services = new List<DeviceInformation>();
        public string Name { get { return Info.Name; } }
        public string Kind;
        public override string ToString() { return Info.Name; }
    }

    public sealed class SoundcoreClient : IDisposable
    {
        public static readonly Guid VendorUuid = new Guid("0cf12d31-fac3-4553-bd80-d6832e700000");
        static readonly byte[] OutHeader = { 0x08, 0xEE, 0x00, 0x00, 0x00 };
        static readonly byte[] InHeader = { 0x09, 0xFF, 0x00, 0x00, 0x01 };

        public event Action<DeviceState> StateChanged;
        public event Action<List<DualDevice>> DualDevicesChanged;
        public DeviceState State { get; private set; }
        // Id of the RFCOMM service that answered, saved for reconnecting on the next start.
        public string ConnectedId { get; private set; }

        StreamSocket socket;
        DataWriter writer;
        DataReader reader;
        RfcommDeviceService service;
        bool running;
        int rxCount;
        readonly System.Threading.SemaphoreSlim sendLock = new System.Threading.SemaphoreSlim(1, 1);
        readonly List<DualDevice> dualBuffer = new List<DualDevice>();

        public static async Task<DeviceEntry> FindByIdAsync(string id)
        {
            try
            {
                var info = await DeviceInformation.CreateFromIdAsync(id);
                return info == null ? null : new DeviceEntry { Info = info, Kind = "paired" };
            }
            catch (Exception ex)
            {
                Trace("saved device lookup failed: " + ex.Message);
                return null;
            }
        }

        // Windows Phone 8.1 has no BluetoothDevice class, so devices are found through their RFCOMM
        // services. Each UUID must also be listed in Package.appxmanifest. A device can expose more
        // than one of them and only one answers, so ConnectAsync tries them in this order:
        // Soundcore vendor services first, the same preference OpenSCQ30 has, then plain SPP.
        static readonly Guid[] ServiceUuids =
        {
            new Guid("0cf12d31-fac3-4553-bd80-d6832e7b3959"),
            new Guid("0cf12d31-fac3-4553-bd80-d6832e7b3949"),
            VendorUuid,
            RfcommServiceId.SerialPort.Uuid,
        };

        public static async Task<List<DeviceEntry>> FindDevicesAsync()
        {
            var list = new List<DeviceEntry>();
            var byName = new Dictionary<string, DeviceEntry>();
            foreach (var uuid in ServiceUuids)
            {
                var selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(uuid));
                var found = await DeviceInformation.FindAllAsync(selector);
                foreach (var d in found)
                {
                    Trace("found " + d.Name + " service " + uuid + " " + d.Id);
                    DeviceEntry entry;
                    if (!byName.TryGetValue(d.Name, out entry))
                    {
                        entry = new DeviceEntry { Info = d, Kind = "paired" };
                        byName[d.Name] = entry;
                        list.Add(entry);
                    }
                    entry.Services.Add(d);
                }
            }
            return list;
        }

        // Tries each service the device has until one answers the state request.
        public async Task ConnectAsync(DeviceEntry entry)
        {
            var candidates = entry.Services.Count > 0 ? entry.Services : new List<DeviceInformation> { entry.Info };
            Trace("connect " + entry + ", " + candidates.Count + " service(s) to try");
            foreach (var info in candidates)
            {
                try
                {
                    if (await TryServiceAsync(info))
                    {
                        ConnectedId = info.Id;
                        return;
                    }
                    Trace("no response in 3s from " + service.ServiceId.Uuid + ", trying the next service");
                }
                catch (Exception ex) { Trace("service failed: " + ex.Message); }
                Close();
            }
            throw new InvalidOperationException("No service answered: wrong service or protocol (see log)");
        }

        async Task<bool> TryServiceAsync(DeviceInformation info)
        {
            service = await RfcommDeviceService.FromIdAsync(info.Id);
            if (service == null) throw new InvalidOperationException("RFCOMM service not available (is the device on and paired?)");
            Trace("using " + service.ServiceId.Uuid);
            Trace("service " + service.ConnectionHostName + " " + service.ConnectionServiceName);

            socket = new StreamSocket();
            await socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName,
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            writer = new DataWriter(socket.OutputStream);
            reader = new DataReader(socket.InputStream);
            rxCount = 0;
            running = true;
            Trace("socket connected");
            var ignore = ReadLoopAsync();
            await RequestStateAsync();

            for (int i = 0; i < 30 && rxCount == 0; i++) await Task.Delay(100);
            return rxCount > 0;
        }

        public Task RequestStateAsync()
        {
            return SendAsync(0x01, 0x01, new byte[0]);
        }

        // ---- settings ----

        DeviceState Need()
        {
            if (State == null) throw new InvalidOperationException("No state yet");
            return State;
        }

        // Command 06 81: the 7 sound-mode bytes.
        public Task SetSoundModesAsync(int ambient, int ncMode, int manual, int adaptiveNibble, bool windSuppression, int sensitivity, int scene)
        {
            var cur = Need().SoundModes;
            var b = new byte[7];
            b[0] = (byte)ambient;
            b[1] = (byte)((manual << 4) | (adaptiveNibble & 0x0F));
            b[2] = (byte)ambient;
            b[3] = (byte)ncMode;
            b[4] = (byte)((windSuppression ? 1 : 0) | (cur[4] & 2));
            b[5] = (byte)sensitivity;
            b[6] = (byte)scene;
            return SendAsync(0x06, 0x81, b);
        }

        public Task SetTouchToneAsync(bool v) { return SendFlag(0x01, 0x83, v); }
        public Task SetGamingModeAsync(bool v) { return SendFlag(0x01, 0x87, v); }
        public Task SetSurroundAsync(bool v) { return SendFlag(0x02, 0x86, v); }
        public Task SetLowBatteryPromptAsync(bool v) { return SendFlag(0x10, 0x82, v); }

        Task SendFlag(byte c0, byte c1, bool v)
        {
            return SendAsync(c0, c1, new[] { (byte)(v ? 1 : 0) });
        }

        public Task SetAutoPowerOffAsync(bool enabled, int index)
        {
            return SendAsync(0x01, 0x86, Protocol.AutoPowerOffBody(enabled, index));
        }

        public Task SetAmbientCycleAsync(byte mask)
        {
            return SendAsync(0x06, 0x82, new[] { mask });
        }

        public Task SetEqualizerAsync(ushort presetId, int[] bands10)
        {
            return SendAsync(0x02, 0x83, Protocol.EqBody(presetId, bands10));
        }

        // Button action byte: high nibble = action when TWS is off, low nibble = when on. 0xF disables.
        public Task SetButtonAsync(int buttonIndex, byte actionByte)
        {
            return SendAsync(0x04, 0x81, new[] { Protocol.ButtonSides[buttonIndex], Protocol.ButtonIds[buttonIndex], actionByte });
        }

        public async Task ResetButtonsAsync()
        {
            await SendAsync(0x04, 0x82, new byte[0]);
            await RequestStateAsync();
        }

        public Task SetDualConnectionsAsync(bool enabled)
        {
            return SendFlag(0x0B, 0x84, enabled);
        }

        public Task RequestDualDevicesAsync()
        {
            return SendAsync(0x0B, 0x01, new byte[0]);
        }

        public Task DualConnectAsync(byte[] mac) { return SendAsync(0x0B, 0x82, mac); }
        public Task DualDisconnectAsync(byte[] mac) { return SendAsync(0x0B, 0x81, mac); }
        public Task DualForgetAsync(byte[] mac) { return SendAsync(0x0B, 0x83, mac); }

        // ---- transport ----

        async Task SendAsync(byte c0, byte c1, byte[] body)
        {
            int len = 5 + 2 + 2 + body.Length + 1;
            var p = new byte[len];
            Array.Copy(OutHeader, p, 5);
            p[5] = c0; p[6] = c1;
            p[7] = (byte)(len & 0xFF); p[8] = (byte)(len >> 8);
            Array.Copy(body, 0, p, 9, body.Length);
            byte sum = 0;
            for (int i = 0; i < len - 1; i++) sum += p[i];
            p[len - 1] = sum;

            await sendLock.WaitAsync();
            try
            {
                writer.WriteBytes(p);
                await writer.StoreAsync();
            }
            finally { sendLock.Release(); }
            Trace("TX " + BitConverter.ToString(p));
        }

        async Task<byte[]> ReadExactAsync(int n)
        {
            uint got = await reader.LoadAsync((uint)n);
            if (got < n) throw new System.IO.IOException("socket closed");
            var buf = new byte[n];
            reader.ReadBytes(buf);
            return buf;
        }

        async Task ReadLoopAsync()
        {
            try
            {
                while (running)
                {
                    var hdr = await ReadExactAsync(9);
                    rxCount++;
                    Trace("RX hdr " + BitConverter.ToString(hdr));
                    bool ok = true;
                    for (int i = 0; i < 5; i++) if (hdr[i] != InHeader[i]) ok = false;
                    int len = hdr[7] | (hdr[8] << 8);
                    if (!ok || len < 10 || len > 2048) { Trace("bad header " + BitConverter.ToString(hdr)); continue; }

                    var rest = await ReadExactAsync(len - 9);
                    var body = new byte[rest.Length - 1];
                    Array.Copy(rest, body, body.Length);
                    Trace("RX cmd " + hdr[5].ToString("X2") + " " + hdr[6].ToString("X2") + " len " + len);

                    if (hdr[5] == 0x01 && hdr[6] == 0x01) HandleState(body);
                    else if (hdr[5] == 0x0B && hdr[6] == 0x01) HandleDual(body);
                }
            }
            catch (Exception ex)
            {
                if (running) Trace("read stopped: " + ex.Message);
            }
        }

        void HandleState(byte[] body)
        {
            var s = Protocol.ParseState(body);
            if (s == null) { Trace("state too short: " + body.Length); return; }
            State = s;
            var h = StateChanged;
            if (h != null) h(s);
            if (s.DualConnections) { var ignore = RequestDualDevicesAsync(); }
        }

        void HandleDual(byte[] body)
        {
            int total, index;
            var devices = Protocol.ParseDualDevices(body, out total, out index);
            if (index <= 1) dualBuffer.Clear();
            dualBuffer.AddRange(devices);
            if (index >= total)
            {
                var h = DualDevicesChanged;
                if (h != null) h(new List<DualDevice>(dualBuffer));
            }
        }

        static void Trace(string m)
        {
            AppLog.Add(m);
        }

        public void Dispose()
        {
            Close();
        }

        void Close()
        {
            running = false;
            if (reader != null) { reader.Dispose(); reader = null; }
            if (writer != null) { writer.Dispose(); writer = null; }
            if (socket != null) { socket.Dispose(); socket = null; }
            // RfcommDeviceService is not disposable on Windows Phone 8.1; closing the socket is enough.
            service = null;
        }
    }
}
