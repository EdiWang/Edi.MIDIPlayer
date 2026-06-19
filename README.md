# Edi.MIDIPlayer

A MIDI player that can visualize playback in either the default SignalR web UI or the v1 terminal UI. It displays detailed MIDI events while playing music, including note on/off events, control changes, program changes, timestamps, and visual indicators.

> IMPORTANT NOTICE: This tool is currently **Windows-only** due to its reliance on Windows-specific MIDI subsystems.

## Features

- 🎵 **Real-time MIDI playback** with live event analysis
- 🌐 **Default web visualizer** powered by SignalR
- 🎨 **Optional color-coded terminal output** for different MIDI event types
- 📊 **Visual velocity bars** showing note velocity levels
- ⏱️ **Precise timing** with millisecond-accurate timestamps
- 🎼 **Multi-track support** with automatic event merging and ordering
- 🎛️ **MIDI event display** including:
  - Note On/Off events with note names and octaves
  - Control change events
  - Program change events
  - Tempo changes with BPM calculation
- 🔢 **Hexadecimal display** for technical MIDI analysis
- 🎹 **Piano-style ASCII art** banner

## Requirements

- **Windows only** (uses Windows-specific MIDI subsystems)
- **.NET 10.0** or later
- **MIDI output device** (software or hardware synthesizer)
- **NAudio library** (automatically installed via NuGet)

## Installation

### As a Global Tool (Recommended)

```powershell
dotnet tool install -g Edi.MIDIPlayer
```

## Usage

After installation, you can use the tool globally:

```powershell
midi-player "path\to\your\file.mid"
```

This starts the web visualizer by default.

or play a MIDI file from Internet:

```powershell
midi-player "https://example.com/path/to/your/file.mid"
```

To use the terminal UI from v1:

```powershell
midi-player --display console "path\to\your\file.mid"
```

Equivalent shortcuts:

```powershell
midi-player --console "path\to\your\file.mid"
midi-player --display web "path\to\your\file.mid"
midi-player --web "path\to\your\file.mid"
```

Run without a MIDI file to enter interactive mode:

```powershell
midi-player
```

The program will prompt you to enter a MIDI file path.

- `.mid` - Standard MIDI files
- `.midi` - Standard MIDI files
