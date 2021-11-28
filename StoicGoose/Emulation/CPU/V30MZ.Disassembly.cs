﻿using System.Linq;

using StoicGoose.Disassembly;

namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		readonly Disassembler disassembler = new();

		private void InitializeDisassembler()
		{
			disassembler.ReadDelegate = new Disassembly.MemoryReadDelegate(memoryReadDelegate);
			disassembler.WriteDelegate = new Disassembly.MemoryWriteDelegate(memoryWriteDelegate);
		}

		private string DisassembleInstruction(ushort cs, ushort ip)
		{
			disassembler.Segment = cs;
			disassembler.Offset = ip;
			var (bytes, disasm, comment) = disassembler.DisassembleInstruction();
			return $"{cs:X4}:{ip:X4} | {string.Join(" ", bytes.Select(x => ($"{x:X2}"))),-24} | {disasm,-32}{(!string.IsNullOrEmpty(comment) ? $";{comment}" : "")}";
		}
	}
}