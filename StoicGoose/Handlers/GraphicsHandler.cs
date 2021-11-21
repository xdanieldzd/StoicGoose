using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using StoicGoose.DataStorage;
using StoicGoose.Extensions;
using StoicGoose.Emulation.Machines;
using StoicGoose.OpenGL;
using StoicGoose.OpenGL.Shaders;
using StoicGoose.OpenGL.Shaders.Bundles;
using StoicGoose.OpenGL.Uniforms;
using StoicGoose.OpenGL.Vertices;
using StoicGoose.WinForms;

using Buffer = StoicGoose.OpenGL.Buffer;
using ShaderProgram = StoicGoose.OpenGL.Shaders.Program;

using static StoicGoose.Utilities;

namespace StoicGoose.Handlers
{
	public enum ShaderRenderMode : int
	{
		Display = 0,
		Icons = 1
	}

	public class GraphicsHandler
	{
		readonly static string defaultModelviewMatrixName = "modelviewMatrix";
		readonly static int maxTextureSamplerCount = 8;

		readonly ObjectStorage metadata = default;

		readonly Dictionary<string, Texture> iconTextures = new();

		readonly Matrix4Uniform projectionMatrix = new(nameof(projectionMatrix));
		readonly Matrix4Uniform textureMatrix = new(nameof(textureMatrix));
		readonly IntUniform renderMode = new(nameof(renderMode));

		readonly Matrix4Uniform displayModelviewMatrix = new(defaultModelviewMatrixName);
		readonly Matrix4Uniform iconBackgroundModelviewMatrix = new(defaultModelviewMatrixName);
		readonly Dictionary<string, Matrix4Uniform> iconModelviewMatrices = new();

		readonly Vector4Uniform outputViewport = new(nameof(outputViewport));
		readonly Vector4Uniform inputViewport = new(nameof(inputViewport));

		readonly FloatUniform displayBrightness = new(nameof(displayBrightness), 0.0f);
		readonly FloatUniform displayContrast = new(nameof(displayContrast), 1.0f);
		readonly FloatUniform displaySaturation = new(nameof(displaySaturation), 1.0f);

		readonly Texture[] displayTextures = new Texture[maxTextureSamplerCount];
		int lastTextureUpdate = 0;

		readonly Texture iconBackgroundTexture = new(8, 8);

		VertexArray displayVertexArray = default, iconBackgroundVertexArray = default, iconVertexArray = default;

		string commonVertexShaderSource = string.Empty, commonFragmentShaderBaseSource = string.Empty;

		ShaderProgram mainShaderProgram = default;
		BundleManifest mainBundleManifest = default;
		bool wasShaderChanged = false;

		Vector2 displayPosition = default, displaySize = default;

		public Vector2i ScreenSize { get; private set; } = Vector2i.Zero;
		public bool IsVerticalOrientation { get; set; } = false;

		public GraphicsHandler(Type machineType)
		{
			metadata = (Activator.CreateInstance(machineType) as IMachine).Metadata;

			ScreenSize = new Vector2i(metadata["machine/display/width"].Get<int>(), metadata["machine/display/height"].Get<int>());

			SetInitialOpenGLState();

			ParseSystemIcons();

			InitializeVertexArrays();
			InitializeBaseShaders();

			ChangeShader(Program.Configuration.Video.Shader);
		}

		private void SetInitialOpenGLState()
		{
			GL.ClearColor(Color.Black);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
		}

		private void ParseSystemIcons()
		{
			foreach (var (name, data) in metadata["interface/icons"].GetStorage().Select(x => (x.Key, (x.Value as ObjectStorage).GetStorage())))
			{
				if (data.ContainsKey("resource") && data["resource"] is ObjectStorage resource)
				{
					iconTextures.Add(name, new Texture(GetEmbeddedSystemIcon(resource.Get<string>()), TextureMinFilter.Linear, TextureMagFilter.Linear));
					iconModelviewMatrices.Add(name, new Matrix4Uniform(defaultModelviewMatrixName));
				}
			}
		}

		private void InitializeVertexArrays()
		{
			var vertices = new Vertex[]
			{
				new Vertex() { Position = new Vector2(0f, 1f), TexCoord = new Vector2(0f, 1f) },
				new Vertex() { Position = new Vector2(1f, 0f), TexCoord = new Vector2(1f, 0f) },
				new Vertex() { Position = new Vector2(0f, 0f), TexCoord = new Vector2(0f, 0f) },

				new Vertex() { Position = new Vector2(0f, 1f), TexCoord = new Vector2(0f, 1f) },
				new Vertex() { Position = new Vector2(1f, 1f), TexCoord = new Vector2(1f, 1f) },
				new Vertex() { Position = new Vector2(1f, 0f), TexCoord = new Vector2(1f, 0f) },
			};

			var displayVertexBuffer = Buffer.CreateVertexBuffer<Vertex>(BufferUsageHint.StaticDraw);
			displayVertexBuffer.Update(vertices);
			displayVertexArray = new VertexArray(displayVertexBuffer);

			var iconBackgroundVertexBuffer = Buffer.CreateVertexBuffer<Vertex>(BufferUsageHint.StaticDraw);
			iconBackgroundVertexBuffer.Update(vertices);
			iconBackgroundVertexArray = new VertexArray(iconBackgroundVertexBuffer);

			var iconVertexBuffer = Buffer.CreateVertexBuffer<Vertex>(BufferUsageHint.StaticDraw);
			iconVertexBuffer.Update(vertices);
			iconVertexArray = new VertexArray(iconVertexBuffer);
		}

		private void InitializeBaseShaders()
		{
			commonVertexShaderSource = GetEmbeddedShaderSource("Vertex.glsl");
			commonFragmentShaderBaseSource = GetEmbeddedShaderSource("FragmentBase.glsl");
		}

		private (BundleManifest, ShaderProgram) LoadShaderBundle(string name)
		{
			string manifestJson, fragmentSource;

			if (!string.IsNullOrEmpty(manifestJson = GetEmbeddedShaderBundleManifest(name)))
				fragmentSource = GetEmbeddedShaderBundleSource(name);
			else
			{
				var externalManifestFile = Path.Combine(Program.ShaderPath, name, BundleManifest.DefaultManifestFilename);
				var externalSourceFile = Path.Combine(Program.ShaderPath, name, BundleManifest.DefaultSourceFilename);
				if (File.Exists(externalManifestFile) && File.Exists(externalSourceFile))
				{
					manifestJson = File.ReadAllText(externalManifestFile);
					fragmentSource = File.ReadAllText(externalSourceFile);
				}
				else
					throw new Exception($"Error loading shader bundle for '{name}'");
			}

			var shaderBundle = manifestJson.DeserializeObject<BundleManifest>();
			var shaderProgram = new ShaderProgram(
				ShaderFactory.FromSource(ShaderType.VertexShader, commonVertexShaderSource),
				ShaderFactory.FromSource(ShaderType.FragmentShader, commonFragmentShaderBaseSource, $"const int numSamplers = {shaderBundle.Samplers};", fragmentSource));

			return (shaderBundle, shaderProgram);
		}

		private void GenerateDisplayTextures()
		{
			var textureMinFilter = TextureMinFilter.Linear;
			var textureMagFilter = TextureMagFilter.Linear;
			var textureWrapMode = TextureWrapMode.Repeat;

			switch (mainBundleManifest.Filter)
			{
				case FilterMode.Linear: textureMinFilter = TextureMinFilter.Linear; textureMagFilter = TextureMagFilter.Linear; break;
				case FilterMode.Nearest: textureMinFilter = TextureMinFilter.Nearest; textureMagFilter = TextureMagFilter.Nearest; break;
			}

			switch (mainBundleManifest.Wrap)
			{
				case WrapMode.Repeat: textureWrapMode = TextureWrapMode.Repeat; break;
				case WrapMode.Edge: textureWrapMode = TextureWrapMode.ClampToEdge; break;
				case WrapMode.Border: textureWrapMode = TextureWrapMode.ClampToBorder; break;
				case WrapMode.Mirror: textureWrapMode = TextureWrapMode.MirroredRepeat; break;
			}

			if (mainBundleManifest.Samplers > maxTextureSamplerCount)
				mainBundleManifest.Samplers = maxTextureSamplerCount;

			for (var i = 0; i < maxTextureSamplerCount; i++)
			{
				displayTextures[i]?.Dispose();
				GL.Uniform1(mainShaderProgram.GetUniformLocation($"textureSamplers[{i}]"), 0);
			}

			mainShaderProgram.Bind();
			for (var i = 0; i < mainBundleManifest.Samplers; i++)
			{
				displayTextures[i] = new Texture(ScreenSize.X, ScreenSize.Y, textureMinFilter, textureMagFilter, textureWrapMode);
				GL.Uniform1(mainShaderProgram.GetUniformLocation($"textureSamplers[{i}]"), i);
			}

			lastTextureUpdate = 0;
		}

		public void ChangeShader(string name)
		{
			wasShaderChanged = true;

			var lastFilterMode = mainBundleManifest?.Filter;
			var lastWrapMode = mainBundleManifest?.Wrap;
			var lastNumSamplers = mainBundleManifest?.Samplers;

			(mainBundleManifest, mainShaderProgram) = LoadShaderBundle(name);

			if (lastFilterMode == null || lastFilterMode != mainBundleManifest.Filter ||
				lastWrapMode == null || lastWrapMode != mainBundleManifest.Wrap ||
				lastNumSamplers == null || lastNumSamplers != mainBundleManifest.Samplers)
			{
				GenerateDisplayTextures();
			}
		}

		public void RenderScreen(object sender, RenderScreenEventArgs e)
		{
			if (wasShaderChanged)
			{
				for (var i = 0; i < mainBundleManifest.Samplers; i++)
					displayTextures[i].Update(e.Framebuffer);

				wasShaderChanged = false;
			}
			else
				displayTextures[lastTextureUpdate].Update(e.Framebuffer);

			lastTextureUpdate = (lastTextureUpdate + 1) % mainBundleManifest.Samplers;
		}

		public void Resize(object sender, EventArgs e)
		{
			var clientRect = (sender as Control).ClientRectangle;
			var screenIconSize = metadata["interface/icons/size"].Value.Integer;

			GL.Viewport(clientRect);

			var screenWidth = IsVerticalOrientation ? (ScreenSize.Y + screenIconSize) : ScreenSize.X;
			var screenHeight = IsVerticalOrientation ? ScreenSize.X : (ScreenSize.Y + screenIconSize);

			var aspects = new Vector2(clientRect.Width / (float)screenWidth, clientRect.Height / (float)screenHeight);
			var multiplier = (float)Math.Max(1, Math.Min(Math.Floor(aspects.X), Math.Floor(aspects.Y)));
			var adjustedWidth = screenWidth * multiplier;
			var adjustedHeight = screenHeight * multiplier;

			var adjustedX = (float)Math.Floor((clientRect.Width - adjustedWidth) / 2f);
			var adjustedY = (float)Math.Floor((clientRect.Height - adjustedHeight) / 2f);

			if (!IsVerticalOrientation) adjustedHeight -= screenIconSize * multiplier;
			else adjustedWidth -= screenIconSize * multiplier;

			displayPosition = new Vector2(adjustedX, adjustedY);
			displaySize = new Vector2(adjustedWidth, adjustedHeight);

			foreach (var (icon, _) in iconTextures)
			{
				var iconLocation = metadata[$"interface/icons/{icon}/location"].Value.Point;

				float x, y;

				if (!IsVerticalOrientation)
				{
					x = adjustedX + (iconLocation.X * multiplier);
					y = adjustedY + (iconLocation.Y * multiplier);
				}
				else
				{
					x = adjustedX + (iconLocation.Y * multiplier);
					y = adjustedY + ((-iconLocation.X + screenHeight - screenIconSize) * multiplier);
				}

				iconModelviewMatrices[icon].Value =
					Matrix4.CreateScale(screenIconSize * multiplier, screenIconSize * multiplier, 1f) *
					Matrix4.CreateTranslation(x, y, 0f);
			}

			projectionMatrix.Value = Matrix4.CreateOrthographicOffCenter(0f, clientRect.Width, clientRect.Height, 0f, -1f, 1f);
			textureMatrix.Value = IsVerticalOrientation ?
				Matrix4.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90f)) * Matrix4.CreateTranslation(0.5f, 0.5f, 0f) :
				Matrix4.Identity;
			displayModelviewMatrix.Value = Matrix4.CreateScale(displaySize.X, displaySize.Y, 1f) * Matrix4.CreateTranslation(displayPosition.X, displayPosition.Y, 0f);

			if (!IsVerticalOrientation)
			{
				iconBackgroundModelviewMatrix.Value =
					Matrix4.CreateScale(adjustedWidth, screenIconSize * multiplier, 1f) *
					Matrix4.CreateTranslation(adjustedX, adjustedY + adjustedHeight, -0.5f);
			}
			else
			{
				iconBackgroundModelviewMatrix.Value =
					Matrix4.CreateScale(screenIconSize * multiplier, adjustedHeight, 1f) *
					Matrix4.CreateTranslation(adjustedX + adjustedWidth, adjustedY, -0.5f);
			}

			outputViewport.Value = new Vector4(adjustedX, adjustedY, adjustedWidth, adjustedHeight);
			inputViewport.Value = new Vector4(0, 0, IsVerticalOrientation ? ScreenSize.Y : ScreenSize.X, IsVerticalOrientation ? ScreenSize.X : ScreenSize.Y);
		}

		public void Paint(object sender, EventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			mainShaderProgram.Bind();

			renderMode.Value = (int)ShaderRenderMode.Display;
			renderMode.SubmitToProgram(mainShaderProgram);

			projectionMatrix.SubmitToProgram(mainShaderProgram);
			textureMatrix.SubmitToProgram(mainShaderProgram);
			displayModelviewMatrix.SubmitToProgram(mainShaderProgram);

			outputViewport.SubmitToProgram(mainShaderProgram);
			inputViewport.SubmitToProgram(mainShaderProgram);

			displayBrightness.Value = Program.Configuration.Video.Brightness * 0.01f;
			displayBrightness.SubmitToProgram(mainShaderProgram);
			displayContrast.Value = Program.Configuration.Video.Contrast * 0.01f;
			displayContrast.SubmitToProgram(mainShaderProgram);
			displaySaturation.Value = Program.Configuration.Video.Saturation * 0.01f;
			displaySaturation.SubmitToProgram(mainShaderProgram);

			for (var i = 0; i < mainBundleManifest.Samplers; i++)
				displayTextures[i].Bind((lastTextureUpdate + i) % mainBundleManifest.Samplers);
			displayVertexArray.Draw(PrimitiveType.Triangles);

			renderMode.Value = (int)ShaderRenderMode.Icons;
			renderMode.SubmitToProgram(mainShaderProgram);

			iconBackgroundModelviewMatrix.SubmitToProgram(mainShaderProgram);
			iconBackgroundTexture.Bind();
			iconBackgroundVertexArray.Draw(PrimitiveType.Triangles);

			var activeIcons = metadata["machine/display/icons/active"].Value?.StringArray;
			if (activeIcons != null)
			{
				foreach (var icon in activeIcons)
				{
					iconModelviewMatrices[icon].SubmitToProgram(mainShaderProgram);

					iconTextures[icon].Bind(0);
					iconVertexArray.Draw(PrimitiveType.Triangles);
				}
			}
		}
	}
}
