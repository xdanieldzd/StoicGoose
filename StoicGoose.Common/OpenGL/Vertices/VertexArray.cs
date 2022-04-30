using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Vertices
{
	public sealed class VertexArray : IDisposable
	{
		static readonly Dictionary<Type, VertexAttribMethodType> methodTypeIdentifier = new()
		{
			{ typeof(sbyte), VertexAttribMethodType.Integer },
			{ typeof(byte), VertexAttribMethodType.Integer },
			{ typeof(short), VertexAttribMethodType.Integer },
			{ typeof(ushort), VertexAttribMethodType.Integer },
			{ typeof(int), VertexAttribMethodType.Integer },
			{ typeof(uint), VertexAttribMethodType.Integer },
			{ typeof(float), VertexAttribMethodType.Pointer },
			{ typeof(double), VertexAttribMethodType.Pointer },
			{ typeof(Color4), VertexAttribMethodType.Pointer },
			{ typeof(Vector2), VertexAttribMethodType.Pointer },
			{ typeof(Vector3), VertexAttribMethodType.Pointer },
			{ typeof(Vector4), VertexAttribMethodType.Pointer },
			{ typeof(Vector2d), VertexAttribMethodType.Pointer },
			{ typeof(Vector3d), VertexAttribMethodType.Pointer },
			{ typeof(Vector4d), VertexAttribMethodType.Pointer },
			{ typeof(Vector2i), VertexAttribMethodType.Integer },
			{ typeof(Vector3i), VertexAttribMethodType.Integer },
			{ typeof(Vector4i), VertexAttribMethodType.Integer }
		};

		static readonly Dictionary<Type, VertexAttribPointerType> pointerTypeTranslator = new()
		{
			{ typeof(sbyte), VertexAttribPointerType.Byte },
			{ typeof(byte), VertexAttribPointerType.UnsignedByte },
			{ typeof(short), VertexAttribPointerType.Short },
			{ typeof(ushort), VertexAttribPointerType.UnsignedShort },
			{ typeof(int), VertexAttribPointerType.Int },
			{ typeof(uint), VertexAttribPointerType.UnsignedInt },
			{ typeof(float), VertexAttribPointerType.Float },
			{ typeof(double), VertexAttribPointerType.Double },
			{ typeof(Color4), VertexAttribPointerType.Float },
			{ typeof(Vector2), VertexAttribPointerType.Float },
			{ typeof(Vector3), VertexAttribPointerType.Float },
			{ typeof(Vector4), VertexAttribPointerType.Float },
			{ typeof(Vector2d), VertexAttribPointerType.Double },
			{ typeof(Vector3d), VertexAttribPointerType.Double },
			{ typeof(Vector4d), VertexAttribPointerType.Double },
			{ typeof(Vector2i), VertexAttribPointerType.Int },
			{ typeof(Vector3i), VertexAttribPointerType.Int },
			{ typeof(Vector4i), VertexAttribPointerType.Int }
		};

		static readonly Dictionary<Type, VertexAttribIntegerType> integerTypeTranslator = new()
		{
			{ typeof(sbyte), VertexAttribIntegerType.Byte },
			{ typeof(byte), VertexAttribIntegerType.UnsignedByte },
			{ typeof(short), VertexAttribIntegerType.Short },
			{ typeof(ushort), VertexAttribIntegerType.UnsignedShort },
			{ typeof(int), VertexAttribIntegerType.Int },
			{ typeof(uint), VertexAttribIntegerType.UnsignedInt },
			{ typeof(Vector2i), VertexAttribIntegerType.Int },
			{ typeof(Vector3i), VertexAttribIntegerType.Int },
			{ typeof(Vector4i), VertexAttribIntegerType.Int }
		};

		static readonly Dictionary<Type, DrawElementsType> drawElementsTypeTranslator = new()
		{
			{ typeof(byte), DrawElementsType.UnsignedByte },
			{ typeof(ushort), DrawElementsType.UnsignedShort },
			{ typeof(uint), DrawElementsType.UnsignedInt }
		};

		enum VertexAttribMethodType { Pointer, Integer }

		internal readonly Buffer vertexBuffer = default, indexBuffer = default;

		internal readonly int handle = 0;
		internal readonly VertexAttribute[] attributes = default;

		public int NumVertices => vertexBuffer.count;
		public int NumIndices => indexBuffer != null ? indexBuffer.count : 0;

		public VertexArray(Buffer vtxBuffer) : this(vtxBuffer, null) { }

		public VertexArray(Buffer vtxBuffer, Buffer idxBuffer)
		{
			vertexBuffer = vtxBuffer;
			indexBuffer = idxBuffer;

			handle = GL.GenVertexArray();
			attributes = DeconstructVertexLayout(vtxBuffer.dataType);

			GL.BindVertexArray(handle);
			vertexBuffer.Bind();

			for (var i = 0; i < attributes.Length; i++)
			{
				var attribute = attributes[i];

				if (!methodTypeIdentifier.ContainsKey(attribute.Type)) continue;

				GL.EnableVertexAttribArray(i);
				switch (methodTypeIdentifier[attribute.Type])
				{
					case VertexAttribMethodType.Pointer:
						GL.VertexAttribPointer(i, attribute.Size, GetVertexAttribPointerType(attribute.Type), false, vtxBuffer.sizeInBytes, new IntPtr(attribute.Offset));
						break;
					case VertexAttribMethodType.Integer:
						GL.VertexAttribIPointer(i, attribute.Size, GetVertexAttribIntegerType(attribute.Type), vtxBuffer.sizeInBytes, new IntPtr(attribute.Offset));
						break;
				}
			}

			indexBuffer?.Bind();

			GL.BindVertexArray(0);
		}

		~VertexArray()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (GL.IsVertexArray(handle))
				GL.DeleteVertexArray(handle);

			vertexBuffer?.Dispose();
			indexBuffer?.Dispose();

			GC.SuppressFinalize(this);
		}

		private static VertexAttribute[] DeconstructVertexLayout(Type vertexType)
		{
			var attributes = new List<VertexAttribute>();

			foreach (var field in vertexType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (!field.FieldType.IsArray)
				{
					var fieldSize = Marshal.SizeOf(field.FieldType);

					if (field.FieldType.IsValueType && !field.FieldType.IsEnum)
					{
						var structFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (structFields == null || structFields.Length < 1 || structFields.Length > 4) throw new Exception("Invalid number of fields in struct");
						fieldSize = structFields.Length;
					}

					attributes.Add(new VertexAttribute()
					{
						Type = field.FieldType,
						Size = fieldSize,
						Offset = Marshal.OffsetOf(vertexType, field.Name).ToInt32(),
						Name = field.Name
					});
				}
				else
					throw new NotImplementedException("GLSL arrays not implemented");
			}

			return attributes.ToArray();
		}

		private static VertexAttribPointerType GetVertexAttribPointerType(Type type)
		{
			if (pointerTypeTranslator.ContainsKey(type))
				return pointerTypeTranslator[type];
			else
				throw new ArgumentException("Unimplemented or unsupported vertex attribute pointer type");
		}

		private static VertexAttribIntegerType GetVertexAttribIntegerType(Type type)
		{
			if (integerTypeTranslator.ContainsKey(type))
				return integerTypeTranslator[type];
			else
				throw new ArgumentException("Unimplemented or unsupported vertex attribute integer type");
		}

		private static DrawElementsType GetDrawElementsType(Type type)
		{
			if (drawElementsTypeTranslator.ContainsKey(type))
				return drawElementsTypeTranslator[type];
			else
				throw new ArgumentException("Unsupported draw elements type");
		}

		public void Draw(PrimitiveType primitiveType)
		{
			GL.BindVertexArray(handle);

			if (indexBuffer != null)
				GL.DrawElements(primitiveType, indexBuffer.count, GetDrawElementsType(indexBuffer.dataType), 0);
			else
				GL.DrawArrays(primitiveType, 0, vertexBuffer.count);
		}

		public void DrawIndices(PrimitiveType primitiveType, int offset, int count)
		{
			if (indexBuffer == null) throw new NotImplementedException("Cannot use DrawIndices without an indexbuffer");

			GL.BindVertexArray(handle);
			GL.DrawElements(primitiveType, count, GetDrawElementsType(indexBuffer.dataType), offset * indexBuffer.sizeInBytes);
		}
	}
}
