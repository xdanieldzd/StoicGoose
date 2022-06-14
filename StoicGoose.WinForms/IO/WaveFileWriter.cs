using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace StoicGoose.WinForms.IO
{
	public sealed class WaveFileWriter : IDisposable
	{
		readonly Stream outStream = default;

		readonly WaveHeader waveHeader = default;
		readonly FormatChunk formatChunk = default;
		readonly DataChunk dataChunk = default;

		public WaveFileWriter(string filename, int sampleRate, int numChannels)
		{
			outStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

			waveHeader = new();
			formatChunk = new FormatChunk(sampleRate, numChannels);
			dataChunk = new();
		}

		~WaveFileWriter()
		{
			Dispose();
		}

		public void Dispose()
		{
			outStream.Flush();
			outStream.Dispose();

			GC.SuppressFinalize(this);
		}

		public void Write(short[] samples) => dataChunk.Write(samples);

		public void Save()
		{
			waveHeader.FileLength += FormatChunk.ChunkSize + 8;
			waveHeader.FileLength += dataChunk.ChunkSize + 8;

			outStream.Write(waveHeader.Bytes);
			outStream.Write(formatChunk.Bytes);
			outStream.Write(dataChunk.Bytes);

			outStream.Flush();
		}

		private class WaveHeader
		{
			public static string FileTypeId => "RIFF";
			public uint FileLength { get; set; } = 4;   /* Minimum size is always 4 bytes */
			public static string MediaTypeId => "WAVE";

			public ReadOnlySpan<byte> Bytes
			{
				get
				{
					var chunkData = new List<byte>();
					chunkData.AddRange(Encoding.ASCII.GetBytes(FileTypeId));
					chunkData.AddRange(BitConverter.GetBytes(FileLength));
					chunkData.AddRange(Encoding.ASCII.GetBytes(MediaTypeId));
					return new(chunkData.ToArray());
				}
			}
		}

		private class FormatChunk
		{
			public static string ChunkId => "fmt ";
			public static uint ChunkSize => 16;
			public static ushort FormatTag => 1;        /* MS PCM (Uncompressed wave file) */
			public ushort Channels => channels;
			public uint Frequency => frequency;
			public uint AverageBytesPerSec { get; private set; } = 0;
			public ushort BlockAlign { get; private set; } = 0;
			public static ushort BitsPerSample => 16;

			readonly ushort channels = 2;               /* Default to stereo */
			readonly uint frequency = 44100;            /* Default to 44100hz */

			public FormatChunk(int frequency, int channels)
			{
				this.channels = (ushort)channels;
				this.frequency = (ushort)frequency;
				RecalcBlockSizes();
			}

			private void RecalcBlockSizes()
			{
				BlockAlign = (ushort)(channels * (BitsPerSample / 8));
				AverageBytesPerSec = frequency * BlockAlign;
			}

			public ReadOnlySpan<byte> Bytes
			{
				get
				{
					var chunkData = new List<byte>();
					chunkData.AddRange(Encoding.ASCII.GetBytes(ChunkId));
					chunkData.AddRange(BitConverter.GetBytes(ChunkSize));
					chunkData.AddRange(BitConverter.GetBytes(FormatTag));
					chunkData.AddRange(BitConverter.GetBytes(Channels));
					chunkData.AddRange(BitConverter.GetBytes(Frequency));
					chunkData.AddRange(BitConverter.GetBytes(AverageBytesPerSec));
					chunkData.AddRange(BitConverter.GetBytes(BlockAlign));
					chunkData.AddRange(BitConverter.GetBytes(BitsPerSample));
					return new(chunkData.ToArray());
				}
			}
		}

		private class DataChunk
		{
			public static string ChunkId => "data";
			public uint ChunkSize => (uint)(WaveData.Count * 2);
			public List<short> WaveData { get; private set; } = new();

			public ReadOnlySpan<byte> Bytes
			{
				get
				{
					var chunkData = new List<byte>();
					chunkData.AddRange(Encoding.ASCII.GetBytes(ChunkId));
					chunkData.AddRange(BitConverter.GetBytes(ChunkSize));
					chunkData.AddRange(MemoryMarshal.Cast<short, byte>(WaveData.ToArray()).ToArray());
					return new(chunkData.ToArray());
				}
			}

			public void Write(short[] samples) => WaveData.AddRange(samples);
		}
	}
}
