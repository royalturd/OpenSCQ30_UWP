using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SCQ30Mobile
{
    public sealed partial class MainPage : Page
    {
        SoundcoreClient client;
        bool updating;

        readonly Slider[] eqSliders = new Slider[8];
        readonly TextBlock[] eqLabels = new TextBlock[8];
        readonly ComboBox[] buttonBoxes = new ComboBox[8];

        public MainPage()
        {
            InitializeComponent();
            LogText.Text = AppLog.Text;
            AppLog.Changed += () => Ui(() =>
            {
                LogText.Text = AppLog.Text;
                LogScroll.ChangeView(null, LogScroll.ScrollableHeight, null);
            });
            AppLog.Add("app started");
            BuildControls();
        }

        void BuildControls()
        {
            foreach (var s in Protocol.AmbientModes) AmbientBox.Items.Add(s);
            foreach (var s in Protocol.NcModes) NcModeBox.Items.Add(s);
            foreach (var s in Protocol.Scenes) SceneBox.Items.Add(s);
            foreach (var s in Protocol.AutoOffNames) ApoBox.Items.Add(s);

            EqPresetBox.Items.Add("Custom");
            foreach (var p in Protocol.EqPresets) EqPresetBox.Items.Add(p.Name);

            for (int i = 0; i < 8; i++)
            {
                var label = new TextBlock { Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.LightGray), FontSize = 14 };
                var slider = new Slider { Minimum = -120, Maximum = 120, StepFrequency = 5, TickFrequency = 60 };
                int idx = i;
                slider.ValueChanged += (s, e) => EqLabel(idx);
                eqLabels[i] = label;
                eqSliders[i] = slider;
                EqPanel.Children.Add(label);
                EqPanel.Children.Add(slider);
                EqLabel(i);
            }

            for (int i = 0; i < 8; i++)
            {
                var box = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Header = Protocol.ButtonNames[i] };
                foreach (var a in Protocol.ButtonActions) box.Items.Add(a);
                box.Items.Add("Disabled");
                int idx = i;
                box.SelectionChanged += (s, e) => ButtonChanged(idx);
                buttonBoxes[i] = box;
                ButtonsPanel.Children.Add(box);
            }
        }

        void EqLabel(int i)
        {
            eqLabels[i].Text = Protocol.BandHz[i] + " Hz   " + (eqSliders[i].Value / 10.0).ToString("0.0") + " dB";
        }

        void Ui(Action a)
        {
            var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => a());
        }

        static void Log(string m)
        {
            AppLog.Add(m);
        }

        // Runs a device command, logging failures instead of crashing the app.
        async void Run(string what, Func<Task> action)
        {
            if (client == null) { StatusText.Text = "not connected"; return; }
            try
            {
                await action();
                Log(what + " sent");
            }
            catch (Exception ex) { Log(what + " failed: " + ex.Message); }
        }

        // ---- log ----

        void CopyLog_Click(object sender, RoutedEventArgs e)
        {
            var pkg = new DataPackage();
            pkg.SetText(AppLog.Report);
            Clipboard.SetContent(pkg);
            Log("log copied to clipboard");
        }

        async void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary, SuggestedFileName = "scq30-log" };
                picker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
                var file = await picker.PickSaveFileAsync();
                if (file == null) return;
                await FileIO.WriteTextAsync(file, AppLog.Report);
                Log("log exported to " + file.Path);
            }
            catch (Exception ex) { Log("export failed: " + ex.Message); }
        }

        void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            AppLog.Clear();
        }

        // ---- device ----

        async void Scan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "scanning…";
                var list = await SoundcoreClient.FindDevicesAsync();
                DeviceList.ItemsSource = list
                    .OrderBy(d => d.Name)
                    .ToList();
                StatusText.Text = list.Count + " device(s)";
            }
            catch (Exception ex) { StatusText.Text = ex.Message; Log("scan failed: " + ex.Message); }
        }

        async void Connect_Click(object sender, RoutedEventArgs e)
        {
            var entry = DeviceList.SelectedItem as DeviceEntry;
            if (entry == null) { await new MessageDialog("Pick a device first.").ShowAsync(); return; }

            try
            {
                if (client != null) client.Dispose();
                client = new SoundcoreClient();
                client.StateChanged += s => Ui(() => ShowState(s));
                client.DualDevicesChanged += d => Ui(() => ShowDual(d));
                StatusText.Text = "connecting…";
                await client.ConnectAsync(entry);
                StatusText.Text = "connected, waiting for state";
            }
            catch (Exception ex)
            {
                StatusText.Text = "failed: " + ex.Message;
                Log("connect failed: " + ex);
            }
        }

        void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Run("refresh", () => client.RequestStateAsync());
        }

        // ---- show state ----

        void ShowState(DeviceState s)
        {
            updating = true;
            try
            {
                BatteryText.Text = "L " + s.BatteryLeft + "/10" + (s.ChargingLeft ? "⚡" : "") +
                                   "   R " + s.BatteryRight + "/10" + (s.ChargingRight ? "⚡" : "");
                FirmwareText.Text = "firmware L " + s.FirmwareLeft + " · R " + s.FirmwareRight;
                SerialText.Text = "serial " + s.Serial;
                TwsText.Text = "tws " + (s.TwsConnected ? "connected" : "not connected");
                NcSummaryText.Text = Pick(Protocol.AmbientModes, s.AmbientMode) + " · " + Pick(Protocol.NcModes, s.NcMode) +
                                     " · manual " + s.ManualLevel + " · adaptive " + s.AdaptiveLevel + "/5";

                AmbientBox.SelectedIndex = Clamp(s.AmbientMode, 0, 2);
                NcModeBox.SelectedIndex = Clamp(s.NcMode, 0, 2);
                ManualSlider.Value = Clamp(s.ManualLevel, 1, 5);
                AdaptiveInfo.Text = "adaptive level " + s.AdaptiveLevel + "/5 (read only)";
                SensSlider.Value = Clamp(s.Sensitivity, 0, 10);
                SceneBox.SelectedIndex = Clamp(s.MultiScene, 0, 2);
                WindToggle.IsOn = s.WindSuppression;
                WindInfo.Text = "wind detected: " + (s.WindDetected ? "yes" : "no");

                int preset = Array.FindIndex(Protocol.EqPresets, p => p.Id == s.EqPresetId);
                EqPresetBox.SelectedIndex = preset >= 0 ? preset + 1 : 0;
                for (int i = 0; i < 8; i++) eqSliders[i].Value = s.EqBands[i];
                SetEqEnabled();

                for (int i = 0; i < 8; i++)
                {
                    int nibble = s.TwsConnected ? (s.Buttons[i] & 0x0F) : (s.Buttons[i] >> 4);
                    int idx = nibble == 0xF ? Protocol.ButtonActions.Length : Array.IndexOf(Protocol.ButtonActionIds, (byte)nibble);
                    buttonBoxes[i].SelectedIndex = idx;
                }

                TouchToggle.IsOn = s.TouchTone;
                SurroundToggle.IsOn = s.Surround;
                GamingToggle.IsOn = s.GamingMode;
                GamingToggle.IsEnabled = s.GamingModeSupported;
                LowBatToggle.IsOn = s.LowBatteryPrompt;
                ApoToggle.IsOn = s.AutoPowerOff;
                ApoBox.SelectedIndex = Clamp(s.AutoPowerOffIndex, 0, 3);
                CycleNc.IsChecked = (s.AmbientCycle & 1) != 0;
                CycleTr.IsChecked = (s.AmbientCycle & 2) != 0;
                CycleNormal.IsChecked = (s.AmbientCycle & 4) != 0;
                DualToggle.IsOn = s.DualConnections;
                if (!s.DualConnections) DualPanel.Children.Clear();

                StatusText.Text = "connected";
            }
            finally { updating = false; }
        }

        static int Clamp(int v, int lo, int hi)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }

        static string Pick(string[] names, int i)
        {
            return i >= 0 && i < names.Length ? names[i] : "?";
        }

        // ---- sound ----

        void ApplySound_Click(object sender, RoutedEventArgs e)
        {
            if (client == null || client.State == null) { StatusText.Text = "no state yet"; return; }
            int ambient = Math.Max(0, AmbientBox.SelectedIndex);
            int nc = Math.Max(0, NcModeBox.SelectedIndex);
            int manual = (int)ManualSlider.Value;
            int adaptive = client.State.AdaptiveLevel;
            bool wind = WindToggle.IsOn;
            int sens = (int)SensSlider.Value;
            int scene = Math.Max(0, SceneBox.SelectedIndex);
            Run("sound modes", async () =>
            {
                await client.SetSoundModesAsync(ambient, nc, manual, adaptive, wind, sens, scene);
                await client.RequestStateAsync();
            });
        }

        // ---- equalizer ----

        void SetEqEnabled()
        {
            bool custom = EqPresetBox.SelectedIndex <= 0;
            foreach (var s in eqSliders) s.IsEnabled = custom;
        }

        void EqPreset_Changed(object sender, SelectionChangedEventArgs e)
        {
            SetEqEnabled();
            if (updating) return;
            int i = EqPresetBox.SelectedIndex;
            if (i <= 0) return;
            var bands = Protocol.EqPresets[i - 1].Bands;
            updating = true;
            for (int b = 0; b < 8; b++) eqSliders[b].Value = bands[b];
            updating = false;
        }

        void ApplyEq_Click(object sender, RoutedEventArgs e)
        {
            int i = EqPresetBox.SelectedIndex;
            ushort id;
            int[] bands;
            if (i <= 0)
            {
                id = Protocol.CustomEqId;
                bands = Protocol.CustomBands10(eqSliders.Select(s => (int)s.Value).ToArray());
            }
            else
            {
                id = Protocol.EqPresets[i - 1].Id;
                bands = Protocol.EqPresets[i - 1].Bands;
            }
            Run("equalizer", async () =>
            {
                await client.SetEqualizerAsync(id, bands);
                await client.RequestStateAsync();
            });
        }

        // ---- buttons ----

        void ButtonChanged(int i)
        {
            if (updating || client == null || client.State == null) return;
            int sel = buttonBoxes[i].SelectedIndex;
            if (sel < 0) return;
            var st = client.State;
            int id = sel == Protocol.ButtonActions.Length ? 0xF : Protocol.ButtonActionIds[sel];
            byte old = st.Buttons[i];
            byte nb = st.TwsConnected ? (byte)((old & 0xF0) | id) : (byte)((old & 0x0F) | (id << 4));
            st.Buttons[i] = nb;
            Run("button " + Protocol.ButtonNames[i], () => client.SetButtonAsync(i, nb));
        }

        void ResetButtons_Click(object sender, RoutedEventArgs e)
        {
            Run("reset buttons", () => client.ResetButtonsAsync());
        }

        // ---- more ----

        void Touch_Toggled(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            bool v = TouchToggle.IsOn;
            Run("touch tone", () => client.SetTouchToneAsync(v));
        }

        void Surround_Toggled(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            bool v = SurroundToggle.IsOn;
            Run("surround", () => client.SetSurroundAsync(v));
        }

        void Gaming_Toggled(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            bool v = GamingToggle.IsOn;
            Run("gaming mode", () => client.SetGamingModeAsync(v));
        }

        void LowBat_Toggled(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            bool v = LowBatToggle.IsOn;
            Run("low battery prompt", () => client.SetLowBatteryPromptAsync(v));
        }

        void Apo_Toggled(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            bool on = ApoToggle.IsOn;
            int idx = Math.Max(0, ApoBox.SelectedIndex);
            Run("auto power off", () => client.SetAutoPowerOffAsync(on, idx));
        }

        void Apo_Selected(object sender, SelectionChangedEventArgs e)
        {
            Apo_Toggled(sender, null);
        }

        void Cycle_Click(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            byte mask = 0;
            if (CycleNc.IsChecked == true) mask |= 1;
            if (CycleTr.IsChecked == true) mask |= 2;
            if (CycleNormal.IsChecked == true) mask |= 4;
            Run("ambient cycle", () => client.SetAmbientCycleAsync(mask));
        }

        void Dual_Toggled(object sender, RoutedEventArgs e)
        {
            if (updating) return;
            bool v = DualToggle.IsOn;
            Run("dual connections", async () =>
            {
                await client.SetDualConnectionsAsync(v);
                await client.RequestStateAsync();
            });
        }

        void ShowDual(List<DualDevice> devices)
        {
            DualPanel.Children.Clear();
            foreach (var d in devices)
            {
                var dev = d;
                var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
                panel.Children.Add(new TextBlock { Text = (dev.Name.Length > 0 ? dev.Name : "(unnamed)") + (dev.Connected ? "  · connected" : ""), FontSize = 16 });
                panel.Children.Add(new TextBlock { Text = dev.MacText, FontSize = 12, Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gray) });
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                var toggle = new Button { Content = dev.Connected ? "disconnect" : "connect", Margin = new Thickness(0, 4, 8, 0) };
                toggle.Click += (s, e) =>
                    Run(dev.Connected ? "disconnect" : "connect", async () =>
                    {
                        if (dev.Connected) await client.DualDisconnectAsync(dev.Mac); else await client.DualConnectAsync(dev.Mac);
                        await client.RequestDualDevicesAsync();
                    });
                var forget = new Button { Content = "forget", Margin = new Thickness(0, 4, 0, 0) };
                forget.Click += (s, e) =>
                    Run("forget", async () =>
                    {
                        await client.DualForgetAsync(dev.Mac);
                        await client.RequestDualDevicesAsync();
                    });
                row.Children.Add(toggle);
                row.Children.Add(forget);
                panel.Children.Add(row);
                DualPanel.Children.Add(panel);
            }
        }
    }
}
