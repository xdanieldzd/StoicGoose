using System.Linq;

using StoicGoose.Disassembly;

namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		readonly Disassembler disassembler = new();

		private void InitializeDisassembler()
		{
			disassembler.ReadDelegate = new MemoryReadDelegate(memoryReadDelegate);
			disassembler.WriteDelegate = new MemoryWriteDelegate(memoryWriteDelegate);
		}

		private string DisassembleInstruction(ushort cs, ushort ip)
		{
			var (_, _, bytes, disasm, comment) = disassembler.DisassembleInstruction(cs, ip);
			return $"{cs:X4}:{ip:X4} | {string.Join(" ", bytes.Select(x => ($"{x:X2}"))),-24} | {disasm,-32}{(!string.IsNullOrEmpty(comment) ? $";{comment}" : "")}";
		}
	}
}
