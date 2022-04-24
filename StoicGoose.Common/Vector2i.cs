using System;

namespace StoicGoose.Common
{
	public class Vector2i
	{
		public int X { get; set; }
		public int Y { get; set; }

		public int this[int index]
		{
			get
			{
				if (index == 0) return X;
				else if (index == 1) return Y;
				else throw new IndexOutOfRangeException();
			}
			set
			{
				if (index == 0) X = value;
				else if (index == 1) Y = value;
				else throw new IndexOutOfRangeException();
			}
		}

		public Vector2i(int x, int y)
		{
			X = x;
			Y = y;
		}

		public Vector2i(int[] values)
		{
			if (values.Length != 2) throw new ArgumentException("Invalid amount of values", nameof(values));
			X = values[0];
			Y = values[1];
		}

		public static Vector2i operator +(Vector2i v1, Vector2i v2) => new(v1.X + v2.X, v1.Y + v2.Y);
		public static Vector2i operator -(Vector2i v1, Vector2i v2) => new(v1.X - v2.X, v1.Y - v2.Y);
		public static Vector2i operator *(Vector2i v1, Vector2i v2) => new(v1.X * v2.X, v1.Y * v2.Y);
		public static Vector2i operator /(Vector2i v1, Vector2i v2) => new(v1.X / v2.X, v1.Y / v2.Y);

		// TODO: add more operators
	}
}
