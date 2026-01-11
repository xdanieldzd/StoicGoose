using OpenTK.Audio.OpenAL;
using StoicGoose.Common.OpenGL;

namespace StoicGoose.Core.CPU
{
    public sealed partial class V30MZ
    {
        private byte Add8(bool withCarry, byte a, byte b)
        {
            int result = a + b + (withCarry && IsFlagSet(Flags.Carry) ? 1 : 0);

            // CF, PF, AF, ZF, SF, OF = according to result
            SetClearFlagConditional(Flags.Carry, (result & 0x100) != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ b) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Add16(bool withCarry, ushort a, ushort b)
        {
            int result = a + b + (withCarry && IsFlagSet(Flags.Carry) ? 1 : 0);

            // CF, PF, AF, ZF, SF, OF = according to result
            SetClearFlagConditional(Flags.Carry, (result & 0x10000) != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ b) & 0x8000) != 0);

            return (ushort)result;
        }

        private byte Or8(byte a, byte b)
        {
            int result = a | b;

            // CF, AF, OF = cleared; PF, ZF, SF = according to result
            ClearFlags(Flags.Carry);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            ClearFlags(Flags.Overflow);

            return (byte)result;
        }

        private ushort Or16(ushort a, ushort b)
        {
            int result = a | b;

            // CF, AF, OF = cleared; PF, ZF, SF = according to result
            ClearFlags(Flags.Carry);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            ClearFlags(Flags.Overflow);

            return (ushort)result;
        }

        private byte Sub8(bool withBorrow, byte a, byte b)
        {
            int result = a - (b + (withBorrow && IsFlagSet(Flags.Carry) ? 1 : 0));

            // CF, PF, AF, ZF, SF, OF = according to result
            SetClearFlagConditional(Flags.Carry, (result & 0x100) != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ b) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Sub16(bool withBorrow, ushort a, ushort b)
        {
            int result = a - (b + (withBorrow && IsFlagSet(Flags.Carry) ? 1 : 0));

            // CF, PF, AF, ZF, SF, OF = according to result
            SetClearFlagConditional(Flags.Carry, (result & 0x10000) != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ b) & 0x8000) != 0);

            return (ushort)result;
        }

        private byte And8(byte a, byte b)
        {
            int result = a & b;

            // CF, AF, OF = cleared; PF, ZF, SF = according to result
            ClearFlags(Flags.Carry);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            ClearFlags(Flags.Overflow);

            return (byte)result;
        }

        private ushort And16(ushort a, ushort b)
        {
            int result = a & b;

            // CF, AF, OF = cleared; PF, ZF, SF = according to result
            ClearFlags(Flags.Carry);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            ClearFlags(Flags.Overflow);

            return (ushort)result;
        }

        private void Daa(bool isSubtract)
        {
            byte al = ax.Low;

            ClearFlags(Flags.Overflow);

            if (((al & 0x0F) > 0x09) || IsFlagSet(Flags.Auxiliary))
            {
                byte oldAl = al;
                al += (byte)(isSubtract ? -0x06 : 0x06);
                SetFlags(Flags.Auxiliary);
                SetClearFlagConditional(Flags.Overflow, ((al ^ oldAl) & ((isSubtract ? oldAl : al) ^ 0x06) & 0x80) != 0);
            }
            else
                ClearFlags(Flags.Auxiliary);

            if ((ax.Low > 0x99) || IsFlagSet(Flags.Carry))
            {
                byte oldAl = al;
                al += (byte)(isSubtract ? -0x60 : 0x60);
                SetFlags(Flags.Carry);
                SetClearFlagConditional(Flags.Overflow, IsFlagSet(Flags.Overflow) || ((al ^ oldAl) & ((isSubtract ? oldAl : al) ^ 0x60) & 0x80) != 0);
            }
            else
                ClearFlags(Flags.Carry);

            ax.Low = al;

            SetClearFlagConditional(Flags.Parity, CalculateParity(ax.Low & 0xFF));
            SetClearFlagConditional(Flags.Zero, (ax.Low & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (ax.Low & 0x80) != 0);
        }

        private byte Xor8(byte a, byte b)
        {
            int result = a ^ b;

            // CF, AF, OF = cleared; PF, ZF, SF = according to result
            ClearFlags(Flags.Carry);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            ClearFlags(Flags.Overflow);

            return (byte)result;
        }

        private ushort Xor16(ushort a, ushort b)
        {
            int result = a ^ b;

            // CF, AF, OF = cleared; PF, ZF, SF = according to result
            ClearFlags(Flags.Carry);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            ClearFlags(Flags.Overflow);

            return (ushort)result;
        }

        private void Aaa(bool isSubtract)
        {
            byte al = ax.Low;
            byte ah = ax.High;

            ClearFlags(Flags.Overflow);
            SetFlags(Flags.Parity);

            if (((al & 0x0F) > 0x09) || IsFlagSet(Flags.Auxiliary))
            {
                al = (byte)(al + (isSubtract ? -0x06 : 0x06));
                ah = (byte)(ah + (isSubtract ? -0x01 : 0x01));

                SetFlags(Flags.Auxiliary);
            }
            else
                ClearFlags(Flags.Auxiliary);

            SetClearFlagConditional(Flags.Zero, IsFlagSet(Flags.Auxiliary));
            SetClearFlagConditional(Flags.Carry, IsFlagSet(Flags.Auxiliary));
            SetClearFlagConditional(Flags.Sign, !IsFlagSet(Flags.Auxiliary));

            al &= 0x0F;

            ax.Low = al;
            ax.High = ah;
        }

        private byte Inc8(byte a)
        {
            int result = a + 1;

            // PF, AF, ZF, SF, OF = according to result, CF = undefined
            //Carry
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ 1) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Inc16(ushort a)
        {
            int result = a + 1;

            // PF, AF, ZF, SF, OF = according to result, CF = undefined
            //Carry
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ 1) & 0x8000) != 0);

            return (ushort)result;
        }

        private byte Dec8(byte a)
        {
            int result = a - 1;

            // PF, AF, ZF, SF, OF = according to result, CF = undefined
            //Carry
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ 1) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Dec16(ushort a)
        {
            int result = a - 1;

            // PF, AF, ZF, SF, OF = according to result, CF = undefined
            //Carry
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ 1) & 0x8000) != 0);

            return (ushort)result;
        }

        private byte Rol8(bool withCarry, byte a, byte b)
        {
            int result = a;

            for (var n = 0; n < (b & 0x1F); n++)
            {
                var carry = result & 0x80;

                if (withCarry)
                    result = (result << 1) | (IsFlagSet(Flags.Carry) ? 0x01 : 0);
                else
                    result = (result << 1) | (carry >> 7);

                SetClearFlagConditional(Flags.Carry, carry != 0);
            }

            SetClearFlagConditional(Flags.Overflow, ((result ^ (IsFlagSet(Flags.Carry) ? 0x80 : 0)) & 0x80) != 0);
            result &= 0xFF;

            return (byte)result;
        }

        private ushort Rol16(bool withCarry, ushort a, ushort b)
        {
            int result = a;

            for (var n = 0; n < (b & 0x1F); n++)
            {
                var carry = result & 0x8000;

                if (withCarry)
                    result = (result << 1) | (IsFlagSet(Flags.Carry) ? 0x01 : 0);
                else
                    result = (result << 1) | (carry >> 15);

                SetClearFlagConditional(Flags.Carry, carry != 0);
            }

            SetClearFlagConditional(Flags.Overflow, ((result ^ (IsFlagSet(Flags.Carry) ? 0x8000 : 0)) & 0x8000) != 0);
            result &= 0xFFFF;

            return (ushort)result;
        }

        private byte Ror8(bool withCarry, byte a, byte b)
        {
            int result = a;

            for (var n = 0; n < (b & 0x1F); n++)
            {
                var carry = result & 0x01;

                if (withCarry)
                    result = (IsFlagSet(Flags.Carry) ? 0x80 : 0) | (result >> 1);
                else
                    result = (carry << 7) | (result >> 1);

                SetClearFlagConditional(Flags.Carry, carry != 0);
            }

            SetClearFlagConditional(Flags.Overflow, ((result ^ (result << 1)) & 0x80) != 0);
            result &= 0xFF;

            return (byte)result;
        }

        private ushort Ror16(bool withCarry, ushort a, ushort b)
        {
            int result = a;

            for (var n = 0; n < (b & 0x1F); n++)
            {
                var carry = result & 0x0001;

                if (withCarry)
                    result = (IsFlagSet(Flags.Carry) ? 0x8000 : 0) | (result >> 1);
                else
                    result = (carry << 15) | (result >> 1);

                SetClearFlagConditional(Flags.Carry, carry != 0);
            }

            SetClearFlagConditional(Flags.Overflow, ((result ^ (result << 1)) & 0x8000) != 0);
            result &= 0xFFFF;

            return (ushort)result;
        }

        private byte Shl8(byte a, byte b)
        {
            b &= 0x1F;

            int result = (a << b) & 0xFF;

            if (b != 0) SetClearFlagConditional(Flags.Carry, ((a << b) & (1 << 8)) != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ (IsFlagSet(Flags.Carry) ? 0x80 : 0)) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Shl16(ushort a, ushort b)
        {
            b &= 0x1F;

            int result = (a << b) & 0xFFFF;

            if (b != 0) SetClearFlagConditional(Flags.Carry, ((a << b) & (1 << 16)) != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFFFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ (IsFlagSet(Flags.Carry) ? 0x8000 : 0)) & 0x8000) != 0);

            return (ushort)result;
        }

        private byte Shr8(bool signed, byte a, byte b)
        {
            b &= 0x1F;

            int result;

            if (signed)
            {
                result = a;

                for (var n = 0; n < b; n++)
                {
                    SetClearFlagConditional(Flags.Carry, (result & 0x01) != 0);
                    result = (result & 0x80) | (result >> 1);
                }
                result &= 0xFF;

            }
            else
            {
                result = (a >> b) & 0xFF;

                if (b != 0) SetClearFlagConditional(Flags.Carry, ((a >> (b - 1)) & (1 << 0)) != 0);
            }

            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ (result << 1)) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Shr16(bool signed, ushort a, ushort b)
        {
            b &= 0x1F;

            int result;

            if (signed)
            {
                result = a;

                for (var n = 0; n < b; n++)
                {
                    SetClearFlagConditional(Flags.Carry, (result & 0x0001) != 0);
                    result = (result & 0x8000) | (result >> 1);
                }
                result &= 0xFFFF;

            }
            else
            {
                result = (a >> b) & 0xFFFF;

                if (b != 0) SetClearFlagConditional(Flags.Carry, ((a >> (b - 1)) & (1 << 0)) != 0);
            }

            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFFFF));
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ (result << 1)) & 0x8000) != 0);

            return (ushort)result;
        }

        private byte Neg8(byte b)
        {
            int result = -b & 0xFF;

            // CF = is operand non-zero?; PF, AF, ZF, SF, OF = according to result
            SetClearFlagConditional(Flags.Carry, b != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((0 ^ b ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ 0) & (0 ^ b) & 0x80) != 0);

            return (byte)result;
        }

        private ushort Neg16(ushort b)
        {
            int result = -b & 0xFFFF;

            // CF = is operand non-zero?; PF, AF, ZF, SF, OF = according to result
            SetClearFlagConditional(Flags.Carry, b != 0);
            SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
            SetClearFlagConditional(Flags.Auxiliary, ((0 ^ b ^ result) & 0x10) != 0);
            SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
            SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
            SetClearFlagConditional(Flags.Overflow, ((result ^ 0) & (0 ^ b) & 0x8000) != 0);

            return (ushort)result;
        }

        private ushort Mul8(bool signed, byte a, byte b)
        {
            uint result = (uint)(signed ? ((sbyte)a * (sbyte)b) : (a * b));
            uint resultTrunctated = (uint)(sbyte)result;

            // CF, OF = is upper half of result non-zero?; PF, AF, SF = cleared
            SetClearFlagConditional(Flags.Carry, signed ? result != resultTrunctated : (result >> 8) != 0);
            ClearFlags(Flags.Parity);
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, true);  // TODO: cleared on ASWAN (WS), set on SPHINX (WSC/SC)
            ClearFlags(Flags.Sign);
            SetClearFlagConditional(Flags.Overflow, signed ? result != resultTrunctated : (result >> 8) != 0);

            prevMulOverflow = IsFlagSet(Flags.Overflow);

            return (ushort)result;
        }

        private uint Mul16(bool signed, ushort a, ushort b)
        {
            uint result = (uint)(signed ? ((short)a * (short)b) : (a * b));
            uint resultTrunctated = (uint)(short)result;

            // CF, OF = is upper half of result non-zero?; PF, AF, SF = cleared
            SetClearFlagConditional(Flags.Carry, signed ? result != resultTrunctated : (result >> 16) != 0);
            ClearFlags(Flags.Parity);
            ClearFlags(Flags.Auxiliary);
            SetClearFlagConditional(Flags.Zero, true);  // TODO: cleared on ASWAN (WS), set on SPHINX (WSC/SC)
            ClearFlags(Flags.Sign);
            SetClearFlagConditional(Flags.Overflow, signed ? result != resultTrunctated : (result >> 16) != 0);

            prevMulOverflow = IsFlagSet(Flags.Overflow);

            return result;
        }

        // TODO: cleanup Div8 & Div16

        private ushort Div8(bool signed, ushort a, byte b)
        {
            // PF, AF, SF = cleared, CF, OF = overflow from last multiplication

            SetClearFlagConditional(Flags.Carry, prevMulOverflow);
            ClearFlags(Flags.Parity);
            ClearFlags(Flags.Auxiliary);
            ClearFlags(Flags.Sign);
            SetClearFlagConditional(Flags.Overflow, prevMulOverflow);

            if (signed && a == 0x8000 && b == 0)
            {
                // CPU bug?

                var result = 0x0081;

                SetFlags(Flags.Parity);
                ClearFlags(Flags.Zero);
                SetFlags(Flags.Sign);

                return (ushort)result;
            }
            else if (b == 0)
            {
                Interrupt(0);

                // ZF = undefined

                return a;
            }
            else
            {
                long quotient = signed ? ((short)a / (sbyte)b) : (a / b);
                long remainder = signed ? ((short)a % (sbyte)b) : (a % b);

                if ((!signed && quotient > 0xFF) ||
                    (signed && (quotient > 0x7F || quotient < -0x7F)))
                {
                    Interrupt(0);

                    return a;
                }

                uint result = (uint)(((remainder & 0xFF) << 8) | (quotient & 0xFF));

                if (!signed)
                {
                    // ZF = is remainder 0 and result bit 0 set?
                    SetClearFlagConditional(Flags.Zero, remainder == 0 && (result & 0x01) != 0);
                }
                else
                {
                    ClearFlags(Flags.Carry);
                    SetClearFlagConditional(Flags.Parity, CalculateParity((int)(result & 0xFF)));
                    SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
                    SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
                    ClearFlags(Flags.Overflow);
                }

                return (ushort)result;
            }
        }

        private uint Div16(bool signed, uint a, ushort b)
        {
            // CF, PF, AF, SF, OF = cleared

            ClearFlags(Flags.Carry);
            ClearFlags(Flags.Parity);
            ClearFlags(Flags.Auxiliary);
            ClearFlags(Flags.Sign);
            ClearFlags(Flags.Overflow);

            if (signed && a == 0x80000000 && b == 0)
            {
                // CPU bug?

                SetFlags(Flags.Zero);

                return 0x00008001;
            }
            else if (b == 0)
            {
                Interrupt(0);

                // ZF = undefined

                return a;
            }
            else
            {
                int quotient = signed ? ((int)a / (short)b) : (int)(a / b);
                int remainder = signed ? ((int)a % (short)b) : (int)(a % b);

                if ((!signed && quotient > 0xFFFF) ||
                    (signed && (quotient > 0x7FFF || quotient < -0x7FFF)))
                {
                    Interrupt(0);

                    return a;
                }

                ulong result = (ulong)(((remainder & 0xFFFF) << 16) | (quotient & 0xFFFF));

                // ZF = is remainder 0 and result bit 0 set?

                SetClearFlagConditional(Flags.Zero, remainder == 0 && (result & 0x0001) != 0);

                return (uint)result;
            }
        }
    }
}
