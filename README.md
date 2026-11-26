# OpenSCQ30 for Windows Phone 8.1

## About

OpenSCQ30 for Windows Phone 8.1 is free software for controlling settings for Soundcore headphones and earbuds from a Windows Phone 8.1 phone. It is a port of the protocol work in [OpenSCQ30](https://github.com/Oppzippy/OpenSCQ30) by Oppzippy, written as a native Windows Phone Store app because the original project has no Windows Phone build.

This is the `wp81` branch. The Windows 10 Mobile (UWP) version lives on `main`.

This project is not affiliated with Soundcore, Anker or the original OpenSCQ30 project. It reuses the original project's name, icon and protocol documentation, and it is licensed under GPL-3.0 like the original.

### Supported Platforms

[x] Windows Phone 8.1 (ARM) - Connects on real hardware  
[x] Windows 10 Mobile (ARM) - Working on real hardware (`main` branch)

Target: Windows Phone 8.1 (OS 6.3.1).

### Building

Visual Studio 2017 and later cannot build Windows Phone 8.1 projects, so this branch builds without the XAML compiler: `MainPage.xaml` is embedded in the app and loaded at runtime with `XamlReader.Load`. `build.ps1` compiles with the C# compiler from any recent Visual Studio and packages with the SDK tools.

Requirements: Visual Studio 2017 or later (any edition, for `csc.exe`), the Windows Phone 8.1 SDK, the Windows 8.1 SDK and a Windows 10 SDK.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

This writes `out\SCQ30Mobile_wp81.appx`, signed with a self-signed `CN=Royalturd` certificate that the script creates on first run. The phone must be developer unlocked. To deploy over USB:

```powershell
& "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows Phone\v8.1\Tools\AppDeploy\AppDeployCmd.exe" /installlaunch out\SCQ30Mobile_wp81.appx /targetdevice:de
```

`SCQ30Mobile.csproj` still opens in Visual Studio 2015 with the Windows Phone 8.1 tools.

Differences from the Windows 10 Mobile version:

- Devices are found through their RFCOMM services, because Windows Phone 8.1 has no `BluetoothDevice` class.
- The live tile uses the Windows Phone 8.1 text templates (medium and wide).
- The log `share` button opens the share UI, because Windows Phone 8.1 has no clipboard API.
- The log header shows the phone model, not the OS build number.
- The page is loaded at runtime from `MainPage.xaml`, and event handlers are attached in `MainPage.Ui.cs`.

### Supported Devices

Two models are supported. The model is detected from the serial number, and the app hides settings the model does not have. Other models in the original project use different packet layouts and are not implemented.

| Model | Name                         | Status                                                                 |
| ----- | ---------------------------- | ---------------------------------------------------------------------- |
| A3959 | Soundcore P30i / R50i NC     | Working on real hardware                                               |
| A3949 | Soundcore P20i / P25i / R50i | Untested. Parser checked against OpenSCQ30 test packets only. No noise canceling, no custom equalizer, 6 buttons, gaming mode and touch tone only |

### Supported Settings

| Tab     | Settings                                                                                                                             |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| status  | Battery and charging for each earbud, firmware, serial number, TWS state                                                             |
| sound   | Ambient sound mode, noise canceling mode (manual, adaptive, multi-scene), manual strength, adaptive sensitivity, scene, wind noise   |
| eq      | 22 presets and a custom 8-band equalizer                                                                                             |
| buttons | Action for single, double, triple and long press on each earbud, reset to default                                                    |
| more    | Touch tone, surround sound, gaming mode, low battery prompt, auto power off, ambient button cycle, dual connections                  |
| log     | Timestamped traffic log with copy and export                                                                                         |

Not implemented: saving custom equalizer profiles and importing or exporting them.

## Authorship

This app was written entirely by Claude (Anthropic's AI model) working with me, the project owner, who directed the work and tested it on the phone. The protocol knowledge it uses comes from the original OpenSCQ30 project, not from Claude or me.

## Status

Confirmed working on real hardware (Lumia 950, build 15254, with a Soundcore A3959). The packet layouts come from the original project's source and test data. Other firmware versions and devices may differ. If something does not respond, open the `log` tab, copy or export the log, and include it in a bug report.

## Original Project

All protocol knowledge comes from [Oppzippy/OpenSCQ30](https://github.com/Oppzippy/OpenSCQ30) (GPL-3.0). If you are on Windows 10/11 desktop, Linux or Android, use the original app instead. Its source is mirrored on [Codeberg](https://codeberg.org/Oppzippy/OpenSCQ30).

## License

GPL-3.0, the same license as the original project.
