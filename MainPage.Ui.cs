using System.IO;
using System.Reflection;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace SCQ30Mobile
{
    // Stands in for the code the XAML compiler would generate. MainPage.xaml is an embedded
    // resource loaded with XamlReader.Load, which allows the app to build with the plain C#
    // compiler and the Windows Phone 8.1 SDK, without Visual Studio 2015.
    public sealed partial class MainPage
    {
        MediaElement Player;
        Pivot Root;
        Button ScanButton;
        ListView DeviceList;
        Button ConnectButton;
        TextBlock StatusText;
        TextBlock BatteryText;
        TextBlock FirmwareText;
        TextBlock SerialText;
        TextBlock TwsText;
        TextBlock ModelText;
        TextBlock NoiseHead;
        TextBlock NcSummaryText;
        Button RefreshButton;
        PivotItem SoundItem;
        ComboBox AmbientBox;
        ComboBox NcModeBox;
        TextBlock ManualLabel;
        Slider ManualSlider;
        TextBlock AdaptiveInfo;
        Slider SensSlider;
        ComboBox SceneBox;
        ToggleSwitch WindToggle;
        TextBlock WindInfo;
        Button ApplySoundButton;
        ComboBox EqPresetBox;
        StackPanel EqPanel;
        Button ApplyEqButton;
        StackPanel ButtonsPanel;
        Button ResetButtonsButton;
        ToggleSwitch ConfirmSoundToggle;
        ToggleSwitch AutoConnectToggle;
        ToggleSwitch TouchToggle;
        ToggleSwitch GamingToggle;
        StackPanel ExtrasPanel;
        ToggleSwitch SurroundToggle;
        ToggleSwitch LowBatToggle;
        ToggleSwitch ApoToggle;
        ComboBox ApoBox;
        CheckBox CycleNc;
        CheckBox CycleTr;
        CheckBox CycleNormal;
        ToggleSwitch DualToggle;
        StackPanel DualPanel;
        Button ShareLogButton;
        Button ExportLogButton;
        Button ClearLogButton;
        ScrollViewer LogScroll;
        TextBlock LogText;

        void InitializeComponent()
        {
            string xaml;
            var asm = typeof(MainPage).GetTypeInfo().Assembly;
            using (var stream = asm.GetManifestResourceStream("SCQ30Mobile.MainPage.xaml"))
            using (var r = new StreamReader(stream)) xaml = r.ReadToEnd();

            var root = (Grid)XamlReader.Load(xaml);
            Background = new SolidColorBrush(Colors.Black);
            Content = root;

            Player = (MediaElement)root.FindName("Player");
            Root = (Pivot)root.FindName("Root");
            ScanButton = (Button)root.FindName("ScanButton");
            DeviceList = (ListView)root.FindName("DeviceList");
            ConnectButton = (Button)root.FindName("ConnectButton");
            StatusText = (TextBlock)root.FindName("StatusText");
            BatteryText = (TextBlock)root.FindName("BatteryText");
            FirmwareText = (TextBlock)root.FindName("FirmwareText");
            SerialText = (TextBlock)root.FindName("SerialText");
            TwsText = (TextBlock)root.FindName("TwsText");
            ModelText = (TextBlock)root.FindName("ModelText");
            NoiseHead = (TextBlock)root.FindName("NoiseHead");
            NcSummaryText = (TextBlock)root.FindName("NcSummaryText");
            RefreshButton = (Button)root.FindName("RefreshButton");
            SoundItem = (PivotItem)root.FindName("SoundItem");
            AmbientBox = (ComboBox)root.FindName("AmbientBox");
            NcModeBox = (ComboBox)root.FindName("NcModeBox");
            ManualLabel = (TextBlock)root.FindName("ManualLabel");
            ManualSlider = (Slider)root.FindName("ManualSlider");
            AdaptiveInfo = (TextBlock)root.FindName("AdaptiveInfo");
            SensSlider = (Slider)root.FindName("SensSlider");
            SceneBox = (ComboBox)root.FindName("SceneBox");
            WindToggle = (ToggleSwitch)root.FindName("WindToggle");
            WindInfo = (TextBlock)root.FindName("WindInfo");
            ApplySoundButton = (Button)root.FindName("ApplySoundButton");
            EqPresetBox = (ComboBox)root.FindName("EqPresetBox");
            EqPanel = (StackPanel)root.FindName("EqPanel");
            ApplyEqButton = (Button)root.FindName("ApplyEqButton");
            ButtonsPanel = (StackPanel)root.FindName("ButtonsPanel");
            ResetButtonsButton = (Button)root.FindName("ResetButtonsButton");
            ConfirmSoundToggle = (ToggleSwitch)root.FindName("ConfirmSoundToggle");
            AutoConnectToggle = (ToggleSwitch)root.FindName("AutoConnectToggle");
            TouchToggle = (ToggleSwitch)root.FindName("TouchToggle");
            GamingToggle = (ToggleSwitch)root.FindName("GamingToggle");
            ExtrasPanel = (StackPanel)root.FindName("ExtrasPanel");
            SurroundToggle = (ToggleSwitch)root.FindName("SurroundToggle");
            LowBatToggle = (ToggleSwitch)root.FindName("LowBatToggle");
            ApoToggle = (ToggleSwitch)root.FindName("ApoToggle");
            ApoBox = (ComboBox)root.FindName("ApoBox");
            CycleNc = (CheckBox)root.FindName("CycleNc");
            CycleTr = (CheckBox)root.FindName("CycleTr");
            CycleNormal = (CheckBox)root.FindName("CycleNormal");
            DualToggle = (ToggleSwitch)root.FindName("DualToggle");
            DualPanel = (StackPanel)root.FindName("DualPanel");
            ShareLogButton = (Button)root.FindName("ShareLogButton");
            ExportLogButton = (Button)root.FindName("ExportLogButton");
            ClearLogButton = (Button)root.FindName("ClearLogButton");
            LogScroll = (ScrollViewer)root.FindName("LogScroll");
            LogText = (TextBlock)root.FindName("LogText");

            WireEvents();
        }

        // XamlReader.Load cannot hook up event handlers, so they are attached here.
        void WireEvents()
        {
            Root.SelectionChanged += Root_SelectionChanged;

            ScanButton.Click += Scan_Click;
            ConnectButton.Click += Connect_Click;
            RefreshButton.Click += Refresh_Click;

            ApplySoundButton.Click += ApplySound_Click;
            EqPresetBox.SelectionChanged += EqPreset_Changed;
            ApplyEqButton.Click += ApplyEq_Click;
            ResetButtonsButton.Click += ResetButtons_Click;

            ConfirmSoundToggle.Toggled += ConfirmSound_Toggled;
            AutoConnectToggle.Toggled += AutoConnect_Toggled;
            TouchToggle.Toggled += Touch_Toggled;
            GamingToggle.Toggled += Gaming_Toggled;
            SurroundToggle.Toggled += Surround_Toggled;
            LowBatToggle.Toggled += LowBat_Toggled;
            ApoToggle.Toggled += Apo_Toggled;
            ApoBox.SelectionChanged += Apo_Selected;
            CycleNc.Click += Cycle_Click;
            CycleTr.Click += Cycle_Click;
            CycleNormal.Click += Cycle_Click;
            DualToggle.Toggled += Dual_Toggled;

            ShareLogButton.Click += CopyLog_Click;
            ExportLogButton.Click += ExportLog_Click;
            ClearLogButton.Click += ClearLog_Click;
        }
    }
}
