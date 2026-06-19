using Edi.MIDIPlayer.Models;
using Xunit;

namespace Edi.MIDIPlayer.Tests;

public class ActiveNoteTrackerTests
{
    [Fact]
    public void NoteOn_CountsSamePitchOnDifferentChannelsSeparately()
    {
        var tracker = new ActiveNoteTracker();

        tracker.NoteOn(channel: 0, noteNumber: 60);
        tracker.NoteOn(channel: 1, noteNumber: 60);

        Assert.Equal(2, tracker.ActiveCount);
    }

    [Fact]
    public void NoteOff_RemovesOnlyMatchingChannelAndNote()
    {
        var tracker = new ActiveNoteTracker();
        tracker.NoteOn(channel: 0, noteNumber: 60);
        tracker.NoteOn(channel: 1, noteNumber: 60);

        tracker.NoteOff(channel: 0, noteNumber: 60);

        Assert.Equal(1, tracker.ActiveCount);
    }

    [Fact]
    public void NoteOff_KeepsOverlappingSameChannelNoteActiveUntilFinalOff()
    {
        var tracker = new ActiveNoteTracker();
        tracker.NoteOn(channel: 0, noteNumber: 60);
        tracker.NoteOn(channel: 0, noteNumber: 60);

        tracker.NoteOff(channel: 0, noteNumber: 60);

        Assert.Equal(1, tracker.ActiveCount);

        tracker.NoteOff(channel: 0, noteNumber: 60);

        Assert.Equal(0, tracker.ActiveCount);
    }

    [Fact]
    public void NoteOff_IgnoresUnknownNotes()
    {
        var tracker = new ActiveNoteTracker();

        tracker.NoteOff(channel: 0, noteNumber: 60);

        Assert.Equal(0, tracker.ActiveCount);
    }
}
