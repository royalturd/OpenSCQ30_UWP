# OpenSCQ30 for Windows 10 Mobile

UWP app for Windows 10 Mobile (tested target: Lumia 950, build 15254) that talks to Soundcore
earbuds and headphones (currently the A3959 protocol) over Bluetooth RFCOMM.

The protocol layout is taken from [OpenSCQ30](https://github.com/Oppzippy/OpenSCQ30) (GPL-3.0),
so this project is also GPL-3.0.

## Status

Experimental. Packet offsets are inferred from OpenSCQ30 test fixtures and are unverified on hardware.
The `log` tab shows raw TX and RX for debugging. Implemented: device scan, connect, battery,
firmware, serial, EQ readout, manual noise-cancelling level.
