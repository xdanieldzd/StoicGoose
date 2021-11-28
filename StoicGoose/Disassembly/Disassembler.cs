using System.Collections.Generic;
using System.Linq;

namespace StoicGoose.Disassembly
{
	public partial class Disassembler
	{
		public MemoryReadDelegate ReadDelegate { get; set; }
		public MemoryWriteDelegate WriteDelegate { get; set; }

		public ushort Segment { get; set; }
		public ushort Offset { get; set; }

		public void IncrementAddress()
		{
			IncrementAddress(1);
		}

		public void IncrementAddress(ushort inc)
		{
			if ((Offset + inc) < Offset) Segment++;
			Offset += inc;
		}

		public (byte[], string, string) DisassembleInstruction()
		{
			var bytes = ReadPrefixesAndOpcode();
			var (hasSegmentOverride, hasLockPrefix, hasRepeatPrefix, hasRepeatCmpSca, hasRepeatCmpScaNotEqual) = AnalyzePrefixes(bytes);
			var opcode = bytes.Last();
			var argsOffset = Offset;

			var disasm = default(string);
			var comment = default(string);

			if (basicOpcodeDict.ContainsKey(opcode))
			{
				disasm = basicOpcodeDict[opcode];
			}
			else if (conditionalJumpOpcodeDict.ContainsKey(opcode))
			{
				disasm = CreateConditionalJump(conditionalJumpOpcodeDict[opcode]);
				IncrementAddress();
			}
			else if (registerImmediate8OpcodeDict.ContainsKey(opcode))
			{
				disasm = CreateOpRegImm8(registerImmediate8OpcodeDict[opcode].op, registerImmediate8OpcodeDict[opcode].arg);
				IncrementAddress();
			}
			else if (registerImmediate16OpcodeDict.ContainsKey(opcode))
			{
				disasm = CreateOpRegImm16(registerImmediate16OpcodeDict[opcode].op, registerImmediate16OpcodeDict[opcode].arg);
				IncrementAddress(2);
			}
			else
			{
				switch (opcode)
				{
					case 0x00: ReadModRm(); disasm = CreateOpEbGb("add", hasSegmentOverride); break;
					case 0x01: ReadModRm(); disasm = CreateOpEwGw("add", hasSegmentOverride); break;
					case 0x02: ReadModRm(); disasm = CreateOpGbEb("add", hasSegmentOverride); break;
					case 0x03: ReadModRm(); disasm = CreateOpGwEw("add", hasSegmentOverride); break;

					case 0x08: ReadModRm(); disasm = CreateOpEbGb("or", hasSegmentOverride); break;
					case 0x09: ReadModRm(); disasm = CreateOpEwGw("or", hasSegmentOverride); break;
					case 0x0A: ReadModRm(); disasm = CreateOpGbEb("or", hasSegmentOverride); break;
					case 0x0B: ReadModRm(); disasm = CreateOpGwEw("or", hasSegmentOverride); break;

					case 0x10: ReadModRm(); disasm = CreateOpEbGb("adc", hasSegmentOverride); break;
					case 0x11: ReadModRm(); disasm = CreateOpEwGw("adc", hasSegmentOverride); break;
					case 0x12: ReadModRm(); disasm = CreateOpGbEb("adc", hasSegmentOverride); break;
					case 0x13: ReadModRm(); disasm = CreateOpGwEw("adc", hasSegmentOverride); break;

					case 0x18: ReadModRm(); disasm = CreateOpEbGb("sbb", hasSegmentOverride); break;
					case 0x19: ReadModRm(); disasm = CreateOpEwGw("sbb", hasSegmentOverride); break;
					case 0x1A: ReadModRm(); disasm = CreateOpGbEb("sbb", hasSegmentOverride); break;
					case 0x1B: ReadModRm(); disasm = CreateOpGwEw("sbb", hasSegmentOverride); break;

					case 0x20: ReadModRm(); disasm = CreateOpEbGb("and", hasSegmentOverride); break;
					case 0x21: ReadModRm(); disasm = CreateOpEwGw("and", hasSegmentOverride); break;
					case 0x22: ReadModRm(); disasm = CreateOpGbEb("and", hasSegmentOverride); break;
					case 0x23: ReadModRm(); disasm = CreateOpGwEw("and", hasSegmentOverride); break;

					case 0x28: ReadModRm(); disasm = CreateOpEbGb("sub", hasSegmentOverride); break;
					case 0x29: ReadModRm(); disasm = CreateOpEwGw("sub", hasSegmentOverride); break;
					case 0x2A: ReadModRm(); disasm = CreateOpGbEb("sub", hasSegmentOverride); break;
					case 0x2B: ReadModRm(); disasm = CreateOpGwEw("sub", hasSegmentOverride); break;

					case 0x30: ReadModRm(); disasm = CreateOpEbGb("xor", hasSegmentOverride); break;
					case 0x31: ReadModRm(); disasm = CreateOpEwGw("xor", hasSegmentOverride); break;
					case 0x32: ReadModRm(); disasm = CreateOpGbEb("xor", hasSegmentOverride); break;
					case 0x33: ReadModRm(); disasm = CreateOpGwEw("xor", hasSegmentOverride); break;

					case 0x38: ReadModRm(); disasm = CreateOpEbGb("cmp", hasSegmentOverride); break;
					case 0x39: ReadModRm(); disasm = CreateOpEwGw("cmp", hasSegmentOverride); break;
					case 0x3A: ReadModRm(); disasm = CreateOpGbEb("cmp", hasSegmentOverride); break;
					case 0x3B: ReadModRm(); disasm = CreateOpGwEw("cmp", hasSegmentOverride); break;

					case 0x62:
						ReadModRm();
						disasm = $"bound {registerNames16[modRm.Mem]}, word ptr {GetModRmPointer(hasSegmentOverride)}";
						break;

					case 0x68: disasm = $"push {GetImmediate16(0)}"; IncrementAddress(2); break;

					case 0x69:
						ReadModRm();
						disasm = $"{CreateOpGwEw("imul", hasSegmentOverride)}, {GetImmediate16(0)}";
						IncrementAddress(2);
						break;

					case 0x6A: disasm = $"push {GetImmediate8(0)}"; IncrementAddress(); break;

					case 0x6B:
						ReadModRm();
						disasm = $"{CreateOpGwEw("imul", hasSegmentOverride)}, {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0x6C: disasm = "ins byte ptr [di], dx"; break;
					case 0x6D: disasm = "ins word ptr [di], dx"; break;
					case 0x6E: disasm = "outs dx, byte ptr [di]"; break;
					case 0x6F: disasm = "outs dx, word ptr [di]"; break;

					case 0x80:
					case 0x82:
						ReadModRm();
						disasm = $"{group1Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0x81:
						ReadModRm();
						disasm = $"{group1Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate16(0)}";
						IncrementAddress(2);
						break;

					case 0x83:
						ReadModRm();
						disasm = $"{group1Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0x84: ReadModRm(); disasm = CreateOpGbEb("test", hasSegmentOverride); break;
					case 0x85: ReadModRm(); disasm = CreateOpGwEw("test", hasSegmentOverride); break;
					case 0x86: ReadModRm(); disasm = CreateOpGbEb("xchg", hasSegmentOverride); break;
					case 0x87: ReadModRm(); disasm = CreateOpGwEw("xchg", hasSegmentOverride); break;

					case 0x88:
						ReadModRm();
						disasm = $"mov {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}, {registerNames8[modRm.Reg]}";
						break;

					case 0x89:
						ReadModRm();
						disasm = $"mov {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}, {registerNames16[modRm.Reg]}";
						break;

					case 0x8A: ReadModRm(); disasm = CreateOpGbEb("mov", hasSegmentOverride); break;
					case 0x8B: ReadModRm(); disasm = CreateOpGwEw("mov", hasSegmentOverride); break;

					case 0x8C:
						ReadModRm();
						disasm = $"mov {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, {segmentNames[modRm.Reg]}";
						break;

					case 0x8D:
						ReadModRm();
						disasm = $"lea {registerNames16[modRm.Reg]}, {GetModRmPointer(hasSegmentOverride)}";
						break;

					case 0x8E:
						ReadModRm();
						disasm = $"mov {segmentNames[modRm.Reg]}, {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}";
						break;

					case 0x8F:
						ReadModRm();
						disasm = $"pop {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}";
						break;

					case 0x90: disasm = "nop"; break; // xchg ax,ax
					case 0x91: disasm = "xchg ax,cx"; break;
					case 0x92: disasm = "xchg ax,dx"; break;
					case 0x93: disasm = "xchg ax,bx"; break;
					case 0x94: disasm = "xchg ax,sp"; break;
					case 0x95: disasm = "xchg ax,bp"; break;
					case 0x96: disasm = "xchg ax,si"; break;
					case 0x97: disasm = "xchg ax,di"; break;

					case 0x9A: disasm = $"call {GetImmediate16(2)}:{GetImmediate16(0)}"; IncrementAddress(4); break;

					case 0xA0: disasm = $"mov al, byte ptr {GetOverrideSegmentName(hasSegmentOverride)}[{GetImmediate16(0)}]"; IncrementAddress(2); break;
					case 0xA1: disasm = $"mov ax, word ptr {GetOverrideSegmentName(hasSegmentOverride)}[{GetImmediate16(0)}]"; IncrementAddress(2); break;
					case 0xA2: disasm = $"mov byte ptr {GetOverrideSegmentName(hasSegmentOverride)}[{GetImmediate16(0)}], al"; IncrementAddress(2); break;
					case 0xA3: disasm = $"mov word ptr {GetOverrideSegmentName(hasSegmentOverride)}[{GetImmediate16(0)}], ax"; IncrementAddress(2); break;

					case 0xA4: disasm = "movsb"; break;
					case 0xA5: disasm = "movsw"; break;
					case 0xA6: disasm = "cmpsb"; break;
					case 0xA7: disasm = "cmpsw"; break;
					case 0xAA: disasm = "stosb"; break;
					case 0xAB: disasm = "stosw"; break;
					case 0xAC: disasm = "lodsb"; break;
					case 0xAD: disasm = "lodsw"; break;
					case 0xAE: disasm = "scasb"; break;
					case 0xAF: disasm = "scasw"; break;

					case 0xC0:
						ReadModRm();
						disasm = $"{group2Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0xC1:
						ReadModRm();
						disasm = $"{group2Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0xC2:
						disasm = $"ret {GetImmediate16(0)}";
						IncrementAddress(2);
						break;

					case 0xC4:
						ReadModRm();
						disasm = $"les {registerNames16[modRm.Reg]}, word ptr {GetModRmPointer(hasSegmentOverride)}";
						IncrementAddress(2);
						break;

					case 0xC5:
						ReadModRm();
						disasm = $"lds {registerNames16[modRm.Reg]}, word ptr {GetModRmPointer(hasSegmentOverride)}";
						IncrementAddress(2);
						break;

					case 0xC6:
						ReadModRm();
						disasm = $"mov {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0xC7:
						ReadModRm();
						disasm = $"mov {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, {GetImmediate16(0)}";
						IncrementAddress(2);
						break;

					case 0xC8:
						disasm = $"enter {GetImmediate16(0)}, {GetImmediate8(2)}";
						IncrementAddress(3);
						break;

					case 0xCA:
						disasm = $"retf {GetImmediate16(0)}";
						IncrementAddress(2);
						break;

					case 0xCD:
						disasm = $"int {GetImmediate8(0)}";
						IncrementAddress();
						break;

					case 0xD0:
						ReadModRm();
						disasm = $"{group2Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}, 1";
						break;

					case 0xD1:
						ReadModRm();
						disasm = $"{group2Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, 1";
						break;

					case 0xD2:
						ReadModRm();
						disasm = $"{group2Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}, cl";
						break;

					case 0xD3:
						ReadModRm();
						disasm = $"{group2Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}, cl";
						break;

					case 0xD4: disasm = $"aam {GetImmediate8(0)}"; IncrementAddress(); break;
					case 0xD5: disasm = $"aad {GetImmediate8(0)}"; IncrementAddress(); break;

					case 0xD7: disasm = "xlat byte ptr [bx]"; break;

					case 0xE0: disasm = CreateConditionalJump("loopnz"); IncrementAddress(); break;
					case 0xE1: disasm = CreateConditionalJump("loopz"); IncrementAddress(); break;
					case 0xE2: disasm = CreateConditionalJump("loop"); IncrementAddress(); break;
					case 0xE3: disasm = CreateConditionalJump("jcxz"); IncrementAddress(); break;

					case 0xE4: disasm = $"in al, {GetImmediate8(0)}"; comment = wonderSwanPortNamesInput[ReadMemory8(Segment, Offset)]; IncrementAddress(); break;
					case 0xE5: disasm = $"in ax, {GetImmediate8(0)}"; comment = wonderSwanPortNamesInput[ReadMemory8(Segment, Offset)]; IncrementAddress(); break;
					case 0xE6: disasm = $"out {GetImmediate8(0)}, al"; comment = wonderSwanPortNamesOutput[ReadMemory8(Segment, Offset)]; IncrementAddress(); break;
					case 0xE7: disasm = $"out {GetImmediate8(0)}, ax"; comment = wonderSwanPortNamesOutput[ReadMemory8(Segment, Offset)]; IncrementAddress(); break;

					case 0xE8:
						disasm = $"call 0x{Offset + 2 + (short)ReadMemory16(Segment, Offset):X4}";
						IncrementAddress(2);
						break;

					case 0xE9:
						disasm = $"jmp 0x{Offset + 2 + (short)ReadMemory16(Segment, Offset):X4}";
						IncrementAddress(2);
						break;

					case 0xEA: disasm = $"jmp {GetImmediate16(2)}:{GetImmediate16(0)}"; IncrementAddress(4); break;
					case 0xEB: disasm = CreateConditionalJump("jmp"); IncrementAddress(); break;

					case 0xF6:
						ReadModRm();
						disasm = $"{group3Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}";
						if (modRm.Reg == 0x0)
						{
							disasm = $"{disasm}, {GetImmediate8(0)}";
							IncrementAddress();
						}
						break;

					case 0xF7:
						ReadModRm();
						disasm = $"{group3Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}";
						if (modRm.Reg == 0x0)
						{
							disasm = $"{disasm}, {GetImmediate16(0)}";
							IncrementAddress();
						}
						break;

					case 0xFE:
						ReadModRm();
						disasm = $"{group4Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"byte ptr {GetModRmPointer(hasSegmentOverride)}")}";
						break;

					case 0xFF:
						// TODO: verify validity checks?
						var tempModRm = (ModRm)ReadMemory8(Segment, Offset);
						if (tempModRm.Reg != 0x7 && !((tempModRm.Reg == 0x3 || tempModRm.Reg == 0x5) && tempModRm.Mod == ModRmModes.Register))
						{
							ReadModRm();
							if (modRm.Reg == 0x3 || modRm.Reg == 0x5)
								disasm = $"{group4Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"dword ptr {GetModRmPointer(hasSegmentOverride)}")}";
							else
								disasm = $"{group4Opcodes[modRm.Reg]} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"word ptr {GetModRmPointer(hasSegmentOverride)}")}";
						}
						break;
				}
			}

			if (!string.IsNullOrEmpty(disasm))
			{
				const int padding = 0;

				var seperatorIndex = disasm.IndexOf(' ');
				if (seperatorIndex != -1)
					disasm = $"{disasm.Substring(0, seperatorIndex),-padding}{disasm[seperatorIndex..]}";

				if (hasLockPrefix)
					disasm = $"lock {disasm}";

				if (hasRepeatPrefix)
				{
					if (hasRepeatCmpSca && !hasRepeatCmpScaNotEqual) disasm = $"repe: {disasm}";
					else if (hasRepeatCmpSca && hasRepeatCmpScaNotEqual) disasm = $"repne: {disasm}";
					else disasm = $"rep: {disasm}";
				}
			}
			else
			{
				disasm = "???";
			}

			var argsBytes = new List<byte>();
			for (var i = argsOffset; i < Offset; i++) argsBytes.Add(ReadMemory8(Segment, i));

			return (bytes.Concat(argsBytes).ToArray(), disasm, comment);
		}

		private string GetImmediate8(int offset2) => $"0x{ReadMemory8(Segment, (ushort)(Offset + offset2)):X2}";
		private string GetImmediate16(int offset2) => $"0x{ReadMemory16(Segment, (ushort)(Offset + offset2)):X4}";

		private string CreateConditionalJump(string jump) => $"{jump} 0x{Offset + 1 + (sbyte)ReadMemory8(Segment, Offset):X4}";
		private string CreateOpRegImm8(string op, string arg1) => $"{op} {arg1}, {GetImmediate8(0)}";
		private string CreateOpRegImm16(string op, string arg1) => $"{op} {arg1}, {GetImmediate16(0)}";

		private string CreateOpGwEw(string op, SegmentNumber hasSegmentOverride) => $"{op} {registerNames16[modRm.Reg]}, {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}";
		private string CreateOpEwGw(string op, SegmentNumber hasSegmentOverride) => $"{op} {(modRm.Mod == ModRmModes.Register ? registerNames16[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}, {registerNames16[modRm.Reg]}";
		private string CreateOpGbEb(string op, SegmentNumber hasSegmentOverride) => $"{op} {registerNames8[modRm.Reg]}, {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}";
		private string CreateOpEbGb(string op, SegmentNumber hasSegmentOverride) => $"{op} {(modRm.Mod == ModRmModes.Register ? registerNames8[modRm.Mem] : $"{GetModRmPointer(hasSegmentOverride)}")}, {registerNames8[modRm.Reg]}";
	}
}
