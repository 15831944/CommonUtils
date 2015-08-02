// MidiFilerReader.java -- MIDI file reading services
//   Copyright (C) 2005 Free Software Foundation, Inc.

using System.IO;

namespace gnu.sound.midi.spi
{
	///
	/// The MidiFileReader abstract class defines the methods to be provided
	/// by a MIDI file reader.
	/// @author Anthony Green (green@redhat.com)
	/// @since 1.3
	public abstract class MidiFileReader
	{
		/// <summary>
		/// Read a MidiFileFormat from the given stream.
		/// @param stream the stream from which to read the MIDI data
		/// @return the MidiFileFormat object
		/// @throws InvalidMidiDataException if the stream refers to invalid data
		/// @throws IOException if an I/O exception occurs while reading
		/// </summary>
		public abstract MidiFileFormat GetMidiFileFormat(Stream stream);
		
		/// <summary>
		/// Read a MidiFileFormat from the given stream.
		/// @param url the url from which to read the MIDI data
		/// @return the MidiFileFormat object
		/// @throws InvalidMidiDataException if the url refers to invalid data
		/// @throws IOException if an I/O exception occurs while reading
		/// </summary>
		public abstract MidiFileFormat GetMidiFileFormat(string url);

		/// <summary>
		/// Read a MidiFileFormat from the given stream.
		/// @param file the file from which to read the MIDI data
		/// @return the MidiFileFormat object
		/// @throws InvalidMidiDataException if the file refers to invalid data
		/// @throws IOException if an I/O exception occurs while reading
		/// </summary>
		public abstract MidiFileFormat GetMidiFileFormat(FileInfo file);
		
		/// <summary>
		/// Read a Sequence from the given stream.
		/// @param stream the stream from which to read the MIDI data
		/// @return the Sequence object
		/// @throws InvalidMidiDataException if the stream refers to invalid data
		/// @throws IOException if an I/O exception occurs while reading
		/// </summary>
		public abstract Sequence GetSequence(Stream stream);
		
		/// <summary>
		/// Read a Sequence from the given stream.
		/// @param url the url from which to read the MIDI data
		/// @return the Sequence object
		/// @throws InvalidMidiDataException if the url refers to invalid data
		/// @throws IOException if an I/O exception occurs while reading
		/// </summary>
		public abstract Sequence GetSequence(string url);
		
		/// <summary>
		/// Read a Sequence from the given stream.
		/// @param file the file from which to read the MIDI data
		/// @return the Sequence object
		/// @throws InvalidMidiDataException if the file refers to invalid data
		/// @throws IOException if an I/O exception occurs while reading
		/// </summary>
		public abstract Sequence GetSequence(FileInfo file);
	}

}