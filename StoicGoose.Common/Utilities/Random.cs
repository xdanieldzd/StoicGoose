using System;
namespace StoicGoose.Common.Utilities
{
public static class Random
{
readonly static System.Random rng = new((int)(DateTime.Now.Ticks & 0xFFFFFFFF));
public static Byte GetByte() => (Byte)rng.Next();
public static SByte GetSByte() => (SByte)rng.Next();
public static UInt16 GetUInt16() => (UInt16)rng.Next();
public static Int16 GetInt16() => (Int16)rng.Next();
public static UInt32 GetUInt32() => (UInt32)rng.Next();
public static Int32 GetInt32() => (Int32)rng.Next();
}
}
