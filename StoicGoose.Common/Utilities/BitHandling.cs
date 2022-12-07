using System;
namespace StoicGoose.Common.Utilities
{
public static class BitHandling
{
public static void ChangeBit(ref Byte value, int bit, bool state)
{
	if (state) value |= (Byte)(1 << bit);
	else value &= (Byte)~(1 << bit);
}
public static bool IsBitSet(Byte value, int bit) => (value & (1 << bit)) != 0;
public static void ChangeBit(ref SByte value, int bit, bool state)
{
	if (state) value |= (SByte)(1 << bit);
	else value &= (SByte)~(1 << bit);
}
public static bool IsBitSet(SByte value, int bit) => (value & (1 << bit)) != 0;
public static void ChangeBit(ref UInt16 value, int bit, bool state)
{
	if (state) value |= (UInt16)(1 << bit);
	else value &= (UInt16)~(1 << bit);
}
public static bool IsBitSet(UInt16 value, int bit) => (value & (1 << bit)) != 0;
public static void ChangeBit(ref Int16 value, int bit, bool state)
{
	if (state) value |= (Int16)(1 << bit);
	else value &= (Int16)~(1 << bit);
}
public static bool IsBitSet(Int16 value, int bit) => (value & (1 << bit)) != 0;
public static void ChangeBit(ref UInt32 value, int bit, bool state)
{
	if (state) value |= (UInt32)(1 << bit);
	else value &= (UInt32)~(1 << bit);
}
public static bool IsBitSet(UInt32 value, int bit) => (value & (1 << bit)) != 0;
public static void ChangeBit(ref Int32 value, int bit, bool state)
{
	if (state) value |= (Int32)(1 << bit);
	else value &= (Int32)~(1 << bit);
}
public static bool IsBitSet(Int32 value, int bit) => (value & (1 << bit)) != 0;
}
}
