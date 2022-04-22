using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace StoicGoose.IO
{
	public sealed class WaveFileWriter
	{
		readonly WaveHeader waveHeader = default;
		readonly FormatChunk formatChunk = default;
		readonly DataChunk dataChunk = default;

		public WaveFileWriter(int sampleRate, int numChannels)
		{
			waveHeader = new();
			formatChunk = new FormatChunk(sampleRate, numChannels);
			dataChunk = new();
		}

		public void Write(short[] samples) => dataChunk.Write(samples);

		public void Save(Stream stream)
		{
			waveHeader.FileLength += formatChunk.ChunkSize + 8;
			waveHeader.FileLength += dataChunk.ChunkSize + 8;

			stream.Write(waveHeader.Bytes);
			stream.Write(formatChunk.Bytes);
			stream.Write(dataChunk.Bytes);
		}

		private class WaveHeader
		{
			public string FileTypeId => "RIFF";
			public uint FileLength { get; set; } = 4;   /* Minimum size is always 4 bytes */
			public string MediaTypeId => "WAVE";

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
			public string ChunkId => "fmt ";
			public uint ChunkSize => 16;
			public ushort FormatTag => 1;       /* MS PCM (Uncompressed wave file) */
			public ushort Channels => channels;
			public uint Frequency => frequency;
			public uint AverageBytesPerSec { get; private set; } = 0;
			public ushort BlockAlign { get; private set; } = 0;
			public ushort BitsPerSample => 16;

			readonly ushort channels = 2;       /* Default to stereo */
			readonly uint frequency = 44100;    /* Default to 44100hz */

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
			public string ChunkId => "data";
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
