using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using StoicGoose.Emulation.Machines;
using StoicGoose.Extensions;
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
		public static string DefaultManifestFilename { get; } = "Manifest.json";
		public static string DefaultSourceFilename { get; } = "Fragment.glsl";
		public static string DefaultShaderName { get; } = "Basic";

		readonly static string defaultModelviewMatrixName = "modelviewMatrix";
		readonly static int maxTextureSamplerCount = 8;

		readonly MetadataBase metadata = default;

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
		readonly Texture iconBackgroundTexture = new(8, 8);
		readonly Dictionary<string, Texture> iconTextures = new();

		int lastTextureUpdate = 0;

		VertexArray commonVertexArray = default;

		string commonVertexShaderSource = string.Empty, commonFragmentShaderBaseSource = string.Empty;

		ShaderProgram commonShaderProgram = default;
		BundleManifest commonBundleManifest = default;
		bool wasShaderChanged = false;

		Vector2 displayPosition = default, displaySize = default;

		public bool IsVerticalOrientation { get; set; } = false;

		public List<string> AvailableShaders { get; private set; } = default;

		public Texture DisplayTexture => displayTextures[lastTextureUpdate];

		public GraphicsHandler(MetadataBase metadata)
		{
			this.metadata = metadata;

			AvailableShaders = EnumerateShaders();

			SetInitialOpenGLState();

			ParseSystemIcons();

			InitializeVertexArray();
			InitializeBaseShaders();

			ChangeShader(Program.Configuration.Video.Shader);
		}

		private List<string> EnumerateShaders()
		{
			var shaderNames = new List<string>();

			if (!string.IsNullOrEmpty(GetEmbeddedShaderBundleManifest(DefaultShaderName)))
				shaderNames.Add(DefaultShaderName);

			foreach (var file in new DirectoryInfo(Program.ShaderPath).EnumerateFiles("*.json", SearchOption.AllDirectories))
				shaderNames.Add(file.Directory.Name);

			return shaderNames;
		}

		public void SetClearColor(Color color)
		{
			GL.ClearColor(color);
		}

		private void SetInitialOpenGLState()
		{
			SetClearColor(Color.Black);

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
		}

		private void ParseSystemIcons()
		{
			foreach (var (name, (filename, _)) in metadata.StatusIcons)
			{
				iconTextures.Add(name, new Texture(GetEmbeddedSystemIcon(filename), TextureMinFilter.Linear, TextureMagFilter.Linear));
				iconModelviewMatrices.Add(name, new Matrix4Uniform(defaultModelviewMatrixName));
			}
		}

		private void InitializeVertexArray()
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

			var commonVertexBuffer = Buffer.CreateVertexBuffer<Vertex>(BufferUsageHint.StaticDraw);
			commonVertexBuffer.Update(vertices);
			commonVertexArray = new VertexArray(commonVertexBuffer);
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
				var externalManifestFile = Path.Combine(Program.ShaderPath, name, DefaultManifestFilename);
				var externalSourceFile = Path.Combine(Program.ShaderPath, name, DefaultSourceFilename);
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

			if (shaderBundle.Samplers > maxTextureSamplerCount)
				shaderBundle.Samplers = maxTextureSamplerCount;

			return (shaderBundle, shaderProgram);
		}

		private void GenerateDisplayTextures()
		{
			var textureMinFilter = TextureMinFilter.Linear;
			var textureMagFilter = TextureMagFilter.Linear;
			var textureWrapMode = TextureWrapMode.Repeat;

			switch (commonBundleManifest.Filter)
			{
				case FilterMode.Linear: textureMinFilter = TextureMinFilter.Linear; textureMagFilter = TextureMagFilter.Linear; break;
				case FilterMode.Nearest: textureMinFilter = TextureMinFilter.Nearest; textureMagFilter = TextureMagFilter.Nearest; break;
			}

			switch (commonBundleManifest.Wrap)
			{
				case WrapMode.Repeat: textureWrapMode = TextureWrapMode.Repeat; break;
				case WrapMode.Edge: textureWrapMode = TextureWrapMode.ClampToEdge; break;
				case WrapMode.Border: textureWrapMode = TextureWrapMode.ClampToBorder; break;
				case WrapMode.Mirror: textureWrapMode = TextureWrapMode.MirroredRepeat; break;
			}

			commonShaderProgram.Bind();

			for (var i = 0; i < maxTextureSamplerCount; i++)
			{
				displayTextures[i]?.Dispose();
				GL.Uniform1(commonShaderProgram.GetUniformLocation($"textureSamplers[{i}]"), 0);
			}

			for (var i = 0; i < commonBundleManifest.Samplers; i++)
			{
				displayTextures[i] = new Texture(255, 255, 255, 255, metadata.ScreenSize.X, metadata.ScreenSize.Y, textureMinFilter, textureMagFilter, textureWrapMode);
				GL.Uniform1(commonShaderProgram.GetUniformLocation($"textureSamplers[{i}]"), i);
			}

			lastTextureUpdate = 0;
		}

		public void ChangeShader(string name)
		{
			wasShaderChanged = true;

			var lastFilterMode = commonBundleManifest?.Filter;
			var lastWrapMode = commonBundleManifest?.Wrap;
			var lastNumSamplers = commonBundleManifest?.Samplers;

			(commonBundleManifest, commonShaderProgram) = LoadShaderBundle(name);

			if (lastFilterMode == null || lastFilterMode != commonBundleManifest.Filter ||
				lastWrapMode == null || lastWrapMode != commonBundleManifest.Wrap ||
				lastNumSamplers == null || lastNumSamplers != commonBundleManifest.Samplers)
			{
				GenerateDisplayTextures();
			}
		}

		public void UpdateScreen(object sender, UpdateScreenEventArgs e)
		{
			if (wasShaderChanged)
			{
				for (var i = 0; i < commonBundleManifest.Samplers; i++)
					displayTextures[i].Update(e.Framebuffer);

				wasShaderChanged = false;
			}
			else
				displayTextures[lastTextureUpdate].Update(e.Framebuffer);

			lastTextureUpdate = (lastTextureUpdate + 1) % commonBundleManifest.Samplers;
		}

		public void Resize(Rectangle clientRect)
		{
			GL.Viewport(clientRect);

			var screenWidth = IsVerticalOrientation ? (metadata.ScreenSize.Y + metadata.StatusIconSize) : metadata.ScreenSize.X;
			var screenHeight = IsVerticalOrientation ? metadata.ScreenSize.X : (metadata.ScreenSize.Y + metadata.StatusIconSize);

			var aspects = new Vector2(clientRect.Width / (float)screenWidth, clientRect.Height / (float)screenHeight);
			var multiplier = (float)Math.Max(1, Math.Min(Math.Floor(aspects.X), Math.Floor(aspects.Y)));
			var adjustedWidth = screenWidth * multiplier;
			var adjustedHeight = screenHeight * multiplier;

			var adjustedX = (float)Math.Floor((clientRect.Width - adjustedWidth) / 2f);
			var adjustedY = (float)Math.Floor((clientRect.Height - adjustedHeight) / 2f);

			if (!IsVerticalOrientation) adjustedHeight -= metadata.StatusIconSize * multiplier;
			else adjustedWidth -= metadata.StatusIconSize * multiplier;

			displayPosition = new Vector2(adjustedX, adjustedY);
			displaySize = new Vector2(adjustedWidth, adjustedHeight);

			foreach (var (icon, _) in iconTextures)
			{
				var iconLocation = metadata.StatusIcons[icon].location;

				float x, y;

				if (!IsVerticalOrientation)
				{
					x = adjustedX + (iconLocation.X * multiplier);
					y = adjustedY + (iconLocation.Y * multiplier);
				}
				else
				{
					x = adjustedX + (iconLocation.Y * multiplier);
					y = adjustedY + ((-iconLocation.X + screenHeight - metadata.StatusIconSize) * multiplier);
				}

				iconModelviewMatrices[icon].Value =
					Matrix4.CreateScale(metadata.StatusIconSize * multiplier, metadata.StatusIconSize * multiplier, 1f) *
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
					Matrix4.CreateScale(adjustedWidth, metadata.StatusIconSize * multiplier, 1f) *
					Matrix4.CreateTranslation(adjustedX, adjustedY + adjustedHeight, -0.5f);
			}
			else
			{
				iconBackgroundModelviewMatrix.Value =
					Matrix4.CreateScale(metadata.StatusIconSize * multiplier, adjustedHeight, 1f) *
					Matrix4.CreateTranslation(adjustedX + adjustedWidth, adjustedY, -0.5f);
			}

			outputViewport.Value = new Vector4(adjustedX, adjustedY, adjustedWidth, adjustedHeight);
			inputViewport.Value = new Vector4(0, 0, IsVerticalOrientation ? metadata.ScreenSize.Y : metadata.ScreenSize.X, IsVerticalOrientation ? metadata.ScreenSize.X : metadata.ScreenSize.Y);
		}

		public void ClearFrame()
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
		}

		public void BindTextures()
		{
			for (var i = 0; i < commonBundleManifest.Samplers; i++)
				displayTextures[i].Bind((lastTextureUpdate + i) % commonBundleManifest.Samplers);
		}

		public void DrawFrame()
		{
			commonShaderProgram.Bind();

			renderMode.Value = (int)ShaderRenderMode.Display;
			renderMode.SubmitToProgram(commonShaderProgram);

			projectionMatrix.SubmitToProgram(commonShaderProgram);
			textureMatrix.SubmitToProgram(commonShaderProgram);
			displayModelviewMatrix.SubmitToProgram(commonShaderProgram);

			outputViewport.SubmitToProgram(commonShaderProgram);
			inputViewport.SubmitToProgram(commonShaderProgram);

			displayBrightness.Value = Program.Configuration.Video.Brightness * 0.01f;
			displayBrightness.SubmitToProgram(commonShaderProgram);
			displayContrast.Value = Program.Configuration.Video.Contrast * 0.01f;
			displayContrast.SubmitToProgram(commonShaderProgram);
			displaySaturation.Value = Program.Configuration.Video.Saturation * 0.01f;
			displaySaturation.SubmitToProgram(commonShaderProgram);

			BindTextures();
			commonVertexArray.Draw(PrimitiveType.Triangles);

			renderMode.Value = (int)ShaderRenderMode.Icons;
			renderMode.SubmitToProgram(commonShaderProgram);

			iconBackgroundModelviewMatrix.SubmitToProgram(commonShaderProgram);
			iconBackgroundTexture.Bind();
			commonVertexArray.Draw(PrimitiveType.Triangles);

			// TODO: verify the Clone() helps wrt collection-modified exceptions? (threading issue?)
			var activeIcons = metadata.IsStatusIconActive.Clone().Where(x => x.Value).Select(x => x.Key);
			if (activeIcons != null)
			{
				foreach (var icon in activeIcons)
				{
					iconModelviewMatrices[icon].SubmitToProgram(commonShaderProgram);

					iconTextures[icon].Bind();
					commonVertexArray.Draw(PrimitiveType.Triangles);
				}
			}
		}
	}
}
