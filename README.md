# OpenSCQ30 for Windows 10 Mobile

## About

OpenSCQ30 for Windows 10 Mobile is free software for controlling settings for Soundcore headphones and earbuds from a Windows 10 Mobile phone. It is a port of the protocol work in [OpenSCQ30](https://github.com/Oppzippy/OpenSCQ30) by Oppzippy, written as a native UWP app because the original project has no Windows 10 Mobile build.

This project is not affiliated with Soundcore, Anker or the original OpenSCQ30 project. It reuses the original project's name, icon and protocol documentation, and it is licensed under GPL-3.0 like the original.

### Supported Platforms

[x] Windows 10 Mobile (ARM) - Working on real hardware  
[ ] Windows 10 desktop (x86 / x64) - Not built or tested

Target: Windows 10 build 10240 or newer. Developed for a Lumia 950 on build 15254 (version 1709).

### Supported Devices

Only one model is supported so far. Other models in the original project use different packet layouts and are not implemented.

| Model | Name                     |
| ----- | ------------------------ |
| A3959 | Soundcore P30i / R50i NC |

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
