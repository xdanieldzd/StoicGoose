using System;

namespace StoicGoose.Common.OpenGL.Vertices
{
	public sealed class VertexAttribute
	{
		public Type Type { get; internal set; } = default;
		public int Size { get; internal set; } = -1;
		public int Offset { get; internal set; } = -1;
		public string Name { get; internal set; } = string.Empty;
	}
}
