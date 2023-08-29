namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		//80  mem8  imm8
		//81  mem16 imm16
		//82  mem8  imm8
		//83  mem16 imm8sign

		internal void GroupImmediate8()
		{
			Wait(1);
			modRM = Fetch8();
			var mem = GetMemory8();
			var imm = Fetch8();
			switch (modRM.Reg)
			{
				case 0b000: SetMemory8(ADD(mem, imm)); break;
				case 0b001: SetMemory8(OR(mem, imm)); break;
				case 0b010: SetMemory8(ADDC(mem, imm)); break;
				case 0b011: SetMemory8(SUBC(mem, imm)); break;
				case 0b100: SetMemory8(AND(mem, imm)); break;
				case 0b101: SetMemory8(SUB(mem, imm)); break;
				case 0b110: SetMemory8(XOR(mem, imm)); break;
				case 0b111: SUB(mem, imm); break;
			}
		}

		internal void GroupImmediate16()
		{
			Wait(1);
			modRM = Fetch8();
			var mem = GetMemory16();
			var imm = Fetch16();
			switch (modRM.Reg)
			{
				case 0b000: SetMemory16(ADD(mem, imm)); break;
				case 0b001: SetMemory16(OR(mem, imm)); break;
				case 0b010: SetMemory16(ADDC(mem, imm)); break;
				case 0b011: SetMemory16(SUBC(mem, imm)); break;
				case 0b100: SetMemory16(AND(mem, imm)); break;
				case 0b101: SetMemory16(SUB(mem, imm)); break;
				case 0b110: SetMemory16(XOR(mem, imm)); break;
				case 0b111: SUB(mem, imm); break;
			}
		}

		internal void GroupImmediateSign()
		{
			Wait(1);
			modRM = Fetch8();
			var mem = GetMemory16();
			var imm = (ushort)(sbyte)Fetch8();
			switch (modRM.Reg)
			{
				case 0b000: SetMemory16(ADD(mem, imm)); break;
				case 0b001: SetMemory16(OR(mem, imm)); break;
				case 0b010: SetMemory16(ADDC(mem, imm)); break;
				case 0b011: SetMemory16(SUBC(mem, imm)); break;
				case 0b100: SetMemory16(AND(mem, imm)); break;
				case 0b101: SetMemory16(SUB(mem, imm)); break;
				case 0b110: SetMemory16(XOR(mem, imm)); break;
				case 0b111: SUB(mem, imm); break;
			}
		}

		//c0  mem8  imm8
		//c1  mem16 imm8
		//d0  mem8  1
		//d1  mem16 1
		//d2  mem8  cl
		//d3  mem16 cl

		internal void GroupShift8(byte value, bool imm)
		{
			modRM = Fetch8();
			var mem = GetMemory8();
			if (imm) value = Fetch8();
			value &= 0x1F;
			switch (modRM.Reg)
			{
				case 0b000: SetMemory8(ROL(mem, value)); break;
				case 0b001: SetMemory8(ROR(mem, value)); break;
				case 0b010: SetMemory8(ROLC(mem, value)); break;
				case 0b011: SetMemory8(RORC(mem, value)); break;
				case 0b100: SetMemory8(SHL(mem, value)); break;
				case 0b101: SetMemory8(SHR(mem, value)); break;
				case 0b110: /* Invalid */ break;
				case 0b111: SetMemory8(SHRA(mem, value)); break;
			}
		}

		internal void GroupShift16(ushort value, bool imm)
		{
			modRM = Fetch8();
			var mem = GetMemory16();
			if (imm) value = Fetch8();
			value &= 0x1F;
			switch (modRM.Reg)
			{
				case 0b000: SetMemory16(ROL(mem, value)); break;
				case 0b001: SetMemory16(ROR(mem, value)); break;
				case 0b010: SetMemory16(ROLC(mem, value)); break;
				case 0b011: SetMemory16(RORC(mem, value)); break;
				case 0b100: SetMemory16(SHL(mem, value)); break;
				case 0b101: SetMemory16(SHR(mem, value)); break;
				case 0b110: /* Invalid */ break;
				case 0b111: SetMemory16(SHRA(mem, value)); break;
			}
		}

		//f6  *8
		//f7  *16

		internal void Group1Misc8()
		{
			modRM = Fetch8();
			var mem = GetMemory8();
			switch (modRM.Reg)
			{
				case 0b000: Wait(1); AND(mem, Fetch8()); break;
				case 0b001: /* Invalid */ break;
				case 0b010: Wait(1); SetMemory8((byte)~mem); break;
				case 0b011: Wait(1); SetMemory8(NEG(mem)); break;
				case 0b100: Wait(3); aw.Word = MULU(aw.Low, mem); break;
				case 0b101: Wait(3); aw.Word = MUL(aw.Low, mem); break;
				case 0b110: Wait(15); aw.Word = DIVU(aw.Word, mem); break;
				case 0b111: Wait(17); aw.Word = DIV(aw.Word, mem); break;
			}
		}

		internal void Group1Misc16()
		{
			modRM = Fetch8();
			var mem = GetMemory16();
			switch (modRM.Reg)
			{
				case 0b000: Wait(1); AND(mem, Fetch16()); break;
				case 0b001: /* Invalid */ break;
				case 0b010: Wait(1); SetMemory16((ushort)~mem); break;
				case 0b011: Wait(1); SetMemory16(NEG(mem)); break;
				case 0b100:
					{
						Wait(3);
						var result = MULU(aw.Word, mem);
						dw.Word = (ushort)((result >> 16) & 0xFFFF);
						aw.Word = (ushort)((result >> 0) & 0xFFFF);
					}
					break;
				case 0b101:
					{
						Wait(3);
						var result = MUL(aw.Word, mem);
						dw.Word = (ushort)((result >> 16) & 0xFFFF);
						aw.Word = (ushort)((result >> 0) & 0xFFFF);
					}
					break;
				case 0b110:
					{
						Wait(15);
						var result = DIVU((uint)(dw.Word << 16 | aw.Word), mem);
						dw.Word = (ushort)((result >> 16) & 0xFFFF);
						aw.Word = (ushort)((result >> 0) & 0xFFFF);
					}
					break;
				case 0b111:
					{
						Wait(17);
						var result = DIV((uint)(dw.Word << 16 | aw.Word), mem);
						dw.Word = (ushort)((result >> 16) & 0xFFFF);
						aw.Word = (ushort)((result >> 0) & 0xFFFF);
					}
					break;
			}
		}

		//fe  *8
		//ff  *16

		internal void Group2Misc8()
		{
			modRM = Fetch8();
			switch (modRM.Reg)
			{
				case 0b000: Wait(1); SetMemory8(INC(GetMemory8())); break;
				case 0b001: Wait(1); SetMemory8(DEC(GetMemory8())); break;
				case 0b010: Wait(5); PUSH(pc); pc = GetMemory16(); break;
				case 0b011: Wait(11); PUSH(ps); PUSH(pc); pc = GetMemory16(0); ps = GetMemory16(2); break;
				case 0b100: Wait(4); pc = GetMemory16(); break;
				case 0b101: Wait(9); pc = GetMemory16(0); ps = GetMemory16(2); break;
				case 0b110: PUSH(GetMemory16()); break;
				case 0b111: /* Invalid */ break;
			}
		}

		internal void Group2Misc16()
		{
			modRM = Fetch8();
			switch (modRM.Reg)
			{
				case 0b000: Wait(1); SetMemory16(INC(GetMemory16())); break;
				case 0b001: Wait(1); SetMemory16(DEC(GetMemory16())); break;
				case 0b010: Wait(5); PUSH(pc); pc = GetMemory16(); break;
				case 0b011: Wait(11); PUSH(ps); PUSH(pc); pc = GetMemory16(0); ps = GetMemory16(2); break;
				case 0b100: Wait(4); pc = GetMemory16(); break;
				case 0b101: Wait(9); pc = GetMemory16(0); ps = GetMemory16(2); break;
				case 0b110: PUSH(GetMemory16()); break;
				case 0b111: /* Invalid */ break;
			}
		}
	}
}
