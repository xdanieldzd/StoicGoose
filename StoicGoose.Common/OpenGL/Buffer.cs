using System;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

using StoicGoose.Common.OpenGL.Vertices;

namespace StoicGoose.Common.OpenGL
{
	public sealed class Buffer : IDisposable
	{
		internal readonly Type dataType = default;
		internal readonly BufferTarget bufferTarget = 0;
		internal readonly BufferUsageHint bufferUsageHint = 0;

		internal readonly int handle = GL.GenBuffer();
		internal readonly int sizeInBytes = 0;
		internal int count = 0;

		public Buffer(Type type, BufferTarget target, BufferUsageHint usage)
		{
			dataType = type;
			bufferTarget = target;
			bufferUsageHint = usage;

			sizeInBytes = Marshal.SizeOf(dataType);
		}

		~Buffer()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (GL.IsBuffer(handle))
				GL.DeleteBuffer(handle);

			GC.SuppressFinalize(this);
		}

		public static Buffer CreateBuffer<T>(BufferTarget target, BufferUsageHint usage) where T : struct => new(typeof(T), target, usage);
		public static Buffer CreateVertexBuffer<T>(BufferUsageHint usage) where T : struct, IVertexStruct => CreateBuffer<T>(BufferTarget.ArrayBuffer, usage);
		public static Buffer CreateIndexBuffer<T>(BufferUsageHint usage) where T : struct, IConvertible => CreateBuffer<T>(BufferTarget.ElementArrayBuffer, usage);

		public void Bind()
		{
			GL.BindBuffer(bufferTarget, handle);
		}

		public void Update<T>(T[] data) where T : struct
		{
			if (dataType != typeof(T))
				throw new Exception("Type mismatch on buffer update");

			if (data != null)
			{
				Bind();

				if (data.Length == count)
					GL.BufferSubData(bufferTarget, IntPtr.Zero, new IntPtr(count * sizeInBytes), data);
				else
				{
					count = data.Length;
					GL.BufferData(bufferTarget, new IntPtr(count * sizeInBytes), data, bufferUsageHint);
				}
			}
		}

		public void Update<T>(IntPtr data, int size) where T : struct
		{
			if (dataType != typeof(T))
				throw new Exception("Type mismatch on buffer update");

			if (data != IntPtr.Zero)
			{
				Bind();

				if (size == count)
					GL.BufferSubData(bufferTarget, IntPtr.Zero, new IntPtr(count * sizeInBytes), data);
				else
				{
					count = size;
					GL.BufferData(bufferTarget, new IntPtr(count * sizeInBytes), data, bufferUsageHint);
				}
			}
		}
	}
}
