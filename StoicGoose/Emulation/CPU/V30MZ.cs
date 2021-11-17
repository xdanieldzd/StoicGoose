using System;

namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		/* General registers */
		Register16 ax, bx, cx, dx;
		ushort sp, bp, si, di;
		/* Segment registers */
		ushort cs, ds, ss, es;
		/* Status and instruction registers */
		ushort ip;
		Flags flags;

		bool halted;

		int pendingIntVector;

		public V30MZ(MemoryReadDelegate memoryRead, MemoryWriteDelegate memoryWrite, RegisterReadDelegate registerRead, RegisterWriteDelegate registerWrite)
		{
			memoryReadDelegate = memoryRead;
			memoryWriteDelegate = memoryWrite;
			registerReadDelegate = registerRead;
			registerWriteDelegate = registerWrite;

			Reset();
		}

		public void Reset()
		{
			/* CPU reset */
			flags = 0;
			ip = 0x0000;
			cs = 0xFFFF;
			ds = 0x0000;
			ss = 0x0000;
			es = 0x0000;

			/* Initialized by WS bootstrap */
			ax.Word = 0x0000;
			dx.Word = 0x0000;
			bp = 0x0000;
			ss = 0x0000;
			sp = 0x2000;
			ds = 0x0000;
			es = 0x0000;

			/* Misc variables */
			halted = false;

			pendingIntVector = -1;

			ResetPrefixes();
			modRm.Reset();
		}

		public void RaiseInterrupt(int vector)
		{
			pendingIntVector = vector;
		}

		private void CheckAndServiceInterrupt()
		{
			if (pendingIntVector != -1)
			{
				halted = false;

				if (IsFlagSet(Flags.InterruptEnable))
				{
					/* Service interrupt */
					var offset = ReadMemory16(0, (ushort)((pendingIntVector * 4) + 0));
					var segment = ReadMemory16(0, (ushort)((pendingIntVector * 4) + 2));

					Push((ushort)flags);
					Push(cs);
					Push(ip);

					cs = segment;
					ip = offset;

					ResetPrefixes();
					modRm.Reset();

					ClearFlags(Flags.InterruptEnable);
				}

				pendingIntVector = -1;  // TODO ????? correct????
			}
		}

		public int Step()
		{
			var ipBegin = ip;

			/* Do interrupt handling & service interrupt if needed */
			CheckAndServiceInterrupt();

			/* Is CPU halted? */
			if (halted) return 1;

			/* Read any prefixes & opcode */
			byte opcode;
			while (!HandlePrefixes(opcode = ReadMemory8(cs, ip++))) { }



			// TODO write proper disassembler etc, replace SLOWDEBUGTRACELOG
			if (GlobalVariables.EnableSuperSlowCPULogger)
			{
				// temp debug log thingy
				var dbg_cs = cs;
				var dbg_ip = ipBegin;
				var dbg_op = ReadMemory8(dbg_cs, dbg_ip);

				var out_regs = $"AX:{ax.Word:X4} BX:{bx.Word:X4} CX:{cx.Word:X4} DX:{dx.Word:X4} SP:{sp:X4} BP:{bp:X4} SI:{si:X4} DI:{di:X4}";
				var out_segs = $"CS:{cs:X4} SS:{ss:X4} DS:{ds:X4} ES:{es:X4}";

				System.IO.File.AppendAllText(@"D:\Temp\Goose\log.txt", $"{dbg_cs:X4}:{dbg_ip:X4} | {dbg_op:X2} ... | {out_regs} | {out_segs}\n");
			}



			int cycles;
			switch (opcode)
			{
				case 0x00:
					/* ADD Eb Gb */
					WriteOpcodeEb(Add8(false, ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x01:
					/* ADD Ew Gw */
					WriteOpcodeEw(Add16(false, ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x02:
					/* ADD Gb Eb */
					WriteOpcodeGb(Add8(false, ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x03:
					/* ADD Gw Ew */
					WriteOpcodeGw(Add16(false, ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x04:
					/* ADD AL Ib */
					ax.Low = Add8(false, ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x05:
					/* ADD AX Iw */
					ax.Word = Add16(false, ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				case 0x06:
					/* PUSH ES */
					Push(es);
					cycles = 1;
					break;

				case 0x07:
					/* POP ES */
					es = Pop();
					cycles = 1;
					break;

				case 0x08:
					/* OR Eb Gb */
					WriteOpcodeEb(Or8(ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x09:
					/* OR Ew Gw */
					WriteOpcodeEw(Or16(ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x0A:
					/* OR Gb Eb */
					WriteOpcodeGb(Or8(ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x0B:
					/* OR Gw Ew */
					WriteOpcodeGw(Or16(ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x0C:
					/* OR AL Ib */
					ax.Low = Or8(ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x0D:
					/* OR AX Iw */
					ax.Word = Or16(ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				case 0x0E:
					/* PUSH CS */
					Push(cs);
					cycles = 1;
					break;

				/* 0x0F -- invalid opcode (undocumented POP CS?) */

				case 0x10:
					/* ADC Eb Gb */
					WriteOpcodeEb(Add8(true, ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x11:
					/* ADC Ew Gw */
					WriteOpcodeEw(Add16(true, ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x12:
					/* ADC Gb Eb */
					WriteOpcodeGb(Add8(true, ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x13:
					/* ADC Gw Ew */
					WriteOpcodeGw(Add16(true, ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x14:
					/* ADC AL Ib */
					ax.Low = Add8(true, ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x15:
					/* ADC AX Iw */
					ax.Word = Add16(true, ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				case 0x16:
					/* PUSH SS */
					Push(ss);
					cycles = 1;
					break;

				case 0x17:
					/* POP SS */
					ss = Pop();
					cycles = 1;
					break;

				case 0x18:
					/* SBB Eb Gb */
					WriteOpcodeEb(Sub8(true, ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x19:
					/* SBB Ew Gw */
					WriteOpcodeEw(Sub16(true, ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x1A:
					/* SBB Gb Eb */
					WriteOpcodeGb(Sub8(true, ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x1B:
					/* SBB Gw Ew */
					WriteOpcodeGw(Sub16(true, ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x1C:
					/* SBB AL Ib */
					ax.Low = Sub8(true, ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x1D:
					/* SBB AX Iw */
					ax.Word = Sub16(true, ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				case 0x1E:
					/* PUSH DS */
					Push(ds);
					cycles = 1;
					break;

				case 0x1F:
					/* POP DS */
					ds = Pop();
					cycles = 1;
					break;

				case 0x20:
					/* AND Eb Gb */
					WriteOpcodeEb(And8(ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x21:
					/* AND Ew Gw */
					WriteOpcodeEw(And16(ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x22:
					/* AND Gb Eb */
					WriteOpcodeGb(And8(ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x23:
					/* AND Gw Ew */
					WriteOpcodeGw(And16(ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x24:
					/* AND AL Ib */
					ax.Low = And8(ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x25:
					/* AND AX Iw */
					ax.Word = And16(ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				/* 0x26 -- prefix ES */

				case 0x27:
					/* DAA */
					Daa(false);
					cycles = 10;
					break;

				case 0x28:
					/* SUB Eb Gb */
					WriteOpcodeEb(Sub8(false, ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x29:
					/* SUB Ew Gw */
					WriteOpcodeEw(Sub16(false, ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x2A:
					/* SUB Gb Eb */
					WriteOpcodeGb(Sub8(false, ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x2B:
					/* SUB Gw Ew */
					WriteOpcodeGw(Sub16(false, ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x2C:
					/* SUB AL Ib */
					ax.Low = Sub8(false, ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x2D:
					/* SUB AX Iw */
					ax.Word = Sub16(false, ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				/* 0x2E -- prefix CS */

				case 0x2F:
					/* DAS */
					Daa(true);
					cycles = 10;
					break;

				case 0x30:
					/* XOR Eb Gb */
					WriteOpcodeEb(Xor8(ReadOpcodeEb(), ReadOpcodeGb()));
					cycles = 1;
					break;

				case 0x31:
					/* XOR Ew Gw */
					WriteOpcodeEw(Xor16(ReadOpcodeEw(), ReadOpcodeGw()));
					cycles = 1;
					break;

				case 0x32:
					/* XOR Gb Eb */
					WriteOpcodeGb(Xor8(ReadOpcodeGb(), ReadOpcodeEb()));
					cycles = 1;
					break;

				case 0x33:
					/* XOR Gw Ew */
					WriteOpcodeGw(Xor16(ReadOpcodeGw(), ReadOpcodeEw()));
					cycles = 1;
					break;

				case 0x34:
					/* XOR AL Ib */
					ax.Low = Xor8(ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x35:
					/* XOR AX Iw */
					ax.Word = Xor16(ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				/* 0x36 -- prefix SS */

				case 0x37:
					/* AAA */
					Aaa(false);
					cycles = 9;
					break;

				case 0x38:
					/* CMP Eb Gb */
					Sub8(false, ReadOpcodeEb(), ReadOpcodeGb());
					cycles = 1;
					break;

				case 0x39:
					/* CMP Ew Gw */
					Sub16(false, ReadOpcodeEw(), ReadOpcodeGw());
					cycles = 1;
					break;

				case 0x3A:
					/* CMP Gb Eb */
					Sub8(false, ReadOpcodeGb(), ReadOpcodeEb());
					cycles = 1;
					break;

				case 0x3B:
					/* CMP Gw Ew */
					Sub16(false, ReadOpcodeGw(), ReadOpcodeEw());
					cycles = 1;
					break;

				case 0x3C:
					/* CMP AL Ib */
					Sub8(false, ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0x3D:
					/* CMP AX Iw */
					Sub16(false, ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				/* 0x3E -- prefix DS */

				case 0x3F:
					/* AAS */
					Aaa(true);
					cycles = 9;
					break;

				case 0x40:
					/* INC AX */
					ax.Word = Inc16(ax.Word);
					cycles = 1;
					break;

				case 0x41:
					/* INC CX */
					cx.Word = Inc16(cx.Word);
					cycles = 1;
					break;

				case 0x42:
					/* INC DX */
					dx.Word = Inc16(dx.Word);
					cycles = 1;
					break;

				case 0x43:
					/* INC BX */
					bx.Word = Inc16(bx.Word);
					cycles = 1;
					break;

				case 0x44:
					/* INC SP */
					sp = Inc16(sp);
					cycles = 1;
					break;

				case 0x45:
					/* INC BP */
					bp = Inc16(bp);
					cycles = 1;
					break;

				case 0x46:
					/* INC SI */
					si = Inc16(si);
					cycles = 1;
					break;

				case 0x47:
					/* INC DI */
					di = Inc16(di);
					cycles = 1;
					break;

				case 0x48:
					/* DEC AX */
					ax.Word = Dec16(ax.Word);
					cycles = 1;
					break;

				case 0x49:
					/* DEC CX */
					cx.Word = Dec16(cx.Word);
					cycles = 1;
					break;

				case 0x4A:
					/* DEC DX */
					dx.Word = Dec16(dx.Word);
					cycles = 1;
					break;

				case 0x4B:
					/* DEC BX */
					bx.Word = Dec16(bx.Word);
					cycles = 1;
					break;

				case 0x4C:
					/* DEC SP */
					sp = Dec16(sp);
					cycles = 1;
					break;

				case 0x4D:
					/* DEC BP */
					bp = Dec16(bp);
					cycles = 1;
					break;

				case 0x4E:
					/* DEC SI */
					si = Dec16(si);
					cycles = 1;
					break;

				case 0x4F:
					/* DEC DI */
					di = Dec16(di);
					cycles = 1;
					break;

				case 0x50:
					/* PUSH AX */
					Push(ax.Word);
					cycles = 1;
					break;

				case 0x51:
					/* PUSH CX */
					Push(cx.Word);
					cycles = 1;
					break;

				case 0x52:
					/* PUSH DX */
					Push(dx.Word);
					cycles = 1;
					break;

				case 0x53:
					/* PUSH BX */
					Push(bx.Word);
					cycles = 1;
					break;

				case 0x54:
					/* PUSH SP */
					Push(sp);
					cycles = 1;
					break;

				case 0x55:
					/* PUSH BP */
					Push(bp);
					cycles = 1;
					break;

				case 0x56:
					/* PUSH SI */
					Push(si);
					cycles = 1;
					break;

				case 0x57:
					/* PUSH DI */
					Push(di);
					cycles = 1;
					break;

				case 0x58:
					/* POP AX */
					ax.Word = Pop();
					cycles = 1;
					break;

				case 0x59:
					/* POP CX */
					cx.Word = Pop();
					cycles = 1;
					break;

				case 0x5A:
					/* POP DX */
					dx.Word = Pop();
					cycles = 1;
					break;

				case 0x5B:
					/* POP BX */
					bx.Word = Pop();
					cycles = 1;
					break;

				case 0x5C:
					/* POP SP */
					sp = Pop();
					cycles = 1;
					break;

				case 0x5D:
					/* POP BP */
					bp = Pop();
					cycles = 1;
					break;

				case 0x5E:
					/* POP SI */
					si = Pop();
					cycles = 1;
					break;

				case 0x5F:
					/* POP DI */
					di = Pop();
					cycles = 1;
					break;

				case 0x60:
					/* PUSHA -- 80186 */
					{
						var oldSp = sp;
						Push(ax.Word);
						Push(cx.Word);
						Push(dx.Word);
						Push(bx.Word);
						Push(oldSp);
						Push(bp);
						Push(si);
						Push(di);
						cycles = 8;
					}
					break;

				case 0x61:
					/* POPA -- 80186 */
					{
						di = Pop();
						si = Pop();
						bp = Pop();
						Pop(); /* don't restore SP */
						bx.Word = Pop();
						dx.Word = Pop();
						cx.Word = Pop();
						ax.Word = Pop();
						cycles = 8;
					}
					break;

				/* BOUND Gw E -- 80186 */
				case 0x62:
					ReadModRM();
					var lo = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 0));
					var hi = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 2));
					var reg = GetRegister16((RegisterNumber16)modRm.Mem);
					if (reg < lo || reg > hi) RaiseInterrupt(5);
					cycles = 12;
					break;

				/* 0x63-0x67 -- invalid opcodes */

				case 0x68:
					/* PUSH Iw -- 80186 */
					Push(ReadOpcodeIw());
					cycles = 1;
					break;

				/* IMUL Gw Ew Iw -- 80186 */
				case 0x69:
					WriteOpcodeGw((ushort)(Mul16(true, ReadOpcodeEw(), ReadOpcodeIw()) & 0xFFFF));
					cycles = 4;
					break;

				case 0x6A:
					/* PUSH Ib -- 80186 */
					Push((ushort)(sbyte)ReadOpcodeIb());
					cycles = 1;
					break;

				/* IMUL Gb Eb Ib -- 80186 */
				case 0x6B:
					WriteOpcodeGb((byte)(Mul8(true, ReadOpcodeEb(), ReadOpcodeIb()) & 0xFF));
					cycles = 4;
					break;

				case 0x6C:
					/* INSB -- 80186 */
					// TODO: verify
					{
						if (!prefixHasRepeat)
							InString(false);
						else if (cx.Word != 0)
						{
							do { InString(false); } while (--cx.Word != 0);
						}
						cycles = 5;
					}
					break;

				case 0x6D:
					/* INSW -- 80186 */
					// TODO: verify
					{
						if (!prefixHasRepeat)
							InString(true);
						else if (cx.Word != 0)
						{
							do { InString(true); } while (--cx.Word != 0);
						}
						cycles = 5;
					}
					break;

				case 0x6E:
					/* OUTSB -- 80186 */
					// TODO: verify
					{
						if (!prefixHasRepeat)
							OutString(false);
						else if (cx.Word != 0)
						{
							do { OutString(false); } while (--cx.Word != 0);
						}
						cycles = 6;
					}
					break;

				case 0x6F:
					/* OUTSW -- 80186 */
					// TODO: verify
					{
						if (!prefixHasRepeat)
							OutString(true);
						else if (cx.Word != 0)
						{
							do { OutString(true); } while (--cx.Word != 0);
						}
						cycles = 6;
					}
					break;

				case 0x70:
					/* JO */
					cycles = JumpConditional(IsFlagSet(Flags.Overflow));
					break;

				case 0x71:
					/* JNO */
					cycles = JumpConditional(!IsFlagSet(Flags.Overflow));
					break;

				case 0x72:
					/* JB */
					cycles = JumpConditional(IsFlagSet(Flags.Carry));
					break;

				case 0x73:
					/* JNB */
					cycles = JumpConditional(!IsFlagSet(Flags.Carry));
					break;

				case 0x74:
					/* JZ */
					cycles = JumpConditional(IsFlagSet(Flags.Zero));
					break;

				case 0x75:
					/* JNZ */
					cycles = JumpConditional(!IsFlagSet(Flags.Zero));
					break;

				case 0x76:
					/* JBE */
					cycles = JumpConditional(IsFlagSet(Flags.Carry) || IsFlagSet(Flags.Zero));
					break;

				case 0x77:
					/* JA */
					cycles = JumpConditional(!IsFlagSet(Flags.Carry) && !IsFlagSet(Flags.Zero));
					break;

				case 0x78:
					/* JS */
					cycles = JumpConditional(IsFlagSet(Flags.Sign));
					break;

				case 0x79:
					/* JNS */
					cycles = JumpConditional(!IsFlagSet(Flags.Sign));
					break;

				case 0x7A:
					/* JPE */
					cycles = JumpConditional(IsFlagSet(Flags.Parity));
					break;

				case 0x7B:
					/* JPO */
					cycles = JumpConditional(!IsFlagSet(Flags.Parity));
					break;

				case 0x7C:
					/* JL */
					//cycles = JumpConditional(!IsFlagSet(Flags.Zero) && IsFlagSet(Flags.Sign) != IsFlagSet(Flags.Overflow));		//???
					cycles = JumpConditional(IsFlagSet(Flags.Sign) != IsFlagSet(Flags.Overflow));
					break;

				case 0x7D:
					/* JGE */
					//cycles = JumpConditional(IsFlagSet(Flags.Zero) || IsFlagSet(Flags.Sign) == IsFlagSet(Flags.Overflow));		//???
					cycles = JumpConditional(IsFlagSet(Flags.Sign) == IsFlagSet(Flags.Overflow));
					break;

				case 0x7E:
					/* JLE */
					cycles = JumpConditional(IsFlagSet(Flags.Zero) || IsFlagSet(Flags.Sign) != IsFlagSet(Flags.Overflow));
					break;

				case 0x7F:
					/* JG */
					cycles = JumpConditional(!IsFlagSet(Flags.Zero) && IsFlagSet(Flags.Sign) == IsFlagSet(Flags.Overflow));
					break;

				case 0x80:
				case 0x82:
					/* GRP1 Eb Ib */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ADD */ WriteOpcodeEb(Add8(false, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x1: /* OR  */ WriteOpcodeEb(Or8(ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x2: /* ADC */ WriteOpcodeEb(Add8(true, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x3: /* SBB */ WriteOpcodeEb(Sub8(true, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x4: /* AND */ WriteOpcodeEb(And8(ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x5: /* SUB */ WriteOpcodeEb(Sub8(false, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x6: /* XOR */ WriteOpcodeEb(Xor8(ReadOpcodeEb(), ReadOpcodeIb())); cycles = 1; break;
						case 0x7: /* CMP */ Sub8(false, ReadOpcodeEb(), ReadOpcodeIb()); cycles = 1; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0x81:
					/* GRP1 Ew Iw */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ADD */ WriteOpcodeEw(Add16(false, ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x1: /* OR  */ WriteOpcodeEw(Or16(ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x2: /* ADC */ WriteOpcodeEw(Add16(true, ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x3: /* SBB */ WriteOpcodeEw(Sub16(true, ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x4: /* AND */ WriteOpcodeEw(And16(ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x5: /* SUB */ WriteOpcodeEw(Sub16(false, ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x6: /* XOR */ WriteOpcodeEw(Xor16(ReadOpcodeEw(), ReadOpcodeIw())); cycles = 1; break;
						case 0x7: /* CMP */ Sub16(false, ReadOpcodeEw(), ReadOpcodeIw()); cycles = 1; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0x83:
					/* GRP1 Ew Ib */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ADD */ WriteOpcodeEw(Add16(false, ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x1: /* OR  */ WriteOpcodeEw(Or16(ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x2: /* ADC */ WriteOpcodeEw(Add16(true, ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x3: /* SBB */ WriteOpcodeEw(Sub16(true, ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x4: /* AND */ WriteOpcodeEw(And16(ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x5: /* SUB */ WriteOpcodeEw(Sub16(false, ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x6: /* XOR */ WriteOpcodeEw(Xor16(ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb())); cycles = 1; break;
						case 0x7: /* CMP */ Sub16(false, ReadOpcodeEw(), (ushort)(sbyte)ReadOpcodeIb()); cycles = 1; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0x84:
					/* TEST Gb Eb */
					And8(ReadOpcodeGb(), ReadOpcodeEb());
					cycles = 1;
					break;

				case 0x85:
					/* TEST Gw Ew */
					And16(ReadOpcodeGw(), ReadOpcodeEw());
					cycles = 1;
					break;

				case 0x86:
					/* XCHG Gb Eb */
					{
						var temp = ReadOpcodeGb();
						WriteOpcodeGb(ReadOpcodeEb());
						WriteOpcodeEb(temp);
						cycles = 3;
					}
					break;

				case 0x87:
					/* XCHG Gw Ew */
					{
						var temp = ReadOpcodeGw();
						WriteOpcodeGw(ReadOpcodeEw());
						WriteOpcodeEw(temp);
						cycles = 3;
					}
					break;

				case 0x88:
					/* MOV Eb Gb */
					WriteOpcodeEb(ReadOpcodeGb());
					cycles = 1;
					break;

				case 0x89:
					/* MOV Ew Gw */
					WriteOpcodeEw(ReadOpcodeGw());
					cycles = 1;
					break;

				case 0x8A:
					/* MOV Gb Eb */
					WriteOpcodeGb(ReadOpcodeEb());
					cycles = 1;
					break;

				case 0x8B:
					/* MOV Gw Ew */
					WriteOpcodeGw(ReadOpcodeEw());
					cycles = 1;
					break;

				case 0x8C:
					/* MOV Ew Sw */
					WriteOpcodeEw(ReadOpcodeSw());
					cycles = 1;
					break;

				case 0x8D:
					/* LEA Gw M */
					ReadModRM();
					if (modRm.Mod == ModRM.Modes.Register)
					{
						RaiseInterrupt(6);
						cycles = 8;
						break;
					}
					WriteOpcodeGw(modRm.Offset);
					cycles = 8;
					break;

				case 0x8E:
					/* MOV Sw Ew */
					WriteOpcodeSw(ReadOpcodeEw());
					cycles = 1;
					break;

				case 0x8F:
					/* POP Ew */
					WriteOpcodeEw(Pop());
					cycles = 1;
					break;

				case 0x90:
					/* NOP (XCHG AX AX) */
					{
						var temp = ax.Word;
						ax.Word = temp;
						cycles = 3;
					}
					break;

				case 0x91:
					/* XCHG CX AX */
					{
						var temp = ax.Word;
						ax.Word = cx.Word;
						cx.Word = temp;
						cycles = 3;
					}
					break;

				case 0x92:
					/* XCHG DX AX */
					{
						var temp = ax.Word;
						ax.Word = dx.Word;
						dx.Word = temp;
						cycles = 3;
					}
					break;

				case 0x93:
					/* XCHG BX AX */
					{
						var temp = ax.Word;
						ax.Word = bx.Word;
						bx.Word = temp;
						cycles = 3;
					}
					break;

				case 0x94:
					/* XCHG SP AX */
					{
						var temp = ax.Word;
						ax.Word = sp;
						sp = temp;
						cycles = 3;
					}
					break;

				case 0x95:
					/* XCHG BP AX */
					{
						var temp = ax.Word;
						ax.Word = bp;
						bp = temp;
						cycles = 3;
					}
					break;

				case 0x96:
					/* XCHG SI AX */
					{
						var temp = ax.Word;
						ax.Word = si;
						si = temp;
						cycles = 3;
					}
					break;

				case 0x97:
					/* XCHG DI AX */
					{
						var temp = ax.Word;
						ax.Word = di;
						di = temp;
						cycles = 3;
					}
					break;

				case 0x98:
					/* CBW */
					ax.Word = (ushort)(sbyte)ax.Low;
					cycles = 2;
					break;

				case 0x99:
					/* CWD */
					{
						var value = (uint)(short)ax.Word;
						dx.Word = (ushort)((value >> 16) & 0xFFFF);
						ax.Word = (ushort)((value >> 0) & 0xFFFF);
						cycles = 2;
					}
					break;

				case 0x9A:
					/* CALL Ap */
					{
						var newIp = ReadOpcodeIw();
						var newCs = ReadOpcodeIw();

						Push(cs);
						Push(ip);

						ip = newIp;
						cs = newCs;

						cycles = 9;
					}
					break;

				case 0x9B:
					/* WAIT */
					cycles = 1;
					break;

				case 0x9C:
					/* PUSHF */
					Push((ushort)flags);
					cycles = 1;
					break;

				case 0x9D:
					/* POPF */
					flags = (Flags)Pop();
					cycles = 1;
					break;

				case 0x9E:
					/* SAHF */
					SetClearFlagConditional(Flags.Sign, ((Flags)ax.High & Flags.Sign) == Flags.Sign);
					SetClearFlagConditional(Flags.Zero, ((Flags)ax.High & Flags.Zero) == Flags.Zero);
					SetClearFlagConditional(Flags.Auxiliary, ((Flags)ax.High & Flags.Auxiliary) == Flags.Auxiliary);
					SetClearFlagConditional(Flags.Parity, ((Flags)ax.High & Flags.Parity) == Flags.Parity);
					SetClearFlagConditional(Flags.Carry, ((Flags)ax.High & Flags.Carry) == Flags.Carry);
					cycles = 4;
					break;

				case 0x9F:
					/* LAHF */
					ax.High = (byte)flags;
					cycles = 2;
					break;

				case 0xA0:
					/* MOV AL Aw */
					ax.Low = ReadMemory8(GetSegmentViaOverride(SegmentNumber.DS), ReadOpcodeIw());
					cycles = 1;
					break;

				case 0xA1:
					/* MOV AX Aw */
					ax.Word = ReadMemory16(GetSegmentViaOverride(SegmentNumber.DS), ReadOpcodeIw());
					cycles = 1;
					break;

				case 0xA2:
					/* MOV Aw AL */
					WriteMemory8(GetSegmentViaOverride(SegmentNumber.DS), ReadOpcodeIw(), ax.Low);
					cycles = 1;
					break;

				case 0xA3:
					/* MOV Aw AX */
					WriteMemory16(GetSegmentViaOverride(SegmentNumber.DS), ReadOpcodeIw(), ax.Word);
					cycles = 1;
					break;

				case 0xA4:
					/* MOVSB */
					if (!prefixHasRepeat)
						MoveString(false);
					else if (cx.Word != 0)
					{
						do { MoveString(false); } while (--cx.Word != 0);
					}
					cycles = 3;
					break;

				case 0xA5:
					/* MOVSW */
					if (!prefixHasRepeat)
						MoveString(true);
					else if (cx.Word != 0)
					{
						do { MoveString(true); } while (--cx.Word != 0);
					}
					cycles = 3;
					break;

				case 0xA6:
					/* CMPSB */
					if (!prefixHasRepeat)
						CompareString(false);
					else if (cx.Word != 0)
					{
						do { CompareString(false); } while (--cx.Word != 0 && (prefixRepeatOnNotEqual ? !IsFlagSet(Flags.Zero) : IsFlagSet(Flags.Zero)));
					}
					cycles = 4;
					break;

				case 0xA7:
					/* CMPSW */
					if (!prefixHasRepeat)
						CompareString(true);
					else if (cx.Word != 0)
					{
						do { CompareString(true); } while (--cx.Word != 0 && (prefixRepeatOnNotEqual ? !IsFlagSet(Flags.Zero) : IsFlagSet(Flags.Zero)));
					}
					cycles = 4;
					break;

				case 0xA8:
					/* TEST AL Ib */
					And8(ax.Low, ReadOpcodeIb());
					cycles = 1;
					break;

				case 0xA9:
					/* TEST AX Iw */
					And16(ax.Word, ReadOpcodeIw());
					cycles = 1;
					break;

				case 0xAA:
					/* STOSB */
					if (!prefixHasRepeat)
						StoreString(false);
					else if (cx.Word != 0)
					{
						do { StoreString(false); } while (--cx.Word != 0);
					}
					cycles = 2;
					break;

				case 0xAB:
					/* STOSW */
					if (!prefixHasRepeat)
						StoreString(true);
					else if (cx.Word != 0)
					{
						do { StoreString(true); } while (--cx.Word != 0);
					}
					cycles = 2;
					break;

				case 0xAC:
					/* LODSB */
					if (!prefixHasRepeat)
						LoadString(false);
					else if (cx.Word != 0)
					{
						do { LoadString(false); } while (--cx.Word != 0);
					}
					cycles = 2;
					break;

				case 0xAD:
					/* LODSW */
					if (!prefixHasRepeat)
						LoadString(true);
					else if (cx.Word != 0)
					{
						do { LoadString(true); } while (--cx.Word != 0);
					}
					cycles = 2;
					break;

				case 0xAE:
					/* SCASB */
					if (!prefixHasRepeat)
						ScanString(false);
					else if (cx.Word != 0)
					{
						do { ScanString(false); } while (--cx.Word != 0 && (prefixRepeatOnNotEqual ? !IsFlagSet(Flags.Zero) : IsFlagSet(Flags.Zero)));
					}
					cycles = 3;
					break;

				case 0xAF:
					/* SCASW */
					if (!prefixHasRepeat)
						ScanString(true);
					else if (cx.Word != 0)
					{
						do { ScanString(true); } while (--cx.Word != 0 && (prefixRepeatOnNotEqual ? !IsFlagSet(Flags.Zero) : IsFlagSet(Flags.Zero)));
					}
					cycles = 3;
					break;

				case 0xB0:
					/* MOV AL Ib */
					ax.Low = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB1:
					/* MOV CL Ib */
					cx.Low = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB2:
					/* MOV DL Ib */
					dx.Low = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB3:
					/* MOV BL Ib */
					bx.Low = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB4:
					/* MOV AH Ib */
					ax.High = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB5:
					/* MOV CH Ib */
					cx.High = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB6:
					/* MOV DH Ib */
					dx.High = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB7:
					/* MOV BH Ib */
					bx.High = ReadOpcodeIb();
					cycles = 1;
					break;

				case 0xB8:
					/* MOV AX Iw */
					ax.Word = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xB9:
					/* MOV CX Iw */
					cx.Word = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xBA:
					/* MOV DX Iw */
					dx.Word = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xBB:
					/* MOV BX Iw */
					bx.Word = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xBC:
					/* MOV SP Iw */
					sp = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xBD:
					/* MOV BP Iw */
					bp = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xBE:
					/* MOV SI Iw */
					si = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xBF:
					/* MOV DI Iw */
					di = ReadOpcodeIw();
					cycles = 1;
					break;

				case 0xC0:
					/* GRP2 Eb Ib -- 80186 */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ROL */ WriteOpcodeEb(Rol8(false, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						case 0x1: /* ROR */ WriteOpcodeEb(Ror8(false, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						case 0x2: /* RCL */ WriteOpcodeEb(Rol8(true, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						case 0x3: /* RCR */ WriteOpcodeEb(Ror8(true, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						case 0x4: /* SHL */ WriteOpcodeEb(Shl8(ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						case 0x5: /* SHR */ WriteOpcodeEb(Shr8(false, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						case 0x6: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x7: /* SAR */ WriteOpcodeEb(Shr8(true, ReadOpcodeEb(), ReadOpcodeIb())); cycles = 3; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xC1:
					/* GRP2 Ew Ib -- 80186 */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ROL */ WriteOpcodeEw(Rol16(false, ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						case 0x1: /* ROR */ WriteOpcodeEw(Ror16(false, ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						case 0x2: /* RCL */ WriteOpcodeEw(Rol16(true, ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						case 0x3: /* RCR */ WriteOpcodeEw(Ror16(true, ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						case 0x4: /* SHL */ WriteOpcodeEw(Shl16(ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						case 0x5: /* SHR */ WriteOpcodeEw(Shr16(false, ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						case 0x6: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x7: /* SAR */ WriteOpcodeEw(Shr16(true, ReadOpcodeEw(), ReadOpcodeIb())); cycles = 3; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xC2:
					/* RET Iw */
					{
						var offset = ReadOpcodeIw();
						ip = Pop();
						sp += offset;
						cycles = 5;
					}
					break;

				case 0xC3:
					/* RET */
					ip = Pop();
					cycles = 5;
					break;

				case 0xC4:
					/* LES Gw Mp */
					ReadModRM();
					if (modRm.Mod == ModRM.Modes.Register)
					{
						RaiseInterrupt(6);
						cycles = 8;
						break;
					}
					WriteOpcodeGw(ReadOpcodeEw());
					es = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 2));
					cycles = 8;
					break;

				case 0xC5:
					/* LDS Gw Mp */
					ReadModRM();
					if (modRm.Mod == ModRM.Modes.Register)
					{
						RaiseInterrupt(6);
						cycles = 8;
						break;
					}
					WriteOpcodeGw(ReadOpcodeEw());
					ds = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 2));
					cycles = 8;
					break;

				case 0xC6:
					/* MOV Eb Ib */
					ReadModRM();
					WriteOpcodeEb(ReadOpcodeIb());
					cycles = 1;
					break;

				case 0xC7:
					/* MOV Ew Iw */
					ReadModRM();
					WriteOpcodeEw(ReadOpcodeIw());
					cycles = 1;
					break;

				case 0xC8:
					/* ENTER -- 80186 */
					{
						var offset = ReadOpcodeIw();
						var length = (byte)(ReadOpcodeIb() & 0x1F);

						Push(bp);
						bp = sp;
						sp -= offset;

						if (length != 0)
						{
							for (var i = 1; i < length; i++)
								Push(ReadMemory16(ss, (ushort)(bp - i * 2)));
							Push(bp);
						}
						cycles = 7;
					}
					break;

				case 0xC9:
					/* LEAVE -- 80186 */
					sp = bp;
					bp = Pop();
					cycles = 1;
					break;

				case 0xCA:
					/* RETF Iw */
					{
						var offset = ReadOpcodeIw();
						ip = Pop();
						cs = Pop();
						sp += offset;
						cycles = 8;
					}
					break;

				case 0xCB:
					/* RETF */
					ip = Pop();
					cs = Pop();
					cycles = 7;
					break;

				case 0xCC:
					/* INT 3 */
					RaiseInterrupt(3);
					cycles = 8;
					break;

				case 0xCD:
					/* INT Ib */
					RaiseInterrupt(ReadOpcodeIb());
					cycles = 9;
					break;

				case 0xCE:
					/* INTO */
					if (IsFlagSet(Flags.Overflow)) RaiseInterrupt(4);
					cycles = 5;
					break;

				case 0xCF:
					/* IRET */
					ip = Pop();
					cs = Pop();
					flags = (Flags)Pop();
					cycles = 9;
					break;

				case 0xD0:
					/* GRP2 Eb 1 */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ROL */ WriteOpcodeEb(Rol8(false, ReadOpcodeEb(), 1)); cycles = 1; break;
						case 0x1: /* ROR */ WriteOpcodeEb(Ror8(false, ReadOpcodeEb(), 1)); cycles = 1; break;
						case 0x2: /* RCL */ WriteOpcodeEb(Rol8(true, ReadOpcodeEb(), 1)); cycles = 1; break;
						case 0x3: /* RCR */ WriteOpcodeEb(Ror8(true, ReadOpcodeEb(), 1)); cycles = 1; break;
						case 0x4: /* SHL */ WriteOpcodeEb(Shl8(ReadOpcodeEb(), 1)); cycles = 1; break;
						case 0x5: /* SHR */ WriteOpcodeEb(Shr8(false, ReadOpcodeEb(), 1)); cycles = 1; break;
						case 0x6: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x7: /* SAR */ WriteOpcodeEb(Shr8(true, ReadOpcodeEb(), 1)); cycles = 1; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xD1:
					/* GRP2 Ew 1 */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ROL */ WriteOpcodeEw(Rol16(false, ReadOpcodeEw(), 1)); cycles = 1; break;
						case 0x1: /* ROR */ WriteOpcodeEw(Ror16(false, ReadOpcodeEw(), 1)); cycles = 1; break;
						case 0x2: /* RCL */ WriteOpcodeEw(Rol16(true, ReadOpcodeEw(), 1)); cycles = 1; break;
						case 0x3: /* RCR */ WriteOpcodeEw(Ror16(true, ReadOpcodeEw(), 1)); cycles = 1; break;
						case 0x4: /* SHL */ WriteOpcodeEw(Shl16(ReadOpcodeEw(), 1)); cycles = 1; break;
						case 0x5: /* SHR */ WriteOpcodeEw(Shr16(false, ReadOpcodeEw(), 1)); cycles = 1; break;
						case 0x6: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x7: /* SAR */ WriteOpcodeEw(Shr16(true, ReadOpcodeEw(), 1)); cycles = 1; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xD2:
					/* GRP2 Eb CL */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ROL */ WriteOpcodeEb(Rol8(false, ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						case 0x1: /* ROR */ WriteOpcodeEb(Ror8(false, ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						case 0x2: /* RCL */ WriteOpcodeEb(Rol8(true, ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						case 0x3: /* RCR */ WriteOpcodeEb(Ror8(true, ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						case 0x4: /* SHL */ WriteOpcodeEb(Shl8(ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						case 0x5: /* SHR */ WriteOpcodeEb(Shr8(false, ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						case 0x6: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x7: /* SAR */ WriteOpcodeEb(Shr8(true, ReadOpcodeEb(), cx.Low)); cycles = 3; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xD3:
					/* GRP2 Ew CL */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* ROL */ WriteOpcodeEw(Rol16(false, ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						case 0x1: /* ROR */ WriteOpcodeEw(Ror16(false, ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						case 0x2: /* RCL */ WriteOpcodeEw(Rol16(true, ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						case 0x3: /* RCR */ WriteOpcodeEw(Ror16(true, ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						case 0x4: /* SHL */ WriteOpcodeEw(Shl16(ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						case 0x5: /* SHR */ WriteOpcodeEw(Shr16(false, ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						case 0x6: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x7: /* SAR */ WriteOpcodeEw(Shr16(true, ReadOpcodeEw(), cx.Low)); cycles = 3; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xD4:
					/* AAM */
					{
						var value = ReadOpcodeIb();
						ax.High = (byte)(ax.Low / value);
						ax.Low = (byte)(ax.Low % value);
						SetClearFlagConditional(Flags.Parity, CalculateParity(ax.Low));
						SetClearFlagConditional(Flags.Zero, (ax.Word & 0xFFFF) == 0);
						SetClearFlagConditional(Flags.Sign, (ax.Word & 0x8000) != 0);
						cycles = 16;
					}
					break;

				case 0xD5:
					/* AAD */
					{
						var value = ReadOpcodeIb();
						ax.Low = (byte)(ax.High * value + ax.Low);
						ax.High = 0;
						SetClearFlagConditional(Flags.Parity, CalculateParity(ax.Low));
						SetClearFlagConditional(Flags.Zero, (ax.Word & 0xFFFF) == 0);
						SetClearFlagConditional(Flags.Sign, (ax.Word & 0x8000) != 0);
						cycles = 6;
					}
					break;

				/* 0xD6 -- invalid opcode */

				case 0xD7:
					/* XLAT */
					ax.Low = ReadMemory8(GetSegmentViaOverride(SegmentNumber.DS), (ushort)(bx.Word + ax.Low));
					cycles = 4;
					break;

				/* 0xD8-0xDF -- invalid opcodes */

				case 0xE0:
					/* LOOPNZ Jb */
					cycles = JumpConditional(--cx.Word != 0 && !IsFlagSet(Flags.Zero));
					break;

				case 0xE1:
					/* LOOPZ Jb */
					cycles = JumpConditional(--cx.Word != 0 && IsFlagSet(Flags.Zero));
					break;

				case 0xE2:
					/* LOOP Jb */
					cycles = JumpConditional(--cx.Word != 0);
					break;

				case 0xE3:
					/* JCXZ */
					cycles = JumpConditional(cx.Word == 0);
					break;

				case 0xE4:
					/* IN Ib AL */
					ax.Low = ReadRegister8(ReadOpcodeIb());
					cycles = 4;
					break;

				case 0xE5:
					/* IN Ib AX */
					ax.Word = ReadRegister16(ReadOpcodeIb());
					cycles = 4;
					break;

				case 0xE6:
					/* OUT Ib AL */
					WriteRegister8(ReadOpcodeIb(), ax.Low);
					cycles = 4;
					break;

				case 0xE7:
					/* OUT Ib AX */
					WriteRegister16(ReadOpcodeIb(), ax.Word);
					cycles = 4;
					break;

				case 0xE8:
					/* CALL Jv */
					{
						var offset = ReadOpcodeIw();
						Push(ip);
						ip += offset;
						cycles = 4;
					}
					break;

				case 0xE9:
					/* JMP Jv */
					{
						var offset = ReadOpcodeIw();
						ip += offset;
						cycles = 3;
					}
					break;

				case 0xEA:
					/* JMP Ap */
					{
						var newIp = ReadOpcodeIw();
						var newCs = ReadOpcodeIw();

						ip = newIp;
						cs = newCs;

						cycles = 6;
					}
					break;

				case 0xEB:
					/* JMP Jb */
					ip = ReadOpcodeJb();
					cycles = 4;
					break;

				case 0xEC:
					/* IN AL DX */
					ax.Low = ReadRegister8(dx.Word);
					cycles = 4;
					break;

				case 0xED:
					/* IN AX DX */
					ax.Word = ReadRegister16(dx.Word);
					cycles = 4;
					break;

				case 0xEE:
					/* OUT DX AL */
					WriteRegister8(dx.Word, ax.Low);
					cycles = 4;
					break;

				case 0xEF:
					/* OUT DX AX */
					WriteRegister16(dx.Word, ax.Word);
					cycles = 4;
					break;

				/* 0xF0 -- prefix LOCK */
				/* 0xF1 -- invalid opcode */
				/* 0xF2-0xF3 -- prefix REPNZ, REPZ */

				case 0xF4:
					/* HLT */
					halted = true;
					cycles = 8;
					break;

				case 0xF5:
					/* CMC */
					SetClearFlagConditional(Flags.Carry, !IsFlagSet(Flags.Carry));
					cycles = 4;
					break;

				case 0xF6:
					/* GRP3 Eb */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* TEST */ And8(ReadOpcodeEb(), ReadOpcodeIb()); cycles = 1; break;
						case 0x1: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x2: /* NOT */ WriteOpcodeEb((byte)~ReadOpcodeEb()); cycles = 1; break;
						case 0x3: /* NEG */ WriteOpcodeEb(Neg8(ReadOpcodeEb())); cycles = 1; break;
						case 0x4: /* MUL */ ax.Word = Mul8(false, ax.Low, ReadOpcodeEb()); cycles = 3; break;
						case 0x5: /* IMUL */ ax.Word = Mul8(true, ax.Low, ReadOpcodeEb()); cycles = 3; break;
						case 0x6: /* DIV */ ax.Word = Div8(false, ax.Word, ReadOpcodeEb()); cycles = 15; break;
						case 0x7: /* IDIV */ ax.Word = Div8(true, ax.Word, ReadOpcodeEb()); cycles = 17; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xF7:
					/* GRP3 Ew */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* TEST */ And16(ReadOpcodeEw(), ReadOpcodeIw()); cycles = 1; break;
						case 0x1: /* --- */ RaiseInterrupt(6); cycles = 8; break;
						case 0x2: /* NOT */ WriteOpcodeEw((ushort)~ReadOpcodeEw()); cycles = 1; break;
						case 0x3: /* NEG */ WriteOpcodeEw(Neg16(ReadOpcodeEw())); cycles = 1; break;
						case 0x4: /* MUL */ { var result = Mul16(false, ax.Word, ReadOpcodeEw()); dx.Word = (ushort)((result >> 16) & 0xFFFF); ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 3; } break;
						case 0x5: /* IMUL */ { var result = Mul16(true, ax.Word, ReadOpcodeEw()); dx.Word = (ushort)((result >> 16) & 0xFFFF); ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 3; } break;
						case 0x6: /* DIV */ { var result = Div16(false, (uint)(dx.Word << 16 | ax.Word), ReadOpcodeEw()); dx.Word = (ushort)((result >> 16) & 0xFFFF); ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 23; } break;
						case 0x7: /* IDIV */ { var result = Div16(true, (uint)(dx.Word << 16 | ax.Word), ReadOpcodeEw()); dx.Word = (ushort)((result >> 16) & 0xFFFF); ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 24; } break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xF8:
					/* CLC */
					ClearFlags(Flags.Carry);
					cycles = 4;
					break;

				case 0xF9:
					/* STC */
					SetFlags(Flags.Carry);
					cycles = 4;
					break;

				case 0xFA:
					/* CLI */
					ClearFlags(Flags.InterruptEnable);
					cycles = 4;
					break;

				case 0xFB:
					/* STI */
					SetFlags(Flags.InterruptEnable);
					cycles = 4;
					break;

				case 0xFC:
					/* CLD */
					ClearFlags(Flags.Direction);
					cycles = 4;
					break;

				case 0xFD:
					/* STD */
					SetFlags(Flags.Direction);
					cycles = 4;
					break;

				case 0xFE:
					/* GRP4 Eb */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* INC */ WriteOpcodeEb(Inc8(ReadOpcodeEb())); cycles = 1; break;
						case 0x1: /* DEC */ WriteOpcodeEb(Dec8(ReadOpcodeEb())); cycles = 1; break;
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				case 0xFF:
					/* GRP4 Ew */
					ReadModRM();
					switch (modRm.Reg)
					{
						case 0x0: /* INC */ WriteOpcodeEw(Inc16(ReadOpcodeEw())); cycles = 1; break;
						case 0x1: /* DEC */ WriteOpcodeEw(Dec16(ReadOpcodeEw())); cycles = 1; break;
						case 0x2: /* CALL */
							{
								var offset = ReadOpcodeEw();
								Push(ip);
								ip = offset;
								cycles = 5;
							}
							break;
						case 0x3: /* CALL Mp */
							{
								if (modRm.Mod == ModRM.Modes.Register)
								{
									RaiseInterrupt(6);
									cycles = 8;
									break;
								}
								var offset = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 0));
								var segment = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 2));
								Push(cs);
								Push(ip);
								cs = segment;
								ip = offset;
								cycles = 11;
							}
							break;
						case 0x4: /* JMP */ ip = ReadOpcodeEw(); cycles = 4; break;
						case 0x5: /* JMP Mp */
							{
								if (modRm.Mod == ModRM.Modes.Register)
								{
									RaiseInterrupt(6);
									cycles = 8;
									break;
								}
								ip = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 0));
								cs = ReadMemory16(modRm.Segment, (ushort)(modRm.Offset + 2));
								cycles = 9;
							}
							break;
						case 0x6: /* PUSH */  Push(ReadOpcodeEw()); cycles = 3; break;
						case 0x7: /* --- */ RaiseInterrupt(6); cycles = 8; break; // undocumented mirror of PUSH?
						default: RaiseInterrupt(6); cycles = 8; break;
					}
					break;

				default:
					RaiseInterrupt(6);
					cycles = 8;
					break;
			}

			ResetPrefixes();
			modRm.Reset();

			if (cycles == 0)
				throw new Exception($"Cycle count for opcode 0x{opcode:X2} is zero");

			return cycles;
		}
	}
}
