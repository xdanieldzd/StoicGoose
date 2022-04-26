using System.Collections.Generic;
using System.Linq;

using Gee.External.Capstone;
using Gee.External.Capstone.X86;

using StoicGoose.Common.Console;
using StoicGoose.Core;

namespace StoicGoose.GLWindow.Debugging
{
	public sealed partial class Disassembler
	{
		const int segmentSize = 0x10000;

		readonly CapstoneX86Disassembler disassembler = default;
		readonly Dictionary<ushort, List<Instruction>> disassembledSegmentCache = new();

		public MemoryReadDelegate ReadDelegate { get; set; }

		readonly static Disassembler instance = new();
		public static Disassembler Instance => instance;

		static Disassembler() { }

		Disassembler()
		{
			disassembler = CapstoneDisassembler.CreateX86Disassembler(X86DisassembleMode.Bit16);
			disassembler.DisassembleSyntax = DisassembleSyntax.Intel;
			disassembler.EnableInstructionDetails = true;
			disassembler.EnableSkipDataMode = true;

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Disassembler initialized.");
		}

		~Disassembler()
		{
			disassembler.Dispose();
		}

		public void Reset()
		{
			disassembledSegmentCache.Clear();

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Disassembler reset.");
		}

		public List<Instruction> GetCachedSegment(ushort segment)
		{
			if (disassembledSegmentCache.ContainsKey(segment))
				return disassembledSegmentCache[segment];
			else
				return null;
		}

		public List<Instruction> DisassembleSegment(ushort segment)
		{
			var instructions = new List<Instruction>();

			var data = new byte[segmentSize];
			for (var i = 0; i < data.Length; i++) data[i] = ReadDelegate((uint)((segment << 4) + i));

			var result = disassembler.Disassemble(data, 0);
			instructions.AddRange(result.Select(x => new Instruction(x)));

			disassembledSegmentCache.Add(segment, instructions);

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, $"Disassembled segment 0x{segment:X4}, {instructions.Count} instruction(s).");

			return instructions;
		}
	}
}
