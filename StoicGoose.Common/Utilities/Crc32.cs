using System;
using System.IO;

namespace StoicGoose.Common.Utilities
{
	public static class Crc32
	{
		static readonly uint[] crcTable;
		static readonly uint crcPolynomial = 0xEDB88320;
		static readonly uint crcSeed = 0xFFFFFFFF;

		static Crc32()
		{
			crcTable = new uint[256];

			for (var i = 0; i < 256; i++)
			{
				var entry = (uint)i;
				for (int j = 0; j < 8; j++)
				{
					if ((entry & 0x00000001) == 0x00000001) entry = (entry >> 1) ^ crcPolynomial;
					else entry >>= 1;
				}
				crcTable[i] = entry;
			}
		}

		private static void VerifyStartAndLength(int dataLength, int segmentStart, int segmentLength)
		{
			if (segmentStart >= dataLength) throw new Exception("Segment start offset is greater than total length");
			if (segmentLength > dataLength) throw new Exception("Segment length is greater than total length");
			if ((segmentStart + segmentLength) > dataLength) throw new Exception("Segment end offset is greater than total length");
		}

		public static uint Calculate(FileInfo fileInfo)
		{
			return Calculate(fileInfo, 0, (int)fileInfo.Length);
		}

		public static uint Calculate(FileInfo fileInfo, int start, int length)
		{
			VerifyStartAndLength((int)fileInfo.Length, start, length);

			using FileStream file = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return Calculate(file, start, length);
		}

		public static uint Calculate(Stream stream)
		{
			return Calculate(stream, 0, (int)stream.Length);
		}

		public static uint Calculate(Stream stream, int start, int length)
		{
			VerifyStartAndLength((int)stream.Length, start, length);

			var lastStreamPosition = stream.Position;
			var data = new byte[length];
			stream.Position = start;
			stream.Read(data, 0, length);
			var crc = Calculate(data, 0, data.Length);
			stream.Position = lastStreamPosition;

			return crc;
		}

		public static uint Calculate(byte[] data)
		{
			return Calculate(data, 0, data.Length);
		}

		public static uint Calculate(byte[] data, int start, int length)
		{
			VerifyStartAndLength(data.Length, start, length);

			uint crc = crcSeed;
			for (var i = start; i < (start + length); i++)
				crc = ((crc >> 8) ^ crcTable[data[i] ^ (crc & 0x000000FF)]);
			return ~crc;
		}
	}
}
