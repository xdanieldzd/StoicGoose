using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using StoicGoose.Common;

namespace StoicGoose.Handlers
{
	public sealed class DatabaseHandler
	{
		readonly Dictionary<string, DatFile> datFiles = new();

		public DatabaseHandler(string directory)
		{
			foreach (var file in Directory.EnumerateFiles(directory, "*.dat").OrderBy(x => x.Length))
			{
				XmlRootAttribute root = new("datafile") { IsNullable = true };
				XmlSerializer serializer = new(typeof(DatFile), root);
				using FileStream stream = new(Path.Combine(directory, file), FileMode.Open);
				datFiles.Add(Path.GetFileName(file), (DatFile)serializer.Deserialize(stream));
			}

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, $"Loaded {datFiles.Count} .dat file(s) with {datFiles.Sum(x => x.Value.Game.Length)} known game(s).");
			foreach (var datFile in datFiles.Select(x => x.Value))
				Console.WriteLine($" '{datFile.Header.Name} ({datFile.Header.Version})' from {datFile.Header.Homepage}");
		}

		private DatGame GetGame(uint romCrc32, int romSize)
		{
			return datFiles.Select(x => x.Value.Game).Select(x => x.FirstOrDefault(x => x.Rom.Any(y => y.Crc.ToLowerInvariant() == $"{romCrc32:x8}" && y.Size.ToLowerInvariant() == $"{romSize:D}"))).FirstOrDefault(x => x != null);
		}

		public string GetGameTitle(uint romCrc32, int romSize)
		{
			return GetGame(romCrc32, romSize)?.Name.Replace("&", "&&") ?? "unrecognized game";
		}

		public class DatHeader
		{
			[XmlElement("name")]
			public string Name { get; set; }
			[XmlElement("description")]
			public string Description { get; set; }
			[XmlElement("category")]
			public string Category { get; set; }
			[XmlElement("version")]
			public string Version { get; set; }
			[XmlElement("date")]
			public string Date { get; set; }
			[XmlElement("author")]
			public string Author { get; set; }
			[XmlElement("email")]
			public string Email { get; set; }
			[XmlElement("homepage")]
			public string Homepage { get; set; }
			[XmlElement("url")]
			public string Url { get; set; }
			[XmlElement("comment")]
			public string Comment { get; set; }
		}

		public class DatRelease
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("region")]
			public string Region { get; set; }
			[XmlAttribute("language")]
			public string Language { get; set; }
			[XmlAttribute("date")]
			public string Date { get; set; }
			[XmlAttribute("default")]
			public string Default { get; set; }
		}

		public class DatBiosSet
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("description")]
			public string Description { get; set; }
			[XmlAttribute("default")]
			public string Default { get; set; }
		}

		public class DatRom
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("size")]
			public string Size { get; set; }
			[XmlAttribute("crc")]
			public string Crc { get; set; }
			[XmlAttribute("sha1")]
			public string Sha1 { get; set; }
			[XmlAttribute("md5")]
			public string Md5 { get; set; }
			[XmlAttribute("merge")]
			public string Merge { get; set; }
			[XmlAttribute("status")]
			public string Status { get; set; }
			[XmlAttribute("date")]
			public string Date { get; set; }
		}

		public class DatDisk
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("sha1")]
			public string Sha1 { get; set; }
			[XmlAttribute("md5")]
			public string Md5 { get; set; }
			[XmlAttribute("merge")]
			public string Merge { get; set; }
			[XmlAttribute("status")]
			public string Status { get; set; }
		}

		public class DatSample
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		public class DatArchive
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
		}

		public class DatGame
		{
			[XmlAttribute("name")]
			public string Name { get; set; }
			[XmlAttribute("sourcefile")]
			public string SourceFile { get; set; }
			[XmlAttribute("isbios")]
			public string IsBios { get; set; }
			[XmlAttribute("cloneof")]
			public string CloneOf { get; set; }
			[XmlAttribute("romof")]
			public string RomOf { get; set; }
			[XmlAttribute("sampleof")]
			public string SampleOf { get; set; }
			[XmlAttribute("board")]
			public string Board { get; set; }
			[XmlAttribute("rebuildto")]
			public string RebuildTo { get; set; }

			[XmlElement("year")]
			public string Year { get; set; }
			[XmlElement("manufacturer")]
			public string Manufacturer { get; set; }

			[XmlElement("release")]
			public DatRelease[] Release { get; set; }

			[XmlElement("biosset")]
			public DatBiosSet[] BiosSet { get; set; }

			[XmlElement("rom")]
			public DatRom[] Rom { get; set; }

			[XmlElement("disk")]
			public DatDisk[] Disk { get; set; }

			[XmlElement("sample")]
			public DatSample[] Sample { get; set; }

			[XmlElement("archive")]
			public DatArchive[] Archive { get; set; }
		}

		[Serializable()]
		public class DatFile
		{
			[XmlElement("header")]
			public DatHeader Header { get; set; }

			[XmlElement("game")]
			public DatGame[] Game { get; set; }
		}
	}
}
