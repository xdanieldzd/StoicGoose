using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using StoicGoose.Common.OpenGL;
using StoicGoose.Common.OpenGL.Shaders;
using StoicGoose.Common.OpenGL.Uniforms;
using StoicGoose.Common.OpenGL.Vertices;
using StoicGoose.Common.Utilities;

using StoicGoose.ImGuiCommon.Windows;

using NumericsVector2 = System.Numerics.Vector2;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using Buffer = StoicGoose.Common.OpenGL.Buffer;
using ShaderProgram = StoicGoose.Common.OpenGL.Shaders.Program;

namespace StoicGoose.ImGuiCommon.Handlers
{
	/* Derived/adapted from...
	 * - https://github.com/NogginBops/ImGui.NET_OpenTK_Sample
	 * - https://github.com/mellinoe/ImGui.NET/blob/eb195f622b40d2f44cb1021f304aac47de21eb1b/src/ImGui.NET.SampleProgram/SampleWindow.cs
	 */

	public sealed class ImGuiHandler : IDisposable
	{
		readonly static string[] vertexShaderSource =
		{
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
			"in vec2 texCoord;",
			"in vec4 color;",
			"out vec4 fragColor;",
			"uniform sampler2D fontTexture;",
			"void main() {",
			"fragColor = color * texture(fontTexture, texCoord);",
			"}"
		};

		readonly GameWindow gameWindow = default;
		readonly List<char> pressedChars = new();

		readonly IntPtr imguiContext = default;

		readonly State renderState = new();

		readonly Matrix4Uniform projectionMatrix = new(nameof(projectionMatrix));

		readonly Buffer vertexBuffer = default;
		readonly Buffer indexBuffer = default;
		readonly VertexArray vertexArray = default;

		readonly ShaderProgram shaderProgram = default;

		readonly List<(WindowBase window, Func<object> getUserDataFunc)> windowList = new();

		Texture fontTexture = default;

		bool wasFrameBegun = false;

		Vector2 lastMouseWheelOffset = default;

		public List<WindowBase> OpenWindows => windowList.Where(x => x.window.IsWindowOpen).Select(x => x.window).ToList();

		public ImGuiHandler(GameWindow window, Version requiredGlVersion)
		{
			gameWindow = window;
			gameWindow.TextInput += (e) => pressedChars.Add((char)e.Unicode);

			imguiContext = ImGui.CreateContext();
			ImGui.SetCurrentContext(imguiContext);
			ImGui.StyleColorsDark();

			renderState.Disable(EnableCap.CullFace);
			renderState.Disable(EnableCap.DepthTest);
			renderState.Enable(EnableCap.ScissorTest);

			vertexBuffer = Buffer.CreateVertexBuffer<ColorVertex>(BufferUsageHint.StaticDraw);
			indexBuffer = Buffer.CreateIndexBuffer<ushort>(BufferUsageHint.StaticDraw);
			vertexArray = new VertexArray(vertexBuffer, indexBuffer);

			var glslVersionString = $"#version {requiredGlVersion.Major}{requiredGlVersion.Minor}{requiredGlVersion.Build}";

			shaderProgram = new ShaderProgram(
				ShaderFactory.FromSource(ShaderType.VertexShader, glslVersionString, string.Join(Environment.NewLine, vertexShaderSource)),
				ShaderFactory.FromSource(ShaderType.FragmentShader, glslVersionString, string.Join(Environment.NewLine, fragmentShaderSource)));

			var io = ImGui.GetIO();
			io.Fonts.AddFontDefault();

			io.Fonts.GetTexDataAsRGBA32(out IntPtr fontTexturePixels, out int fontTextureWidth, out int fontTextureHeight);
			fontTexture = new Texture(fontTextureWidth, fontTextureHeight, fontTexturePixels);
			io.Fonts.SetTexID((IntPtr)fontTexture.Handle);
			io.Fonts.ClearTexData();

			io.KeyMap[(int)ImGuiKey.Space] = (int)Keys.Space;
			io.KeyMap[(int)ImGuiKey.Comma] = (int)Keys.Comma;
			io.KeyMap[(int)ImGuiKey.Minus] = (int)Keys.Minus;
			io.KeyMap[(int)ImGuiKey.Period] = (int)Keys.Period;
			io.KeyMap[(int)ImGuiKey._0] = (int)Keys.D0;
			io.KeyMap[(int)ImGuiKey._1] = (int)Keys.D1;
			io.KeyMap[(int)ImGuiKey._2] = (int)Keys.D2;
			io.KeyMap[(int)ImGuiKey._3] = (int)Keys.D3;
			io.KeyMap[(int)ImGuiKey._4] = (int)Keys.D4;
			io.KeyMap[(int)ImGuiKey._5] = (int)Keys.D5;
			io.KeyMap[(int)ImGuiKey._6] = (int)Keys.D6;
			io.KeyMap[(int)ImGuiKey._7] = (int)Keys.D7;
			io.KeyMap[(int)ImGuiKey._8] = (int)Keys.D8;
			io.KeyMap[(int)ImGuiKey._9] = (int)Keys.D9;
			io.KeyMap[(int)ImGuiKey.Semicolon] = (int)Keys.Semicolon;
			io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
			io.KeyMap[(int)ImGuiKey.B] = (int)Keys.B;
			io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
			io.KeyMap[(int)ImGuiKey.D] = (int)Keys.D;
			io.KeyMap[(int)ImGuiKey.E] = (int)Keys.E;
			io.KeyMap[(int)ImGuiKey.F] = (int)Keys.F;
			io.KeyMap[(int)ImGuiKey.G] = (int)Keys.G;
			io.KeyMap[(int)ImGuiKey.H] = (int)Keys.H;
			io.KeyMap[(int)ImGuiKey.I] = (int)Keys.I;
			io.KeyMap[(int)ImGuiKey.J] = (int)Keys.J;
			io.KeyMap[(int)ImGuiKey.K] = (int)Keys.K;
			io.KeyMap[(int)ImGuiKey.L] = (int)Keys.L;
			io.KeyMap[(int)ImGuiKey.M] = (int)Keys.M;
			io.KeyMap[(int)ImGuiKey.N] = (int)Keys.N;
			io.KeyMap[(int)ImGuiKey.O] = (int)Keys.O;
			io.KeyMap[(int)ImGuiKey.P] = (int)Keys.P;
			io.KeyMap[(int)ImGuiKey.Q] = (int)Keys.Q;
			io.KeyMap[(int)ImGuiKey.R] = (int)Keys.R;
			io.KeyMap[(int)ImGuiKey.S] = (int)Keys.S;
			io.KeyMap[(int)ImGuiKey.T] = (int)Keys.T;
			io.KeyMap[(int)ImGuiKey.U] = (int)Keys.U;
			io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
			io.KeyMap[(int)ImGuiKey.W] = (int)Keys.W;
			io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
			io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
			io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
			io.KeyMap[(int)ImGuiKey.LeftBracket] = (int)Keys.LeftBracket;
			io.KeyMap[(int)ImGuiKey.Backslash] = (int)Keys.Backslash;
			io.KeyMap[(int)ImGuiKey.RightBracket] = (int)Keys.RightBracket;
			io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
			io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
			io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
			io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
			io.KeyMap[(int)ImGuiKey.Insert] = (int)Keys.Insert;
			io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
			io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
			io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
			io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
			io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
			io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
			io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
			io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
			io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
			io.KeyMap[(int)ImGuiKey.Pause] = (int)Keys.Pause;
			io.KeyMap[(int)ImGuiKey.F1] = (int)Keys.F1;
			io.KeyMap[(int)ImGuiKey.F2] = (int)Keys.F2;
			io.KeyMap[(int)ImGuiKey.F3] = (int)Keys.F3;
			io.KeyMap[(int)ImGuiKey.F4] = (int)Keys.F4;
			io.KeyMap[(int)ImGuiKey.F5] = (int)Keys.F5;
			io.KeyMap[(int)ImGuiKey.F6] = (int)Keys.F6;
			io.KeyMap[(int)ImGuiKey.F7] = (int)Keys.F7;
			io.KeyMap[(int)ImGuiKey.F8] = (int)Keys.F8;
			io.KeyMap[(int)ImGuiKey.F9] = (int)Keys.F9;
			io.KeyMap[(int)ImGuiKey.F10] = (int)Keys.F10;
			io.KeyMap[(int)ImGuiKey.F11] = (int)Keys.F11;
			io.KeyMap[(int)ImGuiKey.F12] = (int)Keys.F12;
			io.KeyMap[(int)ImGuiKey.Keypad0] = (int)Keys.KeyPad0;
			io.KeyMap[(int)ImGuiKey.Keypad1] = (int)Keys.KeyPad1;
			io.KeyMap[(int)ImGuiKey.Keypad2] = (int)Keys.KeyPad2;
			io.KeyMap[(int)ImGuiKey.Keypad3] = (int)Keys.KeyPad3;
			io.KeyMap[(int)ImGuiKey.Keypad4] = (int)Keys.KeyPad4;
			io.KeyMap[(int)ImGuiKey.Keypad5] = (int)Keys.KeyPad5;
			io.KeyMap[(int)ImGuiKey.Keypad6] = (int)Keys.KeyPad6;
			io.KeyMap[(int)ImGuiKey.Keypad7] = (int)Keys.KeyPad7;
			io.KeyMap[(int)ImGuiKey.Keypad8] = (int)Keys.KeyPad8;
			io.KeyMap[(int)ImGuiKey.Keypad9] = (int)Keys.KeyPad9;
			io.KeyMap[(int)ImGuiKey.KeypadDecimal] = (int)Keys.KeyPadDecimal;

			io.KeyMap[(int)ImGuiKey.LeftShift] = (int)Keys.LeftShift;
			io.KeyMap[(int)ImGuiKey.RightShift] = (int)Keys.RightShift;
			io.KeyMap[(int)ImGuiKey.LeftCtrl] = (int)Keys.LeftControl;
			io.KeyMap[(int)ImGuiKey.RightCtrl] = (int)Keys.RightControl;
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
			fontTexture?.Dispose();

			GC.SuppressFinalize(this);
		}

		public void AddFontFromEmbeddedResource(string name, float size, GlyphRanges glyphRanges)
		{
			fontTexture?.Dispose();

			var io = ImGui.GetIO();

			var glyphRangePtr = glyphRanges switch
			{
				GlyphRanges.Japanese => io.Fonts.GetGlyphRangesJapanese(),
				GlyphRanges.Korean => io.Fonts.GetGlyphRangesKorean(),
				GlyphRanges.ChineseSimplifiedCommon => io.Fonts.GetGlyphRangesChineseSimplifiedCommon(),
				GlyphRanges.ChineseFull => io.Fonts.GetGlyphRangesChineseFull(),
				GlyphRanges.Cyrillic => io.Fonts.GetGlyphRangesCyrillic(),
				GlyphRanges.Thai => io.Fonts.GetGlyphRangesThai(),
				GlyphRanges.Vietnamese => io.Fonts.GetGlyphRangesVietnamese(),
				GlyphRanges.Greek => io.Fonts.GetGlyphRangesGreek(),
				_ => io.Fonts.GetGlyphRangesDefault()
			};

			var handle = GCHandle.Alloc(Resources.GetEmbeddedRawData(name), GCHandleType.Pinned);
			io.Fonts.AddFontFromMemoryTTF(handle.AddrOfPinnedObject(), (int)size, size, null, glyphRangePtr);
			handle.Free();

			io.Fonts.GetTexDataAsRGBA32(out IntPtr fontTexturePixels, out int fontTextureWidth, out int fontTextureHeight);
			fontTexture = new Texture(fontTextureWidth, fontTextureHeight, fontTexturePixels);
			io.Fonts.SetTexID((IntPtr)fontTexture.Handle);
			io.Fonts.ClearTexData();
		}

		public void Resize(int width, int height)
		{
			renderState.SetViewport(0, 0, width, height);

			var io = ImGui.GetIO();
			io.DisplaySize = new NumericsVector2(width, height);

			projectionMatrix.Value = Matrix4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
		}

		private void UpdateInputState()
		{
			if (gameWindow == null) return;

			var mousePos = gameWindow.MousePosition;
			var mouseState = gameWindow.MouseState;
			var keyboardState = gameWindow.KeyboardState;

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

		public void RegisterWindow(WindowBase window, Func<object> getUserData)
		{
			windowList.Add((window, getUserData));

			Log.WriteEvent(LogSeverity.Information, this, $"Registered {window.GetType().Name}.");
		}

		public T GetWindow<T>() where T : WindowBase
		{
			return (T)windowList.FirstOrDefault(x => x.window is T).window;
		}

		public WindowBase GetWindow(Type type)
		{
			return windowList.FirstOrDefault(x => x.window.GetType() == type).window;
		}

		public void DeregisterWindows<T>() where T : WindowBase
		{
			windowList.RemoveAll(x => x.window is T);

			Log.WriteEvent(LogSeverity.Information, this, $"Deregistered all {typeof(T).Name}.");
		}

		public void BeginFrame(float deltaTime)
		{
			if (wasFrameBegun) throw new Exception("Cannot begin new ImGui frame, last frame still in progress");

			UpdateInputState();

			var io = ImGui.GetIO();
			io.DeltaTime = deltaTime;

			ImGui.NewFrame();

			wasFrameBegun = true;
		}

		public void EndFrame()
		{
			if (!wasFrameBegun) throw new Exception("Cannot end ImGui frame, frame has not begun");

			foreach (var (window, getUserDataFunc) in windowList)
				window.Draw(getUserDataFunc());

			ImGui.Render();
			RenderDrawData(ImGui.GetDrawData());

			wasFrameBegun = false;
		}

		private void RenderDrawData(ImDrawDataPtr drawData)
		{
			if (drawData.Equals(default(ImDrawDataPtr)) || drawData.CmdListsCount == 0) return;

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
						renderState.SetScissor(new((int)clip.X, (int)drawData.DisplaySize.Y - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y)));

						if (cmdBuffer.TextureId != IntPtr.Zero)
						{
							if ((int)cmdBuffer.TextureId == fontTexture.Handle)
								fontTexture.Bind();
							else
								GL.BindTexture(TextureTarget.Texture2D, (int)cmdBuffer.TextureId);
						}

						renderState.Submit();
						vertexArray.DrawIndices(PrimitiveType.Triangles, (int)cmdBuffer.IdxOffset, (int)cmdBuffer.ElemCount);
					}
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ColorVertex : IVertexStruct
		{
			public Vector2 Position;
			public Vector2 TexCoord;
			public uint Color;
		}

		public enum GlyphRanges
		{
			Default,
			Japanese,
			Korean,
			ChineseSimplifiedCommon,
			ChineseFull,
			Cyrillic,
			Thai,
			Vietnamese,
			Greek
		}
	}
}
