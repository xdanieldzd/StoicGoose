using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;

using ImGuiNET;

using StoicGoose.OpenGL;
using StoicGoose.OpenGL.Shaders;
using StoicGoose.OpenGL.Uniforms;
using StoicGoose.OpenGL.Vertices;

using NumericsVector2 = System.Numerics.Vector2;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using Buffer = StoicGoose.OpenGL.Buffer;
using ShaderProgram = StoicGoose.OpenGL.Shaders.Program;

namespace StoicGoose.Handlers
{
	/* Derived/adapted from...
	 * - https://github.com/NogginBops/ImGui.NET_OpenTK_Sample
	 * - https://github.com/mellinoe/ImGui.NET/blob/eb195f622b40d2f44cb1021f304aac47de21eb1b/src/ImGui.NET.SampleProgram/SampleWindow.cs
	 */

	public sealed class ImGuiHandler : IDisposable
	{
		readonly static string[] vertexShaderSource =
		{
			"#version 460",
			"layout (location = 0) in vec2 inPosition;",
			"layout (location = 1) in vec2 inTexCoord;",
			"layout (location = 2) in uint inColor;",
			"out vec2 texCoord;",
			"out vec4 color;",
			"uniform mat4 projectionMatrix;",
			"void main() {",
			"texCoord = inTexCoord;",
			"color = unpackUnorm4x8(inColor);",
			"gl_Position = projectionMatrix * vec4(inPosition, 0.0, 1.0);",
			"}"
		};

		readonly static string[] fragmentShaderSource =
		{
			"#version 460",
			"in vec2 texCoord;",
			"in vec4 color;",
			"out vec4 fragColor;",
			"uniform sampler2D fontTexture;",
			"void main() {",
			"fragColor = color * texture(fontTexture, texCoord);",
			"}"
		};

		readonly INativeInput nativeInput = default;
		readonly List<char> pressedChars = new();

		readonly IntPtr imguiContext = default;

		readonly Matrix4Uniform projectionMatrix = new(nameof(projectionMatrix));

		readonly Buffer vertexBuffer = default;
		readonly Buffer indexBuffer = default;
		readonly VertexArray vertexArray = default;

		readonly ShaderProgram shaderProgram = default;
		readonly Texture texture = default;

		bool wasFrameBegun = false;

		Vector2 lastMouseWheelOffset = default;

		public ImGuiHandler(GLControl glControl)
		{
			nativeInput = glControl.EnableNativeInput();
			nativeInput.TextInput += (e) => pressedChars.Add((char)e.Unicode);

			imguiContext = ImGui.CreateContext();
			ImGui.SetCurrentContext(imguiContext);
			ImGui.StyleColorsDark();

			vertexBuffer = Buffer.CreateVertexBuffer<ColorVertex>(BufferUsageHint.StaticDraw);
			indexBuffer = Buffer.CreateIndexBuffer<ushort>(BufferUsageHint.StaticDraw);
			vertexArray = new VertexArray(vertexBuffer, indexBuffer);

			shaderProgram = new ShaderProgram(
				ShaderFactory.FromSource(ShaderType.VertexShader, vertexShaderSource),
				ShaderFactory.FromSource(ShaderType.FragmentShader, fragmentShaderSource));

			var io = ImGui.GetIO();

			io.Fonts.AddFontDefault();
			io.Fonts.GetTexDataAsRGBA32(out IntPtr fontTexturePixels, out int fontTextureWidth, out int fontTextureHeight);
			texture = new Texture(fontTexturePixels, fontTextureWidth, fontTextureHeight);
			io.Fonts.SetTexID((IntPtr)texture.Handle);
			io.Fonts.ClearTexData();

			io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
			io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
			io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
			io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
			io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
			io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
			io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
			io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
			io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
			io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
			io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
			io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
			io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
			io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
			io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
			io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
			io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
			io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
			io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
		}

		~ImGuiHandler()
		{
			Dispose();
		}

		public void Dispose()
		{
			ImGui.DestroyContext(imguiContext);

			vertexBuffer?.Dispose();
			indexBuffer?.Dispose();
			vertexArray?.Dispose();

			shaderProgram?.Dispose();
			texture?.Dispose();

			GC.SuppressFinalize(this);
		}

		public void Resize(int width, int height)
		{
			var io = ImGui.GetIO();
			io.DisplaySize = new NumericsVector2(width, height);

			projectionMatrix.Value = Matrix4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
		}

		private void UpdateInputState()
		{
			if (nativeInput == null) return;

			var mousePos = nativeInput.MousePosition;
			var mouseState = nativeInput.MouseState;
			var keyboardState = nativeInput.KeyboardState;

			var io = ImGui.GetIO();
			io.MouseDown[0] = mouseState[MouseButton.Left];
			io.MouseDown[1] = mouseState[MouseButton.Right];
			io.MouseDown[2] = mouseState[MouseButton.Middle];
			io.MousePos = new NumericsVector2(mousePos.X, mousePos.Y);

			io.AddMouseWheelEvent(
				mouseState.Scroll.X - lastMouseWheelOffset.X,
				mouseState.Scroll.Y - lastMouseWheelOffset.Y);

			foreach (var key in Enum.GetValues(typeof(Keys)).Cast<Keys>().Where(x => x != Keys.Unknown))
				io.KeysDown[(int)key] = keyboardState.IsKeyDown(key);

			foreach (var ch in pressedChars)
				io.AddInputCharacter(ch);
			pressedChars.Clear();

			io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
			io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
			io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
			io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);

			lastMouseWheelOffset = mouseState.Scroll;
		}

		public void BeginFrame()
		{
			if (wasFrameBegun) throw new Exception("Cannot begin new ImGui frame, last frame still in progress");

			UpdateInputState();

			ImGui.NewFrame();

			wasFrameBegun = true;
		}

		public void EndFrame()
		{
			if (!wasFrameBegun) throw new Exception("Cannot end ImGui frame, frame has not begun");

			ImGui.Render();
			RenderDrawData(ImGui.GetDrawData());

			wasFrameBegun = false;
		}

		private void RenderDrawData(ImDrawDataPtr drawData)
		{
			if (drawData.Equals(default(ImDrawDataPtr)) || drawData.CmdListsCount == 0) return;

			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.DepthTest);
			GL.Enable(EnableCap.ScissorTest);

			var io = ImGui.GetIO();
			drawData.ScaleClipRects(io.DisplayFramebufferScale);

			shaderProgram.Bind();
			projectionMatrix.SubmitToProgram(shaderProgram);

			for (var i = 0; i < drawData.CmdListsCount; i++)
			{
				var commandList = drawData.CmdListsRange[i];

				vertexBuffer.Update<ColorVertex>(commandList.VtxBuffer.Data, commandList.VtxBuffer.Size);
				indexBuffer.Update<ushort>(commandList.IdxBuffer.Data, commandList.IdxBuffer.Size);

				for (var j = 0; j < commandList.CmdBuffer.Size; j++)
				{
					var cmdBuffer = commandList.CmdBuffer[j];

					if (cmdBuffer.UserCallback != IntPtr.Zero)
					{
						throw new NotImplementedException();
					}
					else
					{
						var clip = cmdBuffer.ClipRect;
						GL.Scissor((int)clip.X, (int)drawData.DisplaySize.Y - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

						if (cmdBuffer.TextureId != IntPtr.Zero)
						{
							if ((int)cmdBuffer.TextureId == texture.Handle)
								texture.Bind();
							else
								throw new NotImplementedException();
						}

						vertexArray.DrawIndices(PrimitiveType.Triangles, (int)cmdBuffer.IdxOffset, (int)cmdBuffer.ElemCount);
					}
				}
			}

			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.DepthTest);
			GL.Disable(EnableCap.ScissorTest);
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ColorVertex : IVertexStruct
		{
			public Vector2 Position;
			public Vector2 TexCoord;
			public uint Color;
		}
	}
}
