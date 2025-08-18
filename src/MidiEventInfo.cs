using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edi.MIDIPlayer;

class MidiEventInfo
{
    public long AbsoluteTime { get; set; }
    public MidiEvent Event { get; set; }
}