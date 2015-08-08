﻿using System;
using gnu.sound.midi.info;

namespace gnu.sound.midi
{
	/// <summary>Common manipulations of Midi Sequences.</summary>
	public static class SequenceExtensions
	{
		#region Useful methods based the excellent MidiSharp package by Stephen Toub
		
		/// <summary>Transposes a MIDI sequence up/down the specified number of half-steps.</summary>
		/// <param name="sequence">The sequence to be transposed.</param>
		/// <param name="steps">The number of steps up(+) or down(-) to transpose the sequence.</param>
		public static void Transpose(this Sequence sequence, int steps)
		{
			// Transpose the sequence; do not transpose drum tracks
			Transpose(sequence, steps, false);
		}

		/// <summary>Transposes a MIDI sequence up/down the specified number of half-steps.</summary>
		/// <param name="sequence">The sequence to be transposed.</param>
		/// <param name="steps">The number of steps up(+) or down(-) to transpose the sequence.</param>
		/// <param name="includeDrums">Whether drum tracks should also be transposed.</param>
		/// <remarks>If the step value is too large or too small, notes may wrap.</remarks>
		public static void Transpose(this Sequence sequence, int steps, bool includeDrums)
		{
			// Modify each track
			foreach (Track track in sequence.Tracks) {
				// Modify each event
				foreach (MidiEvent ev in track.Events) {
					
					// If the event is not a voice MIDI event but the channel is the
					// drum channel and the user has chosen not to include drums in the
					// transposition (which makes sense), skip this event.
					MidiMessage msg = ev.Message;
					
					if (msg is ShortMessage) {

						// get status code
						int st = msg.GetStatus();
						
						// check if this is a channel message
						if ((st & 0xf0) <= 0xf0)
						{
							var channel = ((ShortMessage)msg).GetChannel();
							
							if (!includeDrums && channel == (byte)MidiHelper.DRUM_CHANNEL)
								continue;

							int cmd = ((ShortMessage)msg).GetCommand();
							
							// If the event is a NoteOn, NoteOff, or Aftertouch
							if (cmd == (int) MidiHelper.MidiEventType.NoteOff
							    || cmd == (int) MidiHelper.MidiEventType.NoteOn
							    || cmd == (int) MidiHelper.MidiEventType.AfterTouchPoly) {
								
								// shift the note according to the supplied number of steps.
								var data1 = ((ShortMessage)msg).GetData1();
								var data2 = ((ShortMessage)msg).GetData2();

								// note number is stored in data[1]
								byte noteTransposed = (byte)((data1 + steps) % 128);
								
								// store the track number as the channel
								((ShortMessage)msg).SetMessage(cmd, channel, noteTransposed, data2);
							}
						}
					}
				}
			}
		}

		/// <summary>Trims a MIDI file to a specified length.</summary>
		/// <param name="sequence">The sequence to be copied and trimmed.</param>
		/// <param name="totalTime">The requested time length of the new MIDI sequence.</param>
		/// <returns>A MIDI sequence with only those events that fell before the requested time limit.</returns>
		public static Sequence Trim(this Sequence sequence, long totalTime)
		{
			// Create a new sequence to mimic the old
			var newSequence = new Sequence(sequence.DivisionType, sequence.Resolution, 0, sequence.MidiFileType);

			// Copy each track up to the specified time limit
			foreach (Track track in sequence.Tracks) {
				// Create a new track in the new sequence to match the old track in the old sequence
				var newTrack = newSequence.CreateTrack();

				// Copy over all events that fell before the specified time
				for (int i = 0; i < track.Events.Count && track.Events[i].Tick < totalTime; i++) {
					newTrack.Events.Add(track.Events[i].DeepClone()); // add at the end
					//newTrack.Add(track.Events[i].DeepClone()); // insert at correct timing
				}

				// If the new track lacks an end of track, add one
				if (!newTrack.HasEndOfTrack) {
					newTrack.Add(MetaEvent.CreateMetaEvent("EndOfTrack", "", newTrack.Ticks(), 0));
				}
			}

			// Return the new sequence
			return newSequence;
		}
		
		/// <summary>Converts a MIDI sequence from its current format to the specified format.</summary>
		/// <param name="sequence">The sequence to be converted.</param>
		/// <param name="format">The format to which we want to convert the sequence.</param>
		/// <returns>The converted sequence.</returns>
		/// <remarks>
		/// This may or may not return the same sequence as passed in.
		/// </remarks>
		/// <remarks>This is based on the excellent MidiSharp package by Stephen Toub.</remarks>
		public static Sequence Convert(this Sequence sequence, int format)
		{
			return Convert(sequence, format, FormatConversionOption.None);
		}
		
		/// <summary>Converts the MIDI sequence into a new one with the desired format.</summary>
		/// <param name="sequence">The sequence to be converted.</param>
		/// <param name="format">The format to which we want to convert the sequence.</param>
		/// <param name="options">Options used when doing the conversion.</param>
		/// <returns>The new, converted sequence.</returns>
		/// <remarks>This is based on the excellent MidiSharp package by Stephen Toub.</remarks>
		public static Sequence Convert(this Sequence sequence, int format, FormatConversionOption options)
		{
			if (sequence.MidiFileType == format) {
				// If the desired format is the same as the original, just return a copy.
				// No transformation is necessary.
				sequence = new Sequence(sequence);
			}
			else if (format != 0 || sequence.Tracks.Count == 1) {
				// If the desired format is is not 0 or there's only one track, just copy the sequence with a different format number.
				// If it's not zero, then multiple tracks are acceptable, so no transformation is necessary.
				// Or if there's only one track, then there's no possible transformation to be done.
				var newSequence = new Sequence(sequence.DivisionType, sequence.Resolution, 0, format);
				foreach (Track t in sequence.Tracks) {
					newSequence.Tracks.Add(new Track(t));
				}
				sequence = newSequence;
			}
			else {
				// Now the harder cases, converting to format 0.  We need to combine all tracks into 1,
				// as format 0 requires that there only be a single track with all of the events for the song.
				sequence = new Sequence(sequence);
				sequence.MidiFileType = (int) MidiHelper.MidiFormat.SingleTrack;

				// Add all events to new track (except for end of track markers!)
				int trackNumber = 0;
				var newTrack = new Track();
				foreach (Track track in sequence.Tracks) {
					foreach (MidiEvent midiEvent in track.Events) {
						bool doAddEvent = true;
						
						MidiMessage msg = midiEvent.Message;
						if (msg is MetaMessage) {
							// add all meta messages except the end of track markers (we'll add our own)
							int type = ((MetaMessage)msg).GetMetaMessageType();
							if (type == (int) MidiHelper.MetaEventType.EndOfTrack) {
								doAddEvent = false;
							}
						} else if (msg is ShortMessage) {
							// If this event has a channel, and if we're storing tracks as channels, copy to it
							if ((options & FormatConversionOption.CopyTrackToChannel) > 0
							    && trackNumber >= MidiHelper.MIN_CHANNEL && trackNumber <= MidiHelper.MAX_CHANNEL) {

								// get status code
								int st = msg.GetStatus();
								
								// check if this is a channel message
								if ((st & 0xf0) <= 0xf0)
								{
									// get the data
									var commandByte = ((ShortMessage)msg).GetCommand();
									var data1 = ((ShortMessage)msg).GetData1();
									var data2 = ((ShortMessage)msg).GetData2();
									
									// store the track number as the channel
									((ShortMessage)msg).SetMessage(commandByte, trackNumber, data1, data2);
								}
							}
						} else if (msg is SysexMessage) {
							
						}
						
						// Add all events, except for end of track markers (we'll add our own)
						if (doAddEvent) {
							//newTrack.Events.Add(midiEvent);
							newTrack.Add(midiEvent);
						}
					}
					trackNumber++;
				}

				// Sort the events by total time
				// and top things off with an end-of-track marker.
				//newTrack.Events.Sort((x, y) => x.Tick.CompareTo(y.Tick));
				newTrack.Add(MetaEvent.CreateMetaEvent("EndOfTrack", "", newTrack.Ticks(), 0));

				// We now have all of the combined events in newTrack.  Clear out the sequence, replacing all the tracks
				// with this new one.
				sequence.Tracks.Clear();
				sequence.Tracks.Add(newTrack);
			}

			return sequence;
		}

		/// <summary>Options used when performing a format conversion.</summary>
		public enum FormatConversionOption
		{
			/// <summary>No special formatting.</summary>
			None,

			/// <summary>
			/// Uses the number of the track as the channel for all events on that track.
			/// Only valid if the number of the track is a valid track number.
			/// </summary>
			CopyTrackToChannel
		}
		#endregion
		
	}
}