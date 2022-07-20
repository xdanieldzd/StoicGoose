using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using StoicGoose.Common.Extensions;
using StoicGoose.Common.OpenGL;
using StoicGoose.Common.OpenGL.Shaders;
using StoicGoose.Common.OpenGL.Shaders.Bundles;
using StoicGoose.Common.OpenGL.Uniforms;
using StoicGoose.Common.OpenGL.Vertices;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;

using Buffer = StoicGoose.Common.OpenGL.Buffer;
using ShaderProgram = StoicGoose.Common.OpenGL.Shaders.Program;

namespace StoicGoose.WinForms.Handlers
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

		readonly static Dictionary<Type, Dictionary<string, (string filename, Vector2i location)>> statusIconData = new()
		{
			{
				typeof(WonderSwan),
				new()
				{
					{ "Power", ("Power.rgba", new(0, 0)) },
					{ "Initialized", ("Initialized.rgba", new(18, 0)) },
					{ "Sleep", ("Sleep.rgba", new(50, 0)) },
					{ "LowBattery", ("LowBattery.rgba", new(80, 0)) },
					{ "Volume0", ("VolumeA0.rgba", new(105, 0)) },
					{ "Volume1", ("VolumeA1.rgba", new(105, 0)) },
					{ "Volume2", ("VolumeA2.rgba", new(105, 0)) },
					{ "Headphones", ("Headphones.rgba", new(130, 0)) },
					{ "Horizontal", ("Horizontal.rgba", new(155, 0)) },
					{ "Vertical", ("Vertical.rgba", new(168, 0)) },
					{ "Aux1", ("Aux1.rgba", new(185, 0)) },
					{ "Aux2", ("Aux2.rgba", new(195, 0)) },
					{ "Aux3", ("Aux3.rgba", new(205, 0)) }
				}
			},
			{
				typeof(WonderSwanColor),
				new()
				{
					{ "Power", ("Power.rgba", new(0, 132)) },
					{ "Initialized", ("Initialized.rgba", new(0, 120)) },
					{ "Sleep", ("Sleep.rgba", new(0, 100)) },
					{ "LowBattery", ("LowBattery.rgba", new(0, 81)) },
					{ "Volume0", ("VolumeB0.rgba", new(0, 65)) },
					{ "Volume1", ("VolumeB1.rgba", new(0, 65)) },
					{ "Volume2", ("VolumeB2.rgba", new(0, 65)) },
					{ "Volume3", ("VolumeB3.rgba", new(0, 65)) },
					{ "Headphones", ("Headphones.rgba", new(0, 49)) },
					{ "Horizontal", ("Horizontal.rgba", new(0, 32)) },
					{ "Vertical", ("Vertical.rgba", new(0, 24)) },
					{ "Aux1", ("Aux1.rgba", new(0, 14)) },
					{ "Aux2", ("Aux2.rgba", new(0, 7)) },
					{ "Aux3", ("Aux3.rgba", new(0, 0)) }
				}
			}
		};

		readonly static float iconScale = 0.85f;

		readonly Type machineType = default;
		readonly Vector2i screenSize = Vector2i.Zero;
		readonly Vector2i statusIconsLocation = Vector2i.Zero;
		readonly int statusIconSize = 0;
		readonly bool statusIconsInverted = false;

		readonly State renderState = new();

		readonly Matrix4Uniform projectionMatrix = new(nameof(projectionMatrix));
		readonly Matrix4Uniform textureMatrix = new(nameof(textureMatrix));
		readonly IntUniform renderMode = new(nameof(renderMode));
		readonly IntUniform invertIcons = new(nameof(invertIcons));

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

		readonly List<string> activeStatusIcons = new();

		public bool IsVerticalOrientation { get; set; } = false;

		public List<string> AvailableShaders { get; private set; } = default;

		public Texture DisplayTexture => displayTextures[lastTextureUpdate];

		public GraphicsHandler(Type machineType, Vector2i screenSize, Vector2i statusIconsLocation, int statusIconSize, bool statusIconsInverted, string initialShaderName)
		{
			this.machineType = machineType;
			this.screenSize = screenSize;
			this.statusIconsLocation = statusIconsLocation;
			this.statusIconSize = statusIconSize;
			this.statusIconsInverted = statusIconsInverted;

			AvailableShaders = EnumerateShaders();

			SetInitialOpenGLState();

			ParseSystemIcons();

			InitializeVertexArray();
			InitializeBaseShaders();

			ChangeShader(initialShaderName);
		}

		private List<string> EnumerateShaders()
		{
			var shaderNames = new List<string>();

			if (!string.IsNullOrEmpty(Utilities.GetEmbeddedShaderFile($"{DefaultShaderName}.{DefaultManifestFilename}")))
				shaderNames.Add(DefaultShaderName);

			foreach (var file in new DirectoryInfo(Program.ShaderPath).EnumerateFiles("*.json", SearchOption.AllDirectories))
				shaderNames.Add(file.Directory.Name);

			Log.WriteEvent(LogSeverity.Information, this, $"Found {shaderNames.Count} shader(s).");

			return shaderNames;
		}

		public void SetClearColor(Color color)
		{
			renderState.SetClearColor(color);
		}

		private void SetInitialOpenGLState()
		{
			renderState.SetClearColor(Color.Black);
			renderState.Enable(EnableCap.DepthTest);
			renderState.Enable(EnableCap.Blend);
			renderState.SetBlending(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			renderState.Enable(EnableCap.CullFace);
			renderState.SetCullFace(CullFaceMode.Back);
		}

		private void ParseSystemIcons()
		{
			foreach (var (name, (filename, _)) in statusIconData[machineType])
			{
				var texture = new Texture(Utilities.GetEmbeddedSystemIcon(filename));
				texture.SetTextureFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);

				iconTextures.Add(name, texture);
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
			commonVertexShaderSource = Utilities.GetEmbeddedShaderFile("Vertex.glsl");
			commonFragmentShaderBaseSource = Utilities.GetEmbeddedShaderFile("FragmentBase.glsl");
		}

		private (BundleManifest, ShaderProgram) LoadShaderBundle(string name)
		{
			string manifestJson, fragmentSource;

			if (!string.IsNullOrEmpty(manifestJson = Utilities.GetEmbeddedShaderFile($"{name}.{DefaultManifestFilename}")))
				fragmentSource = Utilities.GetEmbeddedShaderFile($"{name}.{DefaultSourceFilename}");
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

			var glslVersionString = $"#version {Program.RequiredGLVersion.Major}{Program.RequiredGLVersion.Minor}{Program.RequiredGLVersion.Build}";

			var shaderBundle = manifestJson.DeserializeObject<BundleManifest>();
			var shaderProgram = new ShaderProgram(
				ShaderFactory.FromSource(ShaderType.VertexShader, glslVersionString, commonVertexShaderSource),
				ShaderFactory.FromSource(ShaderType.FragmentShader, glslVersionString, commonFragmentShaderBaseSource, $"const int numSamplers = {shaderBundle.Samplers};", fragmentSource));

			if (shaderBundle.Samplers > maxTextureSamplerCount)
				shaderBundle.Samplers = maxTextureSamplerCount;

			Log.WriteEvent(LogSeverity.Information, this, $"Loaded shader '{name}'.");

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
				if (!statusIconsInverted)
					displayTextures[i] = new Texture(screenSize.X, screenSize.Y, 255, 255, 255, 255);
				else
					displayTextures[i] = new Texture(screenSize.X, screenSize.Y, 8, 8, 8, 255);

				displayTextures[i].SetTextureFilter(textureMinFilter, textureMagFilter);
				displayTextures[i].SetTextureWrapMode(textureWrapMode, textureWrapMode);

				GL.Uniform1(commonShaderProgram.GetUniformLocation($"textureSamplers[{i}]"), i);
			}

			Log.WriteEvent(LogSeverity.Information, this, $"Generated {commonBundleManifest.Samplers} display texture(s).");

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

		public void UpdateScreen(byte[] framebuffer)
		{
			if (wasShaderChanged)
			{
				for (var i = 0; i < commonBundleManifest.Samplers; i++)
					displayTextures[i].Update(framebuffer);

				Log.WriteEvent(LogSeverity.Information, this, $"Shader changed successfully.");

				wasShaderChanged = false;
			}
			else
				displayTextures[lastTextureUpdate].Update(framebuffer);

			lastTextureUpdate = (lastTextureUpdate + 1) % commonBundleManifest.Samplers;
		}

		public void Resize(Rectangle clientRect)
		{
			renderState.SetViewport(clientRect.X, clientRect.Y, clientRect.Width, clientRect.Height);

			var statusIconsOnRight = statusIconsLocation.X > statusIconsLocation.Y;

			int screenWidth, screenHeight;

			if (!IsVerticalOrientation)
			{
				screenWidth = screenSize.X;
				screenHeight = screenSize.Y;
				if (statusIconsOnRight) screenWidth += statusIconSize;
				if (!statusIconsOnRight) screenHeight += statusIconSize;
			}
			else
			{
				screenWidth = screenSize.Y;
				screenHeight = screenSize.X;
				if (!statusIconsOnRight) screenWidth += statusIconSize;
				if (statusIconsOnRight) screenHeight += statusIconSize;
			}

			var aspects = new Vector2(clientRect.Width / (float)screenWidth, clientRect.Height / (float)screenHeight);
			var multiplier = (float)Math.Max(1, Math.Min(Math.Floor(aspects.X), Math.Floor(aspects.Y)));
			var adjustedWidth = screenWidth * multiplier;
			var adjustedHeight = screenHeight * multiplier;

			var adjustedX = (float)Math.Floor((clientRect.Width - adjustedWidth) / 2f);
			var adjustedY = (float)Math.Floor((clientRect.Height - adjustedHeight) / 2f);

			if ((!IsVerticalOrientation && !statusIconsOnRight) || (IsVerticalOrientation && statusIconsOnRight)) adjustedHeight -= statusIconSize * multiplier;
			else adjustedWidth -= statusIconSize * multiplier;

			if (IsVerticalOrientation && statusIconsOnRight) adjustedY += statusIconSize * multiplier;

			displayPosition = new Vector2(adjustedX, adjustedY);
			displaySize = new Vector2(adjustedWidth, adjustedHeight);

			foreach (var (icon, _) in iconTextures)
			{
				var iconLocation = statusIconsLocation + statusIconData[machineType][icon].location;

				float x, y;

				if (!IsVerticalOrientation)
				{
					x = adjustedX + (iconLocation.X * multiplier);
					y = adjustedY + (iconLocation.Y * multiplier);
				}
				else
				{
					x = adjustedX + (iconLocation.Y * multiplier);
					y = (!statusIconsOnRight ? adjustedY : 0) + ((-iconLocation.X + screenHeight - statusIconSize) * multiplier);
				}

				var iconOffset = (statusIconSize - (statusIconSize * iconScale)) / 2f * multiplier;
				x += iconOffset;
				y += iconOffset;

				iconModelviewMatrices[icon].Value =
					Matrix4.CreateScale(statusIconSize * multiplier * iconScale, statusIconSize * multiplier * iconScale, 1f) *
					Matrix4.CreateTranslation(x, y, 0f);
			}

			projectionMatrix.Value = Matrix4.CreateOrthographicOffCenter(0f, clientRect.Width, clientRect.Height, 0f, -1f, 1f);
			textureMatrix.Value = IsVerticalOrientation ?
				Matrix4.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90f)) * Matrix4.CreateTranslation(0.5f, 0.5f, 0f) :
				Matrix4.Identity;
			displayModelviewMatrix.Value = Matrix4.CreateScale(displaySize.X, displaySize.Y, 1f) * Matrix4.CreateTranslation(displayPosition.X, displayPosition.Y, 0f);

			if (!IsVerticalOrientation && !statusIconsOnRight)
			{
				iconBackgroundModelviewMatrix.Value =
					Matrix4.CreateScale(adjustedWidth, statusIconSize * multiplier, 1f) *
					Matrix4.CreateTranslation(adjustedX, adjustedY + adjustedHeight, -0.5f);
			}
			else
			{
				iconBackgroundModelviewMatrix.Value =
					Matrix4.CreateScale(statusIconSize * multiplier, adjustedHeight, 1f) *
					Matrix4.CreateTranslation(adjustedX + adjustedWidth, adjustedY, -0.5f);
			}

			outputViewport.Value = new Vector4(adjustedX, adjustedY, adjustedWidth, adjustedHeight);
			inputViewport.Value = new Vector4(0, 0, IsVerticalOrientation ? screenSize.Y : screenSize.X, IsVerticalOrientation ? screenSize.X : screenSize.Y);
		}

		public void ClearFrame()
		{
			renderState.Submit();

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
		}

		public void BindTextures()
		{
			for (var i = 0; i < commonBundleManifest.Samplers; i++)
				displayTextures[i].Bind((lastTextureUpdate + i) % commonBundleManifest.Samplers);
		}

		public void UpdateStatusIcons(List<string> icons)
		{
			activeStatusIcons.Clear();
			activeStatusIcons.AddRange(icons);
		}

		public void DrawFrame()
		{
			renderState.Submit();

			commonShaderProgram.Bind();

			renderMode.Value = (int)ShaderRenderMode.Display;
			renderMode.SubmitToProgram(commonShaderProgram);

			invertIcons.Value = statusIconsInverted ? 1 : 0;
			invertIcons.SubmitToProgram(commonShaderProgram);

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

			foreach (var icon in activeStatusIcons)
			{
				iconModelviewMatrices[icon].SubmitToProgram(commonShaderProgram);

				iconTextures[icon].Bind();
				commonVertexArray.Draw(PrimitiveType.Triangles);
			}
		}
	}
}
