using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

using static StoicGoose.Utilities;

namespace StoicGoose.DataStorage
{
	public class ObjectValue
	{
		readonly string value = default;

		IEnumerable<string> stringEnumerable => value.Split(',').Select(x => x.Trim());

		public string String => GetString(value);
		public bool Boolean => GetBool(value);
		public sbyte SByte => GetSByte(value);
		public short Short => GetShort(value);
		public int Integer => GetInt(value);
		public long Long => GetLong(value);
		public byte UnsignedByte => GetByte(value);
		public ushort UnsignedShort => GetUShort(value);
		public uint UnsignedInteger => GetUInt(value);
		public ulong UnsignedLong => GetULong(value);
		public float Float => GetFloat(value);
		public double Double => GetDouble(value);
		public Point Point => GetPoint(value);

		public string[] StringArray => stringEnumerable.Select(x => GetString(x)).ToArray();
		public bool[] BooleanArray => stringEnumerable.Select(x => GetBool(x)).ToArray();
		public sbyte[] SByteArray => stringEnumerable.Select(x => GetSByte(x)).ToArray();
		public short[] ShortArray => stringEnumerable.Select(x => GetShort(x)).ToArray();
		public int[] IntegerArray => stringEnumerable.Select(x => GetInt(x)).ToArray();
		public long[] LongArray => stringEnumerable.Select(x => GetLong(x)).ToArray();
		public byte[] UnsignedByteArray => stringEnumerable.Select(x => GetByte(x)).ToArray();
		public ushort[] UnsignedShortArray => stringEnumerable.Select(x => GetUShort(x)).ToArray();
		public uint[] UnsignedIntegerArray => stringEnumerable.Select(x => GetUInt(x)).ToArray();
		public ulong[] UnsignedLongArray => stringEnumerable.Select(x => GetULong(x)).ToArray();
		public float[] FloatArray => stringEnumerable.Select(x => GetFloat(x)).ToArray();
		public double[] DoubleArray => stringEnumerable.Select(x => GetDouble(x)).ToArray();
		public Point[] PointArray => stringEnumerable.Select(x => GetPoint(x)).ToArray();

		public ObjectValue(string value) => this.value = value;
		public ObjectValue(bool value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(sbyte value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(short value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(int value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(long value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(byte value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(ushort value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(uint value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(ulong value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(float value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(double value) => this.value = value.ToString(CultureInfo.InvariantCulture);
		public ObjectValue(Point value) => this.value = $"{value.X},{value.Y}";

		public static implicit operator string(ObjectValue value) => value?.value;

		public static implicit operator ObjectValue(string value) => new ObjectValue(value);
		public static implicit operator ObjectValue(bool value) => new ObjectValue(value);
		public static implicit operator ObjectValue(sbyte value) => new ObjectValue(value);
		public static implicit operator ObjectValue(short value) => new ObjectValue(value);
		public static implicit operator ObjectValue(int value) => new ObjectValue(value);
		public static implicit operator ObjectValue(long value) => new ObjectValue(value);
		public static implicit operator ObjectValue(byte value) => new ObjectValue(value);
		public static implicit operator ObjectValue(ushort value) => new ObjectValue(value);
		public static implicit operator ObjectValue(uint value) => new ObjectValue(value);
		public static implicit operator ObjectValue(ulong value) => new ObjectValue(value);
		public static implicit operator ObjectValue(float value) => new ObjectValue(value);
		public static implicit operator ObjectValue(double value) => new ObjectValue(value);
		public static implicit operator ObjectValue(Point value) => new ObjectValue(value);

		private static string GetString(string value) => value;
		private static bool GetBool(string value) => value == true.ToString();
		private static sbyte GetSByte(string value) => Convert.ToSByte(value, GetBase(value));
		private static short GetShort(string value) => Convert.ToInt16(value, GetBase(value));
		private static int GetInt(string value) => Convert.ToInt32(value, GetBase(value));
		private static long GetLong(string value) => Convert.ToInt64(value, GetBase(value));
		private static byte GetByte(string value) => Convert.ToByte(value, GetBase(value));
		private static ushort GetUShort(string value) => Convert.ToUInt16(value, GetBase(value));
		private static uint GetUInt(string value) => Convert.ToUInt32(value, GetBase(value));
		private static ulong GetULong(string value) => Convert.ToUInt64(value, GetBase(value));
		private static float GetFloat(string value) => Convert.ToSingle(value);
		private static double GetDouble(string value) => Convert.ToDouble(value);
		private static Point GetPoint(string value) => new Point(int.Parse(value.Split(',')[0]), int.Parse(value.Split(',')[1]));

		public override string ToString() => value;
	}
}
