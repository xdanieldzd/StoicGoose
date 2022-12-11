using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		Action[] instructions = default;

		private void GenerateInstructionHandlers()
		{
			instructions = new Action[256];

			instructions[0x00] = () => { };



			instructions[0x00] = () => { Wait(1); modRM = Fetch8(); SetMemory8(ADD(GetMemory8(), GetRegister8())); };
			instructions[0x01] = () => { Wait(1); modRM = Fetch8(); SetMemory16(ADD(GetMemory16(), GetRegister16())); };
			instructions[0x02] = () => { Wait(1); modRM = Fetch8(); SetRegister8(ADD(GetRegister8(), GetMemory8())); };
			instructions[0x03] = () => { Wait(1); modRM = Fetch8(); SetRegister16(ADD(GetRegister16(), GetMemory16())); };
			instructions[0x04] = () => { Wait(1); aw.Low = ADD(aw.Low, Fetch8()); };
			instructions[0x05] = () => { Wait(1); aw.Word = ADD(aw.Word, Fetch16()); };
			instructions[0x06] = () => { Wait(1); PUSH(ds1); };
			instructions[0x07] = () => { Wait(2); ds1 = POP(); };
			instructions[0x08] = () => { Wait(1); modRM = Fetch8(); SetMemory8(OR(GetMemory8(), GetRegister8())); };
			instructions[0x09] = () => { Wait(1); modRM = Fetch8(); SetMemory16(OR(GetMemory16(), GetRegister16())); };
			instructions[0x0A] = () => { Wait(1); modRM = Fetch8(); SetRegister8(OR(GetRegister8(), GetMemory8())); };
			instructions[0x0B] = () => { Wait(1); modRM = Fetch8(); SetRegister16(OR(GetRegister16(), GetMemory16())); };
			instructions[0x0C] = () => { Wait(1); aw.Low = OR(aw.Low, Fetch8()); };
			instructions[0x0D] = () => { Wait(1); aw.Word = OR(aw.Word, Fetch16()); };
			instructions[0x0E] = () => { Wait(1); PUSH(ps); };
			instructions[0x0F] = () => { Wait(2); ps = POP(); };
			instructions[0x10] = () => { Wait(1); modRM = Fetch8(); SetMemory8(ADDC(GetMemory8(), GetRegister8())); };
			instructions[0x11] = () => { Wait(1); modRM = Fetch8(); SetMemory16(ADDC(GetMemory16(), GetRegister16())); };
			instructions[0x12] = () => { Wait(1); modRM = Fetch8(); SetRegister8(ADDC(GetRegister8(), GetMemory8())); };
			instructions[0x13] = () => { Wait(1); modRM = Fetch8(); SetRegister16(ADDC(GetRegister16(), GetMemory16())); };
			instructions[0x14] = () => { Wait(1); aw.Low = ADDC(aw.Low, Fetch8()); };
			instructions[0x15] = () => { Wait(1); aw.Word = ADDC(aw.Word, Fetch16()); };
			instructions[0x16] = () => { Wait(1); PUSH(ss); };
			instructions[0x17] = () => { Wait(2); ss = POP(); };
			instructions[0x18] = () => { Wait(1); modRM = Fetch8(); SetMemory8(SUBC(GetMemory8(), GetRegister8())); };
			instructions[0x19] = () => { Wait(1); modRM = Fetch8(); SetMemory16(SUBC(GetMemory16(), GetRegister16())); };
			instructions[0x1A] = () => { Wait(1); modRM = Fetch8(); SetRegister8(SUBC(GetRegister8(), GetMemory8())); };
			instructions[0x1B] = () => { Wait(1); modRM = Fetch8(); SetRegister16(SUBC(GetRegister16(), GetMemory16())); };
			instructions[0x1C] = () => { Wait(1); aw.Low = SUBC(aw.Low, Fetch8()); };
			instructions[0x1D] = () => { Wait(1); aw.Word = SUBC(aw.Word, Fetch16()); };
			instructions[0x1E] = () => { Wait(1); PUSH(ds0); };
			instructions[0x1F] = () => { Wait(2); ds0 = POP(); };
			instructions[0x20] = () => { Wait(1); modRM = Fetch8(); SetMemory8(AND(GetMemory8(), GetRegister8())); };
			instructions[0x21] = () => { Wait(1); modRM = Fetch8(); SetMemory16(AND(GetMemory16(), GetRegister16())); };
			instructions[0x22] = () => { Wait(1); modRM = Fetch8(); SetRegister8(AND(GetRegister8(), GetMemory8())); };
			instructions[0x23] = () => { Wait(1); modRM = Fetch8(); SetRegister16(AND(GetRegister16(), GetMemory16())); };
			instructions[0x24] = () => { Wait(1); aw.Low = ADD(aw.Low, Fetch8()); };
			instructions[0x25] = () => { Wait(1); aw.Word = ADD(aw.Word, Fetch16()); };
			instructions[0x26] = () => { EnqueuePrefix(); };
			instructions[0x27] = () => { Wait(10); ADJ4x(false); };
			instructions[0x28] = () => { Wait(1); modRM = Fetch8(); SetMemory8(SUB(GetMemory8(), GetRegister8())); };
			instructions[0x29] = () => { Wait(1); modRM = Fetch8(); SetMemory16(SUB(GetMemory16(), GetRegister16())); };
			instructions[0x2A] = () => { Wait(1); modRM = Fetch8(); SetRegister8(SUB(GetRegister8(), GetMemory8())); };
			instructions[0x2B] = () => { Wait(1); modRM = Fetch8(); SetRegister16(SUB(GetRegister16(), GetMemory16())); };
			instructions[0x2C] = () => { Wait(1); aw.Low = SUB(aw.Low, Fetch8()); };
			instructions[0x2D] = () => { Wait(1); aw.Word = SUB(aw.Word, Fetch16()); };
			instructions[0x2E] = () => { EnqueuePrefix(); };
			instructions[0x2F] = () => { Wait(10); ADJ4x(true); };
			instructions[0x30] = () => { Wait(1); modRM = Fetch8(); SetMemory8(XOR(GetMemory8(), GetRegister8())); };
			instructions[0x31] = () => { Wait(1); modRM = Fetch8(); SetMemory16(XOR(GetMemory16(), GetRegister16())); };
			instructions[0x32] = () => { Wait(1); modRM = Fetch8(); SetRegister8(XOR(GetRegister8(), GetMemory8())); };
			instructions[0x33] = () => { Wait(1); modRM = Fetch8(); SetRegister16(XOR(GetRegister16(), GetMemory16())); };
			instructions[0x34] = () => { Wait(1); aw.Low = XOR(aw.Low, Fetch8()); };
			instructions[0x35] = () => { Wait(1); aw.Word = XOR(aw.Word, Fetch16()); };
			instructions[0x36] = () => { EnqueuePrefix(); };
			instructions[0x37] = () => { Wait(9); ADJBx(false); };
			instructions[0x38] = () => { Wait(1); modRM = Fetch8(); SUB(GetMemory8(), GetRegister8()); };
			instructions[0x39] = () => { Wait(1); modRM = Fetch8(); SUB(GetMemory16(), GetRegister16()); };
			instructions[0x3A] = () => { Wait(1); modRM = Fetch8(); SUB(GetRegister8(), GetMemory8()); };
			instructions[0x3B] = () => { Wait(1); modRM = Fetch8(); SUB(GetRegister16(), GetMemory16()); };
			instructions[0x3C] = () => { Wait(1); SUB(aw.Low, Fetch8()); };
			instructions[0x3D] = () => { Wait(1); SUB(aw.Word, Fetch16()); };
			instructions[0x3E] = () => { EnqueuePrefix(); };
			instructions[0x3F] = () => { Wait(9); ADJBx(true); };
			instructions[0x40] = () => { Wait(1); aw.Word = INC(aw.Word); };
			instructions[0x41] = () => { Wait(1); cw.Word = INC(cw.Word); };
			instructions[0x42] = () => { Wait(1); dw.Word = INC(dw.Word); };
			instructions[0x43] = () => { Wait(1); bw.Word = INC(bw.Word); };
			instructions[0x44] = () => { Wait(1); sp = INC(sp); };
			instructions[0x45] = () => { Wait(1); bp = INC(bp); };
			instructions[0x46] = () => { Wait(1); ix = INC(ix); };
			instructions[0x47] = () => { Wait(1); iy = INC(iy); };
			instructions[0x48] = () => { Wait(1); aw.Word = DEC(aw.Word); };
			instructions[0x49] = () => { Wait(1); cw.Word = DEC(cw.Word); };
			instructions[0x4A] = () => { Wait(1); dw.Word = DEC(dw.Word); };
			instructions[0x4B] = () => { Wait(1); bw.Word = DEC(bw.Word); };
			instructions[0x4C] = () => { Wait(1); sp = DEC(sp); };
			instructions[0x4D] = () => { Wait(1); bp = DEC(bp); };
			instructions[0x4E] = () => { Wait(1); ix = DEC(ix); };
			instructions[0x4F] = () => { Wait(1); iy = DEC(iy); };
			instructions[0x50] = () => { PUSH(aw.Word); };
			instructions[0x51] = () => { PUSH(cw.Word); };
			instructions[0x52] = () => { PUSH(dw.Word); };
			instructions[0x53] = () => { PUSH(bw.Word); };
			instructions[0x54] = () => { PUSH(sp); };
			instructions[0x55] = () => { PUSH(bp); };
			instructions[0x56] = () => { PUSH(ix); };
			instructions[0x57] = () => { PUSH(iy); };
			instructions[0x58] = () => { aw.Word = POP(); };
			instructions[0x59] = () => { cw.Word = POP(); };
			instructions[0x5A] = () => { dw.Word = POP(); };
			instructions[0x5B] = () => { bw.Word = POP(); };
			instructions[0x5C] = () => { sp = POP(); };
			instructions[0x5D] = () => { bp = POP(); };
			instructions[0x5E] = () => { ix = POP(); };
			instructions[0x5F] = () => { iy = POP(); };
			instructions[0x60] = () => { Wait(1); var sp = this.sp; PUSH(aw.Word); PUSH(cw.Word); PUSH(dw.Word); PUSH(bw.Word); PUSH(sp); PUSH(bp); PUSH(ix); PUSH(iy); };
			instructions[0x61] = () => { Wait(1); iy = POP(); ix = POP(); bp = POP(); POP(); bw.Word = POP(); dw.Word = POP(); cw.Word = POP(); aw.Word = POP(); };
			instructions[0x62] = () => { Wait(12); modRM = Fetch8(); CHKIND(); };
			instructions[0x63] = () => { }; /* invalid */
			instructions[0x64] = () => { }; /* REPNC -- not supported */
			instructions[0x65] = () => { }; /* REPC -- not supported */
			instructions[0x66] = () => { }; /* FPO2 -- not supported */
			instructions[0x67] = () => { }; /* FPO2 -- not supported */
			instructions[0x68] = () => { PUSH(Fetch16()); };
			instructions[0x69] = () => { Wait(5); modRM = Fetch8(); SetRegister16((ushort)MUL((short)GetMemory16(), (short)Fetch16())); };
			instructions[0x6A] = () => { PUSH(Fetch8()); };
			instructions[0x6B] = () => { Wait(5); modRM = Fetch8(); SetRegister16((ushort)MUL((short)GetMemory16(), (sbyte)Fetch8())); };
			instructions[0x6C] = () => { Wait(5); INM8(); };
			instructions[0x6D] = () => { Wait(5); INM16(); };
			instructions[0x6E] = () => { Wait(6); OUTM8(); };
			instructions[0x6F] = () => { Wait(6); OUTM16(); };
			instructions[0x70] = () => { BranchIf(psw.Overflow); };
			instructions[0x71] = () => { BranchIf(!psw.Overflow); };
			instructions[0x72] = () => { BranchIf(psw.Carry); };
			instructions[0x73] = () => { BranchIf(!psw.Carry); };
			instructions[0x74] = () => { BranchIf(psw.Zero); };
			instructions[0x75] = () => { BranchIf(!psw.Zero); };
			instructions[0x76] = () => { BranchIf(psw.Carry || psw.Zero); };
			instructions[0x77] = () => { BranchIf(!psw.Carry && !psw.Zero); };
			instructions[0x78] = () => { BranchIf(psw.Sign); };
			instructions[0x79] = () => { BranchIf(!psw.Sign); };
			instructions[0x7A] = () => { BranchIf(psw.Parity); };
			instructions[0x7B] = () => { BranchIf(!psw.Parity); };
			instructions[0x7C] = () => { BranchIf(!psw.Zero && psw.Sign != psw.Overflow); };
			instructions[0x7D] = () => { BranchIf(psw.Zero || psw.Sign == psw.Overflow); };
			instructions[0x7E] = () => { BranchIf(psw.Zero || psw.Sign != psw.Overflow); };
			instructions[0x7F] = () => { BranchIf(!psw.Zero && psw.Sign == psw.Overflow); };
			//80-83 group
			instructions[0x84] = () => { Wait(1); modRM = Fetch8(); AND(GetMemory8(), GetRegister8()); };
			instructions[0x85] = () => { Wait(1); modRM = Fetch8(); AND(GetMemory16(), GetRegister16()); };
			instructions[0x86] = () => { Wait(3); modRM = Fetch8(); var mem = GetMemory8(); var reg = GetRegister8(); SetMemory8(reg); SetRegister8(mem); };
			instructions[0x87] = () => { Wait(3); modRM = Fetch8(); var mem = GetMemory16(); var reg = GetRegister16(); SetMemory16(reg); SetRegister16(mem); };
			instructions[0x88] = () => { modRM = Fetch8(); if (modRM.Mod == 0b11) Wait(1); SetMemory8(GetRegister8()); };
			instructions[0x89] = () => { modRM = Fetch8(); if (modRM.Mod == 0b11) Wait(1); SetMemory16(GetRegister16()); };
			instructions[0x8A] = () => { modRM = Fetch8(); if (modRM.Mod == 0b11) Wait(1); SetRegister8(GetMemory8()); };
			instructions[0x8B] = () => { modRM = Fetch8(); if (modRM.Mod == 0b11) Wait(1); SetRegister16(GetMemory16()); };
			instructions[0x8C] = () => { Wait(1); modRM = Fetch8(); SetMemory16(GetSegment()); };
			instructions[0x8D] = () => { Wait(1); modRM = Fetch8(); SetRegister16(modRM.Mod == 0b11 ? GetRegister16() : modRM.Address); };
			instructions[0x8E] = () => { Wait(2); modRM = Fetch8(); SetSegment(GetMemory16()); };
			instructions[0x8F] = () => { Wait(1); modRM = Fetch8(); SetMemory16(POP()); };
			instructions[0x90] = () => { Wait(3); };
			instructions[0x91] = () => { Wait(3); (aw.Word, cw.Word) = (cw.Word, aw.Word); };
			instructions[0x92] = () => { Wait(3); (aw.Word, dw.Word) = (dw.Word, aw.Word); };
			instructions[0x93] = () => { Wait(3); (aw.Word, bw.Word) = (bw.Word, aw.Word); };
			instructions[0x94] = () => { Wait(3); (aw.Word, sp) = (sp, aw.Word); };
			instructions[0x95] = () => { Wait(3); (aw.Word, bp) = (bp, aw.Word); };
			instructions[0x96] = () => { Wait(3); (aw.Word, ix) = (ix, aw.Word); };
			instructions[0x97] = () => { Wait(3); (aw.Word, iy) = (iy, aw.Word); };
			instructions[0x98] = () => { Wait(1); aw.High = (byte)((aw.Low & 0x80) == 0x80 ? 0xFF : 0x00); };
			instructions[0x99] = () => { Wait(1); dw.Word = (ushort)((aw.Word & 0x8000) == 0x8000 ? 0xFFFF : 0x0000); };
			instructions[0x9A] = () => { Wait(9); var pc = Fetch16(); var ps = Fetch16(); PUSH(ps); PUSH(pc); this.pc = pc; this.ps = ps; Flush(); };
			instructions[0x9B] = () => { Wait(1); };
			instructions[0x9C] = () => { Wait(1); PUSH(psw.Value); };
			instructions[0x9D] = () => { Wait(2); psw.Value = POP(); };
			instructions[0x9E] = () => { Wait(4); psw = (ushort)((psw.Value & 0xFF00) | aw.High); };
			instructions[0x9F] = () => { Wait(2); aw.High = (byte)(psw.Value & 0x00FF); };
			instructions[0xA0] = () => { aw.Low = ReadMemory8(SegmentViaPrefix(ds0), Fetch16()); };
			instructions[0xA1] = () => { aw.Word = ReadMemory16(SegmentViaPrefix(ds0), Fetch16()); };
			instructions[0xA2] = () => { WriteMemory8(SegmentViaPrefix(ds0), Fetch16(), aw.Low); };
			instructions[0xA3] = () => { WriteMemory16(SegmentViaPrefix(ds0), Fetch16(), aw.Word); };
			instructions[0xA4] = () => { Wait(3); MOVBK8(); };
			instructions[0xA5] = () => { Wait(3); MOVBK16(); };

			instructions[0xA8] = () => { Wait(1); AND(aw.Low, Fetch8()); };
			instructions[0xA9] = () => { Wait(1); AND(aw.Word, Fetch16()); };

			instructions[0xB0] = () => { Wait(1); aw.Low = Fetch8(); };
			instructions[0xB1] = () => { Wait(1); cw.Low = Fetch8(); };
			instructions[0xB2] = () => { Wait(1); dw.Low = Fetch8(); };
			instructions[0xB3] = () => { Wait(1); bw.Low = Fetch8(); };
			instructions[0xB4] = () => { Wait(1); aw.High = Fetch8(); };
			instructions[0xB5] = () => { Wait(1); cw.High = Fetch8(); };
			instructions[0xB6] = () => { Wait(1); dw.High = Fetch8(); };
			instructions[0xB7] = () => { Wait(1); bw.High = Fetch8(); };
			instructions[0xB8] = () => { Wait(1); aw.Word = Fetch16(); };
			instructions[0xB9] = () => { Wait(1); cw.Word = Fetch16(); };
			instructions[0xBA] = () => { Wait(1); dw.Word = Fetch16(); };
			instructions[0xBB] = () => { Wait(1); bw.Word = Fetch16(); };
			instructions[0xBC] = () => { Wait(1); sp = Fetch16(); };
			instructions[0xBD] = () => { Wait(1); bp = Fetch16(); };
			instructions[0xBE] = () => { Wait(1); ix = Fetch16(); };
			instructions[0xBF] = () => { Wait(1); iy = Fetch16(); };

			instructions[0xC6] = () => { modRM = Fetch8(); SetMemory8(Fetch8()); };
			instructions[0xC7] = () => { modRM = Fetch8(); SetMemory16(Fetch16()); };
			instructions[0xC8] = () => { PREPARE(); };
			instructions[0xC9] = () => { DISPOSE(); };
			instructions[0xCA] = () => { Wait(8); var offset = Fetch16(); pc = POP(); ps = POP(); sp += offset; Flush(); };
			instructions[0xCB] = () => { Wait(7); pc = POP(); ps = POP(); Flush(); };
			instructions[0xCC] = () => { Wait(8); Interrupt(3); };
			instructions[0xCD] = () => { Wait(9); Interrupt(Fetch8()); };
			instructions[0xCE] = () => { Wait(5); if (psw.Overflow) Interrupt(4); };
			instructions[0xCF] = () => { Wait(9); pc = POP(); ps = POP(); psw.Value = POP(); Flush(); };

			instructions[0xD4] = () => { Wait(17); CVTBD(Fetch8()); };
			instructions[0xD5] = () => { Wait(6); CVTDB(Fetch8()); };
			instructions[0xD6] = () => { }; // TODO: really invalid on V30MZ? undocumented mirror of TRANS/XLAT otherwise
			instructions[0xD7] = () => { Wait(4); aw.Low = ReadMemory8(SegmentViaPrefix(ds0), (ushort)(bw.Word + aw.Low)); };
			instructions[0xD8] = () => { }; /* FPO1 -- not supported */
			instructions[0xD9] = () => { }; /* "" */
			instructions[0xDA] = () => { }; /* "" */
			instructions[0xDB] = () => { }; /* "" */
			instructions[0xDC] = () => { }; /* "" */
			instructions[0xDD] = () => { }; /* "" */
			instructions[0xDE] = () => { }; /* "" */
			instructions[0xDF] = () => { }; /* "" */

			instructions[0xE4] = () => { Wait(6); aw.Low = ReadPort8(Fetch8()); };
			instructions[0xE5] = () => { Wait(6); aw.Word = ReadPort16(Fetch8()); };
			instructions[0xE6] = () => { Wait(6); WritePort8(Fetch8(), aw.Low); };
			instructions[0xE7] = () => { Wait(6); WritePort16(Fetch8(), aw.Word); };
			instructions[0xE8] = () => { Wait(4); var offset = (short)Fetch16(); PUSH(PC); pc = (ushort)(pc + offset); Flush(); };
			instructions[0xE9] = () => { Wait(3); pc = (ushort)(pc + (short)Fetch16()); Flush(); };
			instructions[0xEA] = () => { Wait(6); var pc = Fetch16(); var ps = Fetch16(); this.pc = pc; this.ps = ps; Flush(); };
			instructions[0xEB] = () => { Wait(3); var offset = (sbyte)Fetch8(); PUSH(PC); pc = (ushort)(pc + offset); Flush(); };
			instructions[0xEC] = () => { Wait(4); aw.Low = ReadPort8(dw.Word); };
			instructions[0xED] = () => { Wait(4); aw.Word = ReadPort16(dw.Word); };
			instructions[0xEE] = () => { Wait(4); WritePort8(dw.Word, aw.Low); };
			instructions[0xEF] = () => { Wait(4); WritePort16(dw.Word, aw.Word); };
			instructions[0xF0] = () => { EnqueuePrefix(); };
			instructions[0xF1] = () => { }; /* invalid */
			instructions[0xF2] = () => { Wait(4); EnqueuePrefix(); };
			instructions[0xF3] = () => { Wait(4); EnqueuePrefix(); };
			instructions[0xF4] = () => { Wait(8); isHalted = true; };
			instructions[0xF5] = () => { Wait(4); psw.Carry = !psw.Carry; };

			instructions[0xF8] = () => { Wait(4); psw.Carry = false; };
			instructions[0xF9] = () => { Wait(4); psw.Carry = true; };
			instructions[0xFA] = () => { Wait(4); psw.InterruptEnable = false; };
			instructions[0xFB] = () => { Wait(4); psw.InterruptEnable = true; };
			instructions[0xFC] = () => { Wait(4); psw.Direction = false; };
			instructions[0xFD] = () => { Wait(4); psw.Direction = true; };

			//FF
		}
	}
}
