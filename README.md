# Edi.MIDIPlayer

A command-line MIDI player that provides real-time console output of MIDI events while playing music. This tool displays detailed information about MIDI events including note on/off events, control changes, and program changes with timestamps and visual indicators.

> IMPORTANT NOTICE: This tool is currently **Windows-only** due to its reliance on Windows-specific MIDI subsystems.

## Features

- 🎵 **Real-time MIDI playback** with live console event analysis
- 🎨 **Color-coded console output** for different MIDI event types
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
- **.NET 9.0** or later
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

Run without arguments to enter interactive mode:

```powershell
midi-player
```

The program will prompt you to enter a MIDI file path.

- `.mid` - Standard MIDI files
- `.midi` - Standard MIDI files
