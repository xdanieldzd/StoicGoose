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
			instructions[0x06] = () => { }; // push ds1
			instructions[0x07] = () => { }; // pop ds1

			instructions[0x0E] = () => { }; // push ps
			instructions[0x0F] = () => { }; // pop ps
			instructions[0x10] = () => { Wait(1); modRM = Fetch8(); SetMemory8(ADDC(GetMemory8(), GetRegister8())); };
			instructions[0x11] = () => { Wait(1); modRM = Fetch8(); SetMemory16(ADDC(GetMemory16(), GetRegister16())); };
			instructions[0x12] = () => { Wait(1); modRM = Fetch8(); SetRegister8(ADDC(GetRegister8(), GetMemory8())); };
			instructions[0x13] = () => { Wait(1); modRM = Fetch8(); SetRegister16(ADDC(GetRegister16(), GetMemory16())); };
			instructions[0x14] = () => { Wait(1); aw.Low = ADDC(aw.Low, Fetch8()); };
			instructions[0x15] = () => { Wait(1); aw.Word = ADDC(aw.Word, Fetch16()); };
			instructions[0x16] = () => { }; // push ss
			instructions[0x17] = () => { }; // pop ss
			instructions[0x18] = () => { Wait(1); modRM = Fetch8(); SetMemory8(SUBC(GetMemory8(), GetRegister8())); };
			instructions[0x19] = () => { Wait(1); modRM = Fetch8(); SetMemory16(SUBC(GetMemory16(), GetRegister16())); };
			instructions[0x1A] = () => { Wait(1); modRM = Fetch8(); SetRegister8(SUBC(GetRegister8(), GetMemory8())); };
			instructions[0x1B] = () => { Wait(1); modRM = Fetch8(); SetRegister16(SUBC(GetRegister16(), GetMemory16())); };
			instructions[0x1C] = () => { Wait(1); aw.Low = SUBC(aw.Low, Fetch8()); };
			instructions[0x1D] = () => { Wait(1); aw.Word = SUBC(aw.Word, Fetch16()); };
			instructions[0x1E] = () => { }; // push ds0
			instructions[0x1F] = () => { }; // pop ds0

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

			instructions[0xD4] = () => { Wait(17); CVTBD(Fetch8()); };
			instructions[0xD5] = () => { Wait(6); CVTDB(Fetch8()); };

			instructions[0xD8] = () => { }; //FPO1 -- not supported
			instructions[0xD9] = () => { }; //""
			instructions[0xDA] = () => { }; //""
			instructions[0xDB] = () => { }; //""
			instructions[0xDC] = () => { }; //""
			instructions[0xDD] = () => { }; //""
			instructions[0xDE] = () => { }; //""
			instructions[0xDF] = () => { }; //""

			instructions[0xEA] = () => { Wait(6); var pc = Fetch16(); var ps = Fetch16(); this.pc = pc; this.ps = ps; Flush(); };

			instructions[0xF0] = () => { EnqueuePrefix(); };

			instructions[0xF2] = () => { Wait(4); EnqueuePrefix(); };
			instructions[0xF3] = () => { Wait(4); EnqueuePrefix(); };
		}
	}
}
