using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		delegate int Instruction(V30MZ cpu);
		readonly static Instruction[] instructions = new Instruction[256]
		{
			/* 0x00 */      /* ADD Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.Add8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* ADD Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.Add16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* ADD Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.Add8(false, cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* ADD Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.Add16(false, cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x04 */      /* ADD AL Ib */                 (cpu) => { cpu.ax.Low = cpu.Add8(false, cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* ADD AX Iw */                 (cpu) => { cpu.ax.Word = cpu.Add16(false, cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* PUSH ES */                   (cpu) => { cpu.Push(cpu.es); return 1; },
							/* POP ES */                    (cpu) => { cpu.es = cpu.Pop(); return 1; },
			/* 0x08 */      /* OR Eb Gb */                  (cpu) => { cpu.WriteOpcodeEb(cpu.Or8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* OR Ew Gw */                  (cpu) => { cpu.WriteOpcodeEw(cpu.Or16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* OR Gb Eb */                  (cpu) => { cpu.WriteOpcodeGb(cpu.Or8(cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* OR Gw Ew */                  (cpu) => { cpu.WriteOpcodeGw(cpu.Or16(cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x0C */      /* OR AL Ib */                  (cpu) => { cpu.ax.Low = cpu.Or8(cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* OR AX Iw */                  (cpu) => { cpu.ax.Word = cpu.Or16(cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* PUSH CS */                   (cpu) => { cpu.Push(cpu.cs); return 1; },
							/* (Invalid; NOP?) */			(cpu) => 3,

			/* 0x10 */      /* ADC Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.Add8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* ADC Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.Add16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* ADC Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.Add8(true, cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* ADC Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.Add16(true, cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x14 */      /* ADC AL Ib */                 (cpu) => { cpu.ax.Low = cpu.Add8(true, cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* ADC AX Iw */                 (cpu) => { cpu.ax.Word = cpu.Add16(true, cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* PUSH SS */                   (cpu) => { cpu.Push(cpu.ss); return 1; },
							/* POP SS */                    (cpu) => { cpu.ss = cpu.Pop(); return 1; },
			/* 0x18 */      /* SBB Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.Sub8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* SBB Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.Sub16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* SBB Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.Sub8(true, cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* SBB Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.Sub16(true, cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x1C */      /* SBB AL Ib */                 (cpu) => { cpu.ax.Low = cpu.Sub8(true, cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* SBB AX Iw */                 (cpu) => { cpu.ax.Word = cpu.Sub16(true, cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* PUSH DS */                   (cpu) => { cpu.Push(cpu.ds); return 1; },
							/* POP DS */                    (cpu) => { cpu.ds = cpu.Pop(); return 1; },
                            
			/* 0x20 */      /* AND Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.And8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* AND Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.And16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* AND Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.And8(cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* AND Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.And16(cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x24 */      /* AND AL Ib */                 (cpu) => { cpu.ax.Low = cpu.And8(cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* AND AX Iw */                 (cpu) => { cpu.ax.Word = cpu.And16(cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* (Prefix ES) */               (cpu) => 0,
							/* DAA */                       (cpu) => { cpu.Daa(false); return 10; },
			/* 0x28 */      /* SUB Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.Sub8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* SUB Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.Sub16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* SUB Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.Sub8(false, cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* SUB Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.Sub16(false, cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x2C */      /* SUB AL Ib */                 (cpu) => { cpu.ax.Low = cpu.Sub8(false, cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* SUB AX Iw */                 (cpu) => { cpu.ax.Word = cpu.Sub16(false, cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* (Prefix CS) */               (cpu) => 0,
							/* DAS */                       (cpu) => { cpu.Daa(true); return 10; },
                            
			/* 0x30 */      /* XOR Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.Xor8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb())); return 1; },
							/* XOR Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.Xor16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw())); return 1; },
							/* XOR Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.Xor8(cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb())); return 1; },
							/* XOR Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.Xor16(cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw())); return 1; },
			/* 0x34 */      /* XOR AL Ib */                 (cpu) => { cpu.ax.Low = cpu.Xor8(cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* XOR AX Iw */                 (cpu) => { cpu.ax.Word = cpu.Xor16(cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* (Prefix SS) */               (cpu) => 0,
							/* AAA */                       (cpu) => { cpu.Aaa(false); return 9; },
			/* 0x38 */      /* CMP Eb Gb */                 (cpu) => { cpu.Sub8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeGb()); return 1; },
							/* CMP Ew Gw */                 (cpu) => { cpu.Sub16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeGw()); return 1; },
							/* CMP Gb Eb */                 (cpu) => { cpu.Sub8(false, cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb()); return 1; },
							/* CMP Gw Ew */                 (cpu) => { cpu.Sub16(false, cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw()); return 1; },
			/* 0x3C */      /* CMP AL Ib */                 (cpu) => { cpu.Sub8(false, cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* CMP AX Iw */                 (cpu) => { cpu.Sub16(false, cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* (Prefix DS) */               (cpu) => 0,
							/* AAS */                       (cpu) => { cpu.Aaa(true); return 9; },

			/* 0x40 */      /* INC AX */                    (cpu) => { cpu.ax.Word = cpu.Inc16(cpu.ax.Word); return 1; },
							/* INC CX */                    (cpu) => { cpu.cx.Word = cpu.Inc16(cpu.cx.Word); return 1; },
							/* INC DX */                    (cpu) => { cpu.dx.Word = cpu.Inc16(cpu.dx.Word); return 1; },
							/* INC BX */                    (cpu) => { cpu.bx.Word = cpu.Inc16(cpu.bx.Word); return 1; },
			/* 0x44 */      /* INC SP */                    (cpu) => { cpu.sp = cpu.Inc16(cpu.sp); return 1; },
							/* INC BP */                    (cpu) => { cpu.bp = cpu.Inc16(cpu.bp); return 1; },
							/* INC SI */                    (cpu) => { cpu.si = cpu.Inc16(cpu.si); return 1; },
							/* INC DI */                    (cpu) => { cpu.di = cpu.Inc16(cpu.di); return 1; },
			/* 0x48 */      /* DEC AX */                    (cpu) => { cpu.ax.Word = cpu.Dec16(cpu.ax.Word); return 1; },
							/* DEC CX */                    (cpu) => { cpu.cx.Word = cpu.Dec16(cpu.cx.Word); return 1; },
							/* DEC DX */                    (cpu) => { cpu.dx.Word = cpu.Dec16(cpu.dx.Word); return 1; },
							/* DEC BX */                    (cpu) => { cpu.bx.Word = cpu.Dec16(cpu.bx.Word); return 1; },
			/* 0x4C */      /* DEC SP */                    (cpu) => { cpu.sp = cpu.Dec16(cpu.sp); return 1; },
							/* DEC BP */                    (cpu) => { cpu.bp = cpu.Dec16(cpu.bp); return 1; },
							/* DEC SI */                    (cpu) => { cpu.si = cpu.Dec16(cpu.si); return 1; },
							/* DEC DI */                    (cpu) => { cpu.di = cpu.Dec16(cpu.di); return 1; },

			/* 0x50 */      /* PUSH AX */                   (cpu) => { cpu.Push(cpu.ax.Word); return 1; },
							/* PUSH CX */                   (cpu) => { cpu.Push(cpu.cx.Word); return 1; },
							/* PUSH DX */                   (cpu) => { cpu.Push(cpu.dx.Word); return 1; },
							/* PUSH BX */                   (cpu) => { cpu.Push(cpu.bx.Word); return 1; },
			/* 0x54 */      /* PUSH SP */                   (cpu) => { cpu.Push(cpu.sp); return 1; },
							/* PUSH BP */                   (cpu) => { cpu.Push(cpu.bp); return 1; },
							/* PUSH SI */                   (cpu) => { cpu.Push(cpu.si); return 1; },
							/* PUSH DI */                   (cpu) => { cpu.Push(cpu.di); return 1; },
			/* 0x58 */      /* POP AX */                    (cpu) => { cpu.ax.Word = cpu.Pop(); return 1; },
							/* POP CX */                    (cpu) => { cpu.cx.Word = cpu.Pop(); return 1; },
							/* POP DX */                    (cpu) => { cpu.dx.Word = cpu.Pop(); return 1; },
							/* POP BX */                    (cpu) => { cpu.bx.Word = cpu.Pop(); return 1; },
			/* 0x5C */      /* POP SP */                    (cpu) => { cpu.sp = cpu.Pop(); return 1; },
							/* POP BP */                    (cpu) => { cpu.bp = cpu.Pop(); return 1; },
							/* POP SI */                    (cpu) => { cpu.si = cpu.Pop(); return 1; },
							/* POP DI */                    (cpu) => { cpu.di = cpu.Pop(); return 1; },

			/* 0x60 */      /* PUSHA */                     Opcode0x60,
							/* POPA */                      Opcode0x61,
							/* BOUND Gw E */                Opcode0x62,
							/* (Invalid; NOP?) */			(cpu) => 3,
			/* 0x64 */      /* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
			/* 0x68 */      /* PUSH Iw */                   (cpu) => { cpu.Push(cpu.ReadOpcodeIw()); return 1; },
							/* IMUL Gw Ew Iw */             (cpu) => { cpu.ReadModRM(); cpu.WriteOpcodeGw((ushort)cpu.Mul16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); return 4; },
							/* PUSH Ib */                   (cpu) => { cpu.Push((ushort)(sbyte)cpu.ReadOpcodeIb()); return 1; },
							/* IMUL Gb Eb Ib */             (cpu) => { cpu.ReadModRM(); cpu.WriteOpcodeGw((ushort)cpu.Mul16(true, cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); return 4; },
			/* 0x6C */      /* INSB */                      Opcode0x6C,
							/* INSW */                      Opcode0x6D,
							/* OUTSB */                     Opcode0x6E,
							/* OUTSW */                     Opcode0x6F,

			/* 0x70 */      /* JO */                        (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Overflow)),
							/* JNO */                       (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Overflow)),
							/* JB */                        (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Carry)),
							/* JNB */                       (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Carry)),
			/* 0x74 */      /* JZ */                        (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Zero)),
							/* JNZ */                       (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Zero)),
							/* JBE */                       (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Carry) || cpu.IsFlagSet(Flags.Zero)),
							/* JA */                        (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Carry) && !cpu.IsFlagSet(Flags.Zero)),
			/* 0x78 */      /* JS */                        (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Sign)),
							/* JNS */                       (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Sign)),
							/* JPE */                       (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Parity)),
							/* JPO */                       (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Parity)),
			/* 0x7C */      /* JL */                        (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Zero) && cpu.IsFlagSet(Flags.Sign) != cpu.IsFlagSet(Flags.Overflow)),
							/* JGE */                       (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Zero) || cpu.IsFlagSet(Flags.Sign) == cpu.IsFlagSet(Flags.Overflow)),
							/* JLE */                       (cpu) => cpu.JumpConditional(cpu.IsFlagSet(Flags.Zero) || cpu.IsFlagSet(Flags.Sign) != cpu.IsFlagSet(Flags.Overflow)),
							/* JG */                        (cpu) => cpu.JumpConditional(!cpu.IsFlagSet(Flags.Zero) && cpu.IsFlagSet(Flags.Sign) == cpu.IsFlagSet(Flags.Overflow)),

			/* 0x80 */      /* GRP1 Eb Ib */                Opcode0x80,
							/* GRP1 Ew Iw */                Opcode0x81,
							/* GRP1 Eb Ib */                Opcode0x80,
							/* GRP1 Ew Ib */                Opcode0x83,
			/* 0x84 */      /* TEST Gb Eb */                (cpu) => { cpu.And8(cpu.ReadOpcodeGb(), cpu.ReadOpcodeEb()); return 1; },
							/* TEST Gw Ew */                (cpu) => { cpu.And16(cpu.ReadOpcodeGw(), cpu.ReadOpcodeEw()); return 1; },
							/* XCHG Gb Eb */                (cpu) => { var temp = cpu.ReadOpcodeGb(); cpu.WriteOpcodeGb(cpu.ReadOpcodeEb()); cpu.WriteOpcodeEb(temp); return 3; },
							/* XCHG Gw Ew */                (cpu) => { var temp = cpu.ReadOpcodeGw(); cpu.WriteOpcodeGw(cpu.ReadOpcodeEw()); cpu.WriteOpcodeEw(temp); return 3; },
			/* 0x88 */      /* MOV Eb Gb */                 (cpu) => { cpu.WriteOpcodeEb(cpu.ReadOpcodeGb()); return 1; },
							/* MOV Ew Gw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.ReadOpcodeGw()); return 1; },
							/* MOV Gb Eb */                 (cpu) => { cpu.WriteOpcodeGb(cpu.ReadOpcodeEb()); return 1; },
							/* MOV Gw Ew */                 (cpu) => { cpu.WriteOpcodeGw(cpu.ReadOpcodeEw()); return 1; },
			/* 0x8C */      /* MOV Ew Sw */                 (cpu) => { cpu.WriteOpcodeEw(cpu.ReadOpcodeSw()); return 1; },
							/* LEA Gw M */                  Opcode0x8D,
							/* MOV Sw Ew */                 (cpu) => { cpu.WriteOpcodeSw(cpu.ReadOpcodeEw()); return 1; },
							/* POP Ew */                    (cpu) => { cpu.WriteOpcodeEw(cpu.Pop()); return 1; },

			/* 0x90 */      /* NOP (XCHG AX AX) */          (cpu) => { Exchange(ref cpu.ax.Word, ref cpu.ax.Word); return 3; },
							/* XCHG CX AX */                (cpu) => { Exchange(ref cpu.cx.Word, ref cpu.ax.Word); return 3; },
							/* XCHG DX AX */                (cpu) => { Exchange(ref cpu.dx.Word, ref cpu.ax.Word); return 3; },
							/* XCHG BX AX */                (cpu) => { Exchange(ref cpu.bx.Word, ref cpu.ax.Word); return 3; },
			/* 0x94 */      /* XCHG SP AX */                (cpu) => { Exchange(ref cpu.sp, ref cpu.ax.Word); return 3; },
							/* XCHG BP AX */                (cpu) => { Exchange(ref cpu.bp, ref cpu.ax.Word); return 3; },
							/* XCHG SI AX */                (cpu) => { Exchange(ref cpu.si, ref cpu.ax.Word); return 3; },
							/* XCHG DI AX */                (cpu) => { Exchange(ref cpu.di, ref cpu.ax.Word); return 3; },
			/* 0x98 */      /* CBW */                       (cpu) => { cpu.ax.Word = (ushort)(sbyte)cpu.ax.Low; return 2; },
							/* CWD */                       (cpu) => { var value = (uint)(short)cpu.ax.Word; cpu.dx.Word = (ushort)((value >> 16) & 0xFFFF); cpu.ax.Word = (ushort)((value >> 0) & 0xFFFF); return 2; },
							/* CALL Ap */                   Opcode0x9A,
							/* WAIT */                      (cpu) => 1,
			/* 0x9C */      /* PUSHF */                     (cpu) => { cpu.Push((ushort)cpu.flags); return 1; },
							/* POPF */                      (cpu) => { cpu.flags = (Flags)cpu.Pop(); return 1; },
							/* SAHF */                      Opcode0x9E,
							/* LAHF */                      (cpu) => { cpu.ax.High = (byte)cpu.flags; return 2; },

			/* 0xA0 */      /* MOV AL Aw */                 (cpu) => { cpu.ax.Low = cpu.ReadMemory8(cpu.GetSegmentViaOverride(SegmentNumber.DS), cpu.ReadOpcodeIw()); return 1; },
							/* MOV AX Aw */                 (cpu) => { cpu.ax.Word = cpu.ReadMemory16(cpu.GetSegmentViaOverride(SegmentNumber.DS), cpu.ReadOpcodeIw()); return 1; },
							/* MOV Aw AL */                 (cpu) => { cpu.WriteMemory8(cpu.GetSegmentViaOverride(SegmentNumber.DS), cpu.ReadOpcodeIw(), cpu.ax.Low); return 1; },
							/* MOV Aw AX */                 (cpu) => { cpu.WriteMemory16(cpu.GetSegmentViaOverride(SegmentNumber.DS), cpu.ReadOpcodeIw(), cpu.ax.Word); return 1; },
			/* 0xA4 */      /* MOVSB */                     Opcode0xA4,
							/* MOVSW */                     Opcode0xA5,
							/* CMPSB */                     Opcode0xA6,
							/* CMPSW */                     Opcode0xA7,
			/* 0xA8 */      /* TEST AL Ib */                (cpu) => { cpu.And8(cpu.ax.Low, cpu.ReadOpcodeIb()); return 1; },
							/* TEST AX Iw */                (cpu) => { cpu.And16(cpu.ax.Word, cpu.ReadOpcodeIw()); return 1; },
							/* STOSB */                     Opcode0xAA,
							/* STOSW */                     Opcode0xAB,
			/* 0xAC */      /* LODSB */                     Opcode0xAC,
							/* LODSW */                     Opcode0xAD,
							/* SCASB */                     Opcode0xAE,
							/* SCASW */                     Opcode0xAF,

			/* 0xB0 */      /* MOV AL Ib */                 (cpu) => { cpu.ax.Low = cpu.ReadOpcodeIb(); return 1; },
							/* MOV CL Ib */                 (cpu) => { cpu.cx.Low = cpu.ReadOpcodeIb(); return 1; },
							/* MOV DL Ib */                 (cpu) => { cpu.dx.Low = cpu.ReadOpcodeIb(); return 1; },
							/* MOV BL Ib */                 (cpu) => { cpu.bx.Low = cpu.ReadOpcodeIb(); return 1; },
			/* 0xB4 */      /* MOV AH Ib */                 (cpu) => { cpu.ax.High = cpu.ReadOpcodeIb(); return 1; },
							/* MOV CH Ib */                 (cpu) => { cpu.cx.High = cpu.ReadOpcodeIb(); return 1; },
							/* MOV DH Ib */                 (cpu) => { cpu.dx.High = cpu.ReadOpcodeIb(); return 1; },
							/* MOV BH Ib */                 (cpu) => { cpu.bx.High = cpu.ReadOpcodeIb(); return 1; },
			/* 0xB8 */      /* MOV AX Iw */                 (cpu) => { cpu.ax.Word = cpu.ReadOpcodeIw(); return 1; },
							/* MOV CX Iw */                 (cpu) => { cpu.cx.Word = cpu.ReadOpcodeIw(); return 1; },
							/* MOV DX Iw */                 (cpu) => { cpu.dx.Word = cpu.ReadOpcodeIw(); return 1; },
							/* MOV BX Iw */                 (cpu) => { cpu.bx.Word = cpu.ReadOpcodeIw(); return 1; },
			/* 0xBC */      /* MOV SP Iw */                 (cpu) => { cpu.sp = cpu.ReadOpcodeIw(); return 1; },
							/* MOV BP Iw */                 (cpu) => { cpu.bp = cpu.ReadOpcodeIw(); return 1; },
							/* MOV SI Iw */                 (cpu) => { cpu.si = cpu.ReadOpcodeIw(); return 1; },
							/* MOV DI Iw */                 (cpu) => { cpu.di = cpu.ReadOpcodeIw(); return 1; },

			/* 0xC0 */      /* GRP2 Eb Ib */                Opcode0xC0,
							/* GRP2 Ew Ib */                Opcode0xC1,
							/* RET Iw */                    (cpu) => { var offset = cpu.ReadOpcodeIw(); cpu.ip = cpu.Pop(); cpu.sp += offset; return 5; },
							/* RET */                       (cpu) => { cpu.ip = cpu.Pop(); return 5; },
			/* 0xC4 */      /* LES Gw Mp */                 Opcode0xC4,
							/* LDS Gw Mp */                 Opcode0xC5,
							/* MOV Eb Ib */                 (cpu) => { cpu.ReadModRM(); cpu.WriteOpcodeEb(cpu.ReadOpcodeIb()); return 1; },
							/* MOV Ew Iw */                 (cpu) => { cpu.ReadModRM(); cpu.WriteOpcodeEw(cpu.ReadOpcodeIw()); return 1; },
			/* 0xC8 */      /* ENTER */                     Opcode0xC8,
							/* LEAVE */                     (cpu) => { cpu.sp = cpu.bp; cpu.bp = cpu.Pop(); return 1; },
							/* RETF Iw */                   (cpu) => { var offset = cpu.ReadOpcodeIw(); cpu.ip = cpu.Pop(); cpu.cs = cpu.Pop(); cpu.sp += offset; return 8; },
							/* RETF */                      (cpu) => { cpu.ip = cpu.Pop(); cpu.cs = cpu.Pop(); return 7; },
			/* 0xCC */      /* INT 3 */                     (cpu) => { cpu.Interrupt(3); return 8; },
							/* INT Ib */                    (cpu) => { cpu.Interrupt(cpu.ReadOpcodeIb()); return 9; },
							/* INTO */                      (cpu) => { if (cpu.IsFlagSet(Flags.Overflow)) cpu.Interrupt(4); return 5; },
							/* IRET */                      (cpu) => { cpu.ip = cpu.Pop(); cpu.cs = cpu.Pop(); cpu.flags = (Flags)cpu.Pop(); return 9; },

			/* 0xD0 */      /* GRP2 Eb 1 */                 Opcode0xD0,
							/* GRP2 Ew 1 */                 Opcode0xD1,
							/* GRP2 Eb CL */                Opcode0xD2,
							/* GRP2 Ew CL */                Opcode0xD3,
			/* 0xD4 */      /* AAM */                       Opcode0xD4,
							/* AAD */                       Opcode0xD5,
							/* (undocumented XLAT) */       (cpu) => { cpu.ax.Low = cpu.ReadMemory8(cpu.GetSegmentViaOverride(SegmentNumber.DS), (ushort)(cpu.bx.Word + cpu.ax.Low)); return 4; },
							/* XLAT */                      (cpu) => { cpu.ax.Low = cpu.ReadMemory8(cpu.GetSegmentViaOverride(SegmentNumber.DS), (ushort)(cpu.bx.Word + cpu.ax.Low)); return 4; },
			/* 0xD8 */      /* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
			/* 0xDC */      /* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Invalid; NOP?) */			(cpu) => 3,

			/* 0xE0 */      /* LOOPNZ Jb */                 (cpu) => { return cpu.LoopWhile(!cpu.IsFlagSet(Flags.Zero)); },
							/* LOOPZ Jb */                  (cpu) => { return cpu.LoopWhile(cpu.IsFlagSet(Flags.Zero)); },
							/* LOOP Jb */                   (cpu) => { return cpu.Loop(); },
							/* JCXZ */                      (cpu) => { return cpu.JumpConditional(cpu.cx.Word == 0); },
			/* 0xE4 */      /* IN Ib AL */                  (cpu) => { cpu.ax.Low = cpu.machine.ReadPort(cpu.ReadOpcodeIb()); return 6; },
							/* IN Ib AX */                  (cpu) => { cpu.ax.Word = cpu.ReadPort16(cpu.ReadOpcodeIb()); return 6; },
							/* OUT Ib AL */                 (cpu) => { cpu.machine.WritePort(cpu.ReadOpcodeIb(), cpu.ax.Low); return 6; },
							/* OUT Ib AX */                 (cpu) => { cpu.WritePort16(cpu.ReadOpcodeIb(), cpu.ax.Word); return 6; },
			/* 0xE8 */      /* CALL Jv */                   (cpu) => { var offset = cpu.ReadOpcodeIw(); cpu.Push(cpu.ip); cpu.ip += offset; return 4; },
							/* JMP Jv */                    (cpu) => { var offset = cpu.ReadOpcodeIw(); cpu.ip += offset; return 3; },
							/* JMP Ap */                    (cpu) => { var newIp = cpu.ReadOpcodeIw(); var newCs = cpu.ReadOpcodeIw(); cpu.ip = newIp; cpu.cs = newCs; return 5; },
							/* JMP Jb */                    (cpu) => { cpu.ip = cpu.ReadOpcodeJb(); return 3; },
			/* 0xEC */      /* IN AL DX */                  (cpu) => { cpu.ax.Low = cpu.machine.ReadPort(cpu.dx.Word); return 4; },
							/* IN AX DX */                  (cpu) => { cpu.ax.Word = cpu.ReadPort16(cpu.dx.Word); return 4; },
							/* OUT DX AL */                 (cpu) => { cpu.machine.WritePort(cpu.dx.Word, cpu.ax.Low); return 4; },
							/* OUT DX AX */                 (cpu) => { cpu.WritePort16(cpu.dx.Word, cpu.ax.Word); return 4; },

			/* 0xF0 */      /* (Prefix LOCK) */             (cpu) => 0,
							/* (Invalid; NOP?) */			(cpu) => 3,
							/* (Prefix REPNZ) */            (cpu) => 0,
							/* (Prefix REPZ) */             (cpu) => 0,
			/* 0xF4 */      /* HLT */                       (cpu) => { cpu.halted = true; return 8; },
							/* CMC */                       (cpu) => { cpu.SetClearFlagConditional(Flags.Carry, !cpu.IsFlagSet(Flags.Carry)); return 4; },
							/* GRP3 Eb */                   Opcode0xF6,
							/* GRP3 Ew */                   Opcode0xF7,
			/* 0xF8 */      /* CLC */                       (cpu) => { cpu.ClearFlags(Flags.Carry); return 4; },
							/* STC */                       (cpu) => { cpu.SetFlags(Flags.Carry); return 4; },
							/* CLI */                       (cpu) => { cpu.ClearFlags(Flags.InterruptEnable); return 4; },
							/* STI */                       (cpu) => { cpu.SetFlags(Flags.InterruptEnable); return 4; },
			/* 0xFC */      /* CLD */                       (cpu) => { cpu.ClearFlags(Flags.Direction); return 4; },
							/* STD */                       (cpu) => { cpu.SetFlags(Flags.Direction); return 4; },
							/* GRP4 Eb */                   Opcode0xFE,
							/* GRP4 Ew */                   Opcode0xFF
		};

		private static int Opcode0x60(V30MZ cpu)
		{
			/* PUSHA */
			var oldSp = cpu.sp;
			cpu.Push(cpu.ax.Word);
			cpu.Push(cpu.cx.Word);
			cpu.Push(cpu.dx.Word);
			cpu.Push(cpu.bx.Word);
			cpu.Push(oldSp);
			cpu.Push(cpu.bp);
			cpu.Push(cpu.si);
			cpu.Push(cpu.di);
			return 8;
		}

		private static int Opcode0x61(V30MZ cpu)
		{
			/* POPA */
			cpu.di = cpu.Pop();
			cpu.si = cpu.Pop();
			cpu.bp = cpu.Pop();
			cpu.Pop(); /* don't restore SP */
			cpu.bx.Word = cpu.Pop();
			cpu.dx.Word = cpu.Pop();
			cpu.cx.Word = cpu.Pop();
			cpu.ax.Word = cpu.Pop();
			return 8;
		}

		private static int Opcode0x62(V30MZ cpu)
		{
			/* BOUND Gw E */
			cpu.ReadModRM();
			var lo = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 0));
			var hi = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 2));
			var reg = cpu.GetRegister16((RegisterNumber16)cpu.modRm.Mem);
			if (reg < lo || reg > hi) cpu.Interrupt(5);
			return 12;
		}

		private static int Opcode0x6C(V30MZ cpu)
		{
			/* INSB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.InString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.InString(false); cycles += 5; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0x6D(V30MZ cpu)
		{
			/* INSW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.InString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.InString(true); cycles += 5; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0x6E(V30MZ cpu)
		{
			/* OUTSB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.OutString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.OutString(false); cycles += 6; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0x6F(V30MZ cpu)
		{
			/* OUTSW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.OutString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.OutString(true); cycles += 6; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0x80(V30MZ cpu)
		{
			/* GRP1 Eb Ib */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ADD */ cpu.WriteOpcodeEb(cpu.Add8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x1: /* OR  */ cpu.WriteOpcodeEb(cpu.Or8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x2: /* ADC */ cpu.WriteOpcodeEb(cpu.Add8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x3: /* SBB */ cpu.WriteOpcodeEb(cpu.Sub8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x4: /* AND */ cpu.WriteOpcodeEb(cpu.And8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x5: /* SUB */ cpu.WriteOpcodeEb(cpu.Sub8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x6: /* XOR */ cpu.WriteOpcodeEb(cpu.Xor8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x7: /* CMP */ cpu.Sub8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb()); cycles = 1; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0x81(V30MZ cpu)
		{
			/* GRP1 Ew Iw */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ADD */ cpu.WriteOpcodeEw(cpu.Add16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x1: /* OR  */ cpu.WriteOpcodeEw(cpu.Or16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x2: /* ADC */ cpu.WriteOpcodeEw(cpu.Add16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x3: /* SBB */ cpu.WriteOpcodeEw(cpu.Sub16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x4: /* AND */ cpu.WriteOpcodeEw(cpu.And16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x5: /* SUB */ cpu.WriteOpcodeEw(cpu.Sub16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x6: /* XOR */ cpu.WriteOpcodeEw(cpu.Xor16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw())); cycles = 1; break;
				case 0x7: /* CMP */ cpu.Sub16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw()); cycles = 1; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0x83(V30MZ cpu)
		{
			/* GRP1 Ew Ib */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ADD */ cpu.WriteOpcodeEw(cpu.Add16(false, cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x1: /* OR  */ cpu.WriteOpcodeEw(cpu.Or16(cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x2: /* ADC */ cpu.WriteOpcodeEw(cpu.Add16(true, cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x3: /* SBB */ cpu.WriteOpcodeEw(cpu.Sub16(true, cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x4: /* AND */ cpu.WriteOpcodeEw(cpu.And16(cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x5: /* SUB */ cpu.WriteOpcodeEw(cpu.Sub16(false, cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x6: /* XOR */ cpu.WriteOpcodeEw(cpu.Xor16(cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb())); cycles = 1; break;
				case 0x7: /* CMP */ cpu.Sub16(false, cpu.ReadOpcodeEw(), (ushort)(sbyte)cpu.ReadOpcodeIb()); cycles = 1; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0x8D(V30MZ cpu)
		{
			/* LEA Gw M */
			cpu.ReadModRM();
			if (cpu.modRm.Mod != ModRM.Modes.Register)
			{
				cpu.WriteOpcodeGw(cpu.modRm.Offset);
			}
			return 8;
		}

		private static int Opcode0x9A(V30MZ cpu)
		{
			/* CALL Ap */
			var newIp = cpu.ReadOpcodeIw();
			var newCs = cpu.ReadOpcodeIw();

			cpu.Push(cpu.cs);
			cpu.Push(cpu.ip);

			cpu.ip = newIp;
			cpu.cs = newCs;

			return 9;
		}

		private static int Opcode0x9E(V30MZ cpu)
		{
			/* SAHF */
			cpu.SetClearFlagConditional(Flags.Sign, ((Flags)cpu.ax.High & Flags.Sign) == Flags.Sign);
			cpu.SetClearFlagConditional(Flags.Zero, ((Flags)cpu.ax.High & Flags.Zero) == Flags.Zero);
			cpu.SetClearFlagConditional(Flags.Auxiliary, ((Flags)cpu.ax.High & Flags.Auxiliary) == Flags.Auxiliary);
			cpu.SetClearFlagConditional(Flags.Parity, ((Flags)cpu.ax.High & Flags.Parity) == Flags.Parity);
			cpu.SetClearFlagConditional(Flags.Carry, ((Flags)cpu.ax.High & Flags.Carry) == Flags.Carry);
			return 4;
		}

		private static int Opcode0xA4(V30MZ cpu)
		{
			/* MOVSB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.MoveString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.MoveString(false); cycles += 5; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0xA5(V30MZ cpu)
		{
			/* MOVSW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.MoveString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.MoveString(true); cycles += 5; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0xA6(V30MZ cpu)
		{
			/* CMPSB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.CompareString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.CompareString(false); cycles += 4; } while (--cpu.cx.Word != 0 && (cpu.prefixRepeatOnNotEqual ? !cpu.IsFlagSet(Flags.Zero) : cpu.IsFlagSet(Flags.Zero)));
			}
			return cycles;
		}

		private static int Opcode0xA7(V30MZ cpu)
		{
			/* CMPSW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.CompareString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.CompareString(true); cycles += 4; } while (--cpu.cx.Word != 0 && (cpu.prefixRepeatOnNotEqual ? !cpu.IsFlagSet(Flags.Zero) : cpu.IsFlagSet(Flags.Zero)));
			}
			return cycles;
		}

		private static int Opcode0xAA(V30MZ cpu)
		{
			/* STOSB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.StoreString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.StoreString(false); cycles += 2; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0xAB(V30MZ cpu)
		{
			/* STOSW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.StoreString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.StoreString(true); cycles += 2; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0xAC(V30MZ cpu)
		{
			/* LODSB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.LoadString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.LoadString(false); cycles += 2; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0xAD(V30MZ cpu)
		{
			/* LODSW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.LoadString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.LoadString(true); cycles += 2; } while (--cpu.cx.Word != 0);
			}
			return cycles;
		}

		private static int Opcode0xAE(V30MZ cpu)
		{
			/* SCASB */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.ScanString(false);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.ScanString(false); cycles += 3; } while (--cpu.cx.Word != 0 && (cpu.prefixRepeatOnNotEqual ? !cpu.IsFlagSet(Flags.Zero) : cpu.IsFlagSet(Flags.Zero)));
			}
			return cycles;
		}

		private static int Opcode0xAF(V30MZ cpu)
		{
			/* SCASW */
			var cycles = 5;
			if (!cpu.prefixHasRepeat)
				cpu.ScanString(true);
			else if (cpu.cx.Word != 0)
			{
				do { cpu.ScanString(true); cycles += 3; } while (--cpu.cx.Word != 0 && (cpu.prefixRepeatOnNotEqual ? !cpu.IsFlagSet(Flags.Zero) : cpu.IsFlagSet(Flags.Zero)));
			}
			return cycles;
		}

		private static int Opcode0xC0(V30MZ cpu)
		{
			/* GRP2 Eb Ib */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ROL */ cpu.WriteOpcodeEb(cpu.Rol8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x1: /* ROR */ cpu.WriteOpcodeEb(cpu.Ror8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x2: /* RCL */ cpu.WriteOpcodeEb(cpu.Rol8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x3: /* RCR */ cpu.WriteOpcodeEb(cpu.Ror8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x4: /* SHL */ cpu.WriteOpcodeEb(cpu.Shl8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x5: /* SHR */ cpu.WriteOpcodeEb(cpu.Shr8(false, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x6: /* --- */ cycles = 3; break;
				case 0x7: /* SAR */ cpu.WriteOpcodeEb(cpu.Shr8(true, cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb())); cycles = 3; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xC1(V30MZ cpu)
		{
			/* GRP2 Ew Ib */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ROL */ cpu.WriteOpcodeEw(cpu.Rol16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x1: /* ROR */ cpu.WriteOpcodeEw(cpu.Ror16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x2: /* RCL */ cpu.WriteOpcodeEw(cpu.Rol16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x3: /* RCR */ cpu.WriteOpcodeEw(cpu.Ror16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x4: /* SHL */ cpu.WriteOpcodeEw(cpu.Shl16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x5: /* SHR */ cpu.WriteOpcodeEw(cpu.Shr16(false, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				case 0x6: /* --- */ cycles = 3; break;
				case 0x7: /* SAR */ cpu.WriteOpcodeEw(cpu.Shr16(true, cpu.ReadOpcodeEw(), cpu.ReadOpcodeIb())); cycles = 3; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xC4(V30MZ cpu)
		{
			/* LES Gw Mp */
			cpu.ReadModRM();
			if (cpu.modRm.Mod != ModRM.Modes.Register)
			{
				cpu.WriteOpcodeGw(cpu.ReadOpcodeEw());
				cpu.es = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 2));
			}
			return 8;
		}

		private static int Opcode0xC5(V30MZ cpu)
		{
			/* LDS Gw Mp */
			cpu.ReadModRM();
			if (cpu.modRm.Mod != ModRM.Modes.Register)
			{
				cpu.WriteOpcodeGw(cpu.ReadOpcodeEw());
				cpu.ds = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 2));
			}
			return 8;
		}

		private static int Opcode0xC8(V30MZ cpu)
		{
			/* ENTER */
			var offset = cpu.ReadOpcodeIw();
			var length = (byte)(cpu.ReadOpcodeIb() & 0x1F);

			cpu.Push(cpu.bp);
			cpu.bp = cpu.sp;
			cpu.sp -= offset;

			if (length != 0)
			{
				for (var i = 1; i < length; i++)
					cpu.Push(cpu.ReadMemory16(cpu.ss, (ushort)(cpu.bp - i * 2)));
				cpu.Push(cpu.bp);
			}
			return 7;
		}

		private static int Opcode0xD0(V30MZ cpu)
		{
			/* GRP2 Eb 1 */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ROL */ cpu.WriteOpcodeEb(cpu.Rol8(false, cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				case 0x1: /* ROR */ cpu.WriteOpcodeEb(cpu.Ror8(false, cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				case 0x2: /* RCL */ cpu.WriteOpcodeEb(cpu.Rol8(true, cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				case 0x3: /* RCR */ cpu.WriteOpcodeEb(cpu.Ror8(true, cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				case 0x4: /* SHL */ cpu.WriteOpcodeEb(cpu.Shl8(cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				case 0x5: /* SHR */ cpu.WriteOpcodeEb(cpu.Shr8(false, cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				case 0x6: /* --- */ cycles = 3; break;
				case 0x7: /* SAR */ cpu.WriteOpcodeEb(cpu.Shr8(true, cpu.ReadOpcodeEb(), 1)); cycles = 1; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xD1(V30MZ cpu)
		{
			/* GRP2 Ew 1 */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ROL */ cpu.WriteOpcodeEw(cpu.Rol16(false, cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				case 0x1: /* ROR */ cpu.WriteOpcodeEw(cpu.Ror16(false, cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				case 0x2: /* RCL */ cpu.WriteOpcodeEw(cpu.Rol16(true, cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				case 0x3: /* RCR */ cpu.WriteOpcodeEw(cpu.Ror16(true, cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				case 0x4: /* SHL */ cpu.WriteOpcodeEw(cpu.Shl16(cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				case 0x5: /* SHR */ cpu.WriteOpcodeEw(cpu.Shr16(false, cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				case 0x6: /* --- */ cycles = 3; break;
				case 0x7: /* SAR */ cpu.WriteOpcodeEw(cpu.Shr16(true, cpu.ReadOpcodeEw(), 1)); cycles = 1; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xD2(V30MZ cpu)
		{
			/* GRP2 Eb CL */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ROL */ cpu.WriteOpcodeEb(cpu.Rol8(false, cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				case 0x1: /* ROR */ cpu.WriteOpcodeEb(cpu.Ror8(false, cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				case 0x2: /* RCL */ cpu.WriteOpcodeEb(cpu.Rol8(true, cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				case 0x3: /* RCR */ cpu.WriteOpcodeEb(cpu.Ror8(true, cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				case 0x4: /* SHL */ cpu.WriteOpcodeEb(cpu.Shl8(cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				case 0x5: /* SHR */ cpu.WriteOpcodeEb(cpu.Shr8(false, cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				case 0x6: /* --- */ cycles = 3; break;
				case 0x7: /* SAR */ cpu.WriteOpcodeEb(cpu.Shr8(true, cpu.ReadOpcodeEb(), cpu.cx.Low)); cycles = 3; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xD3(V30MZ cpu)
		{
			/* GRP2 Ew CL */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* ROL */ cpu.WriteOpcodeEw(cpu.Rol16(false, cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				case 0x1: /* ROR */ cpu.WriteOpcodeEw(cpu.Ror16(false, cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				case 0x2: /* RCL */ cpu.WriteOpcodeEw(cpu.Rol16(true, cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				case 0x3: /* RCR */ cpu.WriteOpcodeEw(cpu.Ror16(true, cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				case 0x4: /* SHL */ cpu.WriteOpcodeEw(cpu.Shl16(cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				case 0x5: /* SHR */ cpu.WriteOpcodeEw(cpu.Shr16(false, cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				case 0x6: /* --- */ cycles = 3; break;
				case 0x7: /* SAR */ cpu.WriteOpcodeEw(cpu.Shr16(true, cpu.ReadOpcodeEw(), cpu.cx.Low)); cycles = 3; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		/* NOTE: AAM/AAD: While NEC V20/V30 do ignore immediate & always use base 10 (1), V30MZ does *not* ignore the immediate (2)
		 * (1): https://www.vcfed.org/forum/forum/technical-support/vintage-computer-programming/36551/
		 * (2): https://github.com/xdanieldzd/StoicGoose/issues/9
		 */

		private static int Opcode0xD4(V30MZ cpu)
		{
			/* AAM */
			var value = cpu.ReadOpcodeIb();
			if (value == 0)
			{
				/* Division-by-zero exception */
				cpu.Interrupt(0);
			}
			else
			{
				cpu.ax.High = (byte)(cpu.ax.Low / value);
				cpu.ax.Low = (byte)(cpu.ax.Low % value);
				cpu.SetClearFlagConditional(Flags.Parity, CalculateParity(cpu.ax.Low));
				cpu.SetClearFlagConditional(Flags.Zero, (cpu.ax.Word & 0xFFFF) == 0);
				cpu.SetClearFlagConditional(Flags.Sign, (cpu.ax.Word & 0x8000) != 0);
			}
			return 16;
		}

		private static int Opcode0xD5(V30MZ cpu)
		{
			/* AAD */
			var value = cpu.ReadOpcodeIb();
			cpu.ax.Low = (byte)(cpu.ax.High * value + cpu.ax.Low);
			cpu.ax.High = 0;
			cpu.SetClearFlagConditional(Flags.Parity, CalculateParity(cpu.ax.Low));
			cpu.SetClearFlagConditional(Flags.Zero, (cpu.ax.Word & 0xFFFF) == 0);
			cpu.SetClearFlagConditional(Flags.Sign, (cpu.ax.Word & 0x8000) != 0);
			return 6;
		}

		private static int Opcode0xF6(V30MZ cpu)
		{
			/* GRP3 Eb */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* TEST */ cpu.And8(cpu.ReadOpcodeEb(), cpu.ReadOpcodeIb()); cycles = 1; break;
				case 0x1: /* --- */ cycles = 3; break;
				case 0x2: /* NOT */ cpu.WriteOpcodeEb((byte)~cpu.ReadOpcodeEb()); cycles = 1; break;
				case 0x3: /* NEG */ cpu.WriteOpcodeEb(cpu.Neg8(cpu.ReadOpcodeEb())); cycles = 1; break;
				case 0x4: /* MUL */ cpu.ax.Word = cpu.Mul8(false, cpu.ax.Low, cpu.ReadOpcodeEb()); cycles = 3; break;
				case 0x5: /* IMUL */ cpu.ax.Word = cpu.Mul8(true, cpu.ax.Low, cpu.ReadOpcodeEb()); cycles = 3; break;
				case 0x6: /* DIV */ cpu.ax.Word = cpu.Div8(false, cpu.ax.Word, cpu.ReadOpcodeEb()); cycles = 15; break;
				case 0x7: /* IDIV */ cpu.ax.Word = cpu.Div8(true, cpu.ax.Word, cpu.ReadOpcodeEb()); cycles = 17; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xF7(V30MZ cpu)
		{
			/* GRP3 Ew */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* TEST */ cpu.And16(cpu.ReadOpcodeEw(), cpu.ReadOpcodeIw()); cycles = 1; break;
				case 0x1: /* --- */ cycles = 3; break;
				case 0x2: /* NOT */ cpu.WriteOpcodeEw((ushort)~cpu.ReadOpcodeEw()); cycles = 1; break;
				case 0x3: /* NEG */ cpu.WriteOpcodeEw(cpu.Neg16(cpu.ReadOpcodeEw())); cycles = 1; break;
				case 0x4: /* MUL */ { var result = cpu.Mul16(false, cpu.ax.Word, cpu.ReadOpcodeEw()); cpu.dx.Word = (ushort)((result >> 16) & 0xFFFF); cpu.ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 3; } break;
				case 0x5: /* IMUL */ { var result = cpu.Mul16(true, cpu.ax.Word, cpu.ReadOpcodeEw()); cpu.dx.Word = (ushort)((result >> 16) & 0xFFFF); cpu.ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 3; } break;
				case 0x6: /* DIV */ { var result = cpu.Div16(false, (uint)(cpu.dx.Word << 16 | cpu.ax.Word), cpu.ReadOpcodeEw()); cpu.dx.Word = (ushort)((result >> 16) & 0xFFFF); cpu.ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 23; } break;
				case 0x7: /* IDIV */ { var result = cpu.Div16(true, (uint)(cpu.dx.Word << 16 | cpu.ax.Word), cpu.ReadOpcodeEw()); cpu.dx.Word = (ushort)((result >> 16) & 0xFFFF); cpu.ax.Word = (ushort)((result >> 0) & 0xFFFF); cycles = 24; } break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xFE(V30MZ cpu)
		{
			/* GRP4 Eb */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* INC */ cpu.WriteOpcodeEb(cpu.Inc8(cpu.ReadOpcodeEb())); cycles = 1; break;
				case 0x1: /* DEC */ cpu.WriteOpcodeEb(cpu.Dec8(cpu.ReadOpcodeEb())); cycles = 1; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}

		private static int Opcode0xFF(V30MZ cpu)
		{
			/* GRP4 Ew */
			int cycles;
			cpu.ReadModRM();
			switch (cpu.modRm.Reg)
			{
				case 0x0: /* INC */ cpu.WriteOpcodeEw(cpu.Inc16(cpu.ReadOpcodeEw())); cycles = 1; break;
				case 0x1: /* DEC */ cpu.WriteOpcodeEw(cpu.Dec16(cpu.ReadOpcodeEw())); cycles = 1; break;
				case 0x2: /* CALL */
					{
						var offset = cpu.ReadOpcodeEw();
						cpu.Push(cpu.ip);
						cpu.ip = offset;
						cycles = 5;
					}
					break;
				case 0x3: /* CALL Mp */
					{
						if (cpu.modRm.Mod != ModRM.Modes.Register)
						{
							var offset = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 0));
							var segment = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 2));
							cpu.Push(cpu.cs);
							cpu.Push(cpu.ip);
							cpu.cs = segment;
							cpu.ip = offset;
						}
						cycles = 11;
					}
					break;
				case 0x4: /* JMP */ cpu.ip = cpu.ReadOpcodeEw(); cycles = 4; break;
				case 0x5: /* JMP Mp */
					{
						if (cpu.modRm.Mod != ModRM.Modes.Register)
						{
							cpu.ip = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 0));
							cpu.cs = cpu.ReadMemory16(cpu.modRm.Segment, (ushort)(cpu.modRm.Offset + 2));
						}
						cycles = 9;
					}
					break;
				case 0x6: /* PUSH */ cpu.Push(cpu.ReadOpcodeEw()); cycles = 3; break;
				case 0x7: /* --- */ cycles = 3; break;
				default: throw new Exception("Invalid opcode");
			}
			return cycles;
		}
	}
}
