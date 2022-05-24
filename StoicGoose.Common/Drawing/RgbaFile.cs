using System;
using System.IO;
using System.Linq;

namespace StoicGoose.Common.Drawing
{
	/* RGBA bitmap file format -- https://github.com/bzotto/rgba_bitmap
	 * ".rgba is the dumbest possible image interchange format, now available for your programming pleasure."
	 */

	public class RgbaFile
	{
		const string expectedMagic = "RGBA";

		public string MagicNumber { get; protected set; }
		public uint Width { get; protected set; }
		public uint Height { get; protected set; }
		public byte[] PixelData { get; protected set; }

		public RgbaFile(string filename) : this(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }

		public RgbaFile(Stream stream)
		{
			MagicNumber = ReadString(stream, 4);
			Width = ReadUInt32(stream);
			Height = ReadUInt32(stream);
			PixelData = new byte[Width * Height * 4];
			stream.Read(PixelData);
		}

		public RgbaFile(uint width, uint height, byte[] pixelData)
		{
			MagicNumber = expectedMagic;
			Width = width;
			Height = height;
			PixelData = pixelData;
		}

		public void Save(string filename) => Save(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));

		public void Save(Stream stream)
		{
			WriteString(stream, MagicNumber);
			WriteUInt32(stream, Width);
			WriteUInt32(stream, Height);
			stream.Write(PixelData);
		}

		private static string ReadString(Stream stream, int length) => new(Enumerable.Range(0, length).Select(_ => (char)stream.ReadByte()).ToArray());
		private static uint ReadUInt32(Stream stream) => (uint)(((stream.ReadByte() & 0xFF) << 24) | ((stream.ReadByte() & 0xFF) << 16) | ((stream.ReadByte() & 0xFF) << 8) | ((stream.ReadByte() & 0xFF) << 0));

		private static void WriteString(Stream stream, string str) => Array.ForEach(str.ToCharArray(), (x) => stream.WriteByte((byte)x));
		private static void WriteUInt32(Stream stream, uint val) { stream.WriteByte((byte)((val >> 24) & 0xFF)); stream.WriteByte((byte)((val >> 16) & 0xFF)); stream.WriteByte((byte)((val >> 8) & 0xFF)); stream.WriteByte((byte)((val >> 0) & 0xFF)); }
	}
}
