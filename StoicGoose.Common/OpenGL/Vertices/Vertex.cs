using System.Runtime.InteropServices;

using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Vertices
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex : IVertexStruct
	{
		public Vector2 Position;
		public Vector2 TexCoord;
	}
}
