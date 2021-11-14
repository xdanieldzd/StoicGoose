﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using StoicGoose.DataStorage;
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
	public class GraphicsHandler
	{
		readonly static string defaultModelviewMatrixName = "modelviewMatrix";
		readonly static int maxTextureSamplerCount = 8;

		readonly ObjectStorage metadata = default;

		readonly Dictionary<string, Texture> iconTextures = new Dictionary<string, Texture>();

		readonly Matrix4Uniform projectionMatrix = new Matrix4Uniform(nameof(projectionMatrix));
		readonly Matrix4Uniform textureMatrix = new Matrix4Uniform(nameof(textureMatrix));

		readonly Matrix4Uniform displayModelviewMatrix = new Matrix4Uniform(defaultModelviewMatrixName);
		readonly Dictionary<string, Matrix4Uniform> iconModelviewMatrices = new Dictionary<string, Matrix4Uniform>();

		readonly Vector4Uniform outputViewport = new Vector4Uniform(nameof(outputViewport));
		readonly Vector4Uniform inputViewport = new Vector4Uniform(nameof(inputViewport));

		readonly Texture[] displayTextures = new Texture[maxTextureSamplerCount];
		int lastTextureUpdate = 0;

		VertexArray displayVertexArray = default, iconVertexArray = default;

		string commonVertexShaderSource = string.Empty, commonFragmentShaderBaseSource = string.Empty;

		ShaderProgram mainShaderProgram = default, iconShaderProgram = default;
		BundleManifest mainBundleManifest = default;

		Vector2 displayPosition = default, displaySize = default;

		public Vector2i ScreenSize { get; private set; } = Vector2i.Zero;
		public bool IsVerticalOrientation { get; set; } = false;

		public GraphicsHandler(ObjectStorage metadata)
		{
			this.metadata = metadata;

			ScreenSize = new Vector2i(metadata["machine/display/width"].Get<int>(), metadata["machine/display/height"].Get<int>());

			SetInitialOpenGLState();

			ParseSystemIcons();

			InitializeVertexArrays();
			InitializeBaseShaders();

			ChangeShader(Program.Configuration.Video.Shader);
		}

		private void SetInitialOpenGLState()
		{
			GL.ClearColor(Color.White);
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

			var iconVertexBuffer = Buffer.CreateVertexBuffer<Vertex>(BufferUsageHint.StaticDraw);
			iconVertexBuffer.Update(vertices);
			iconVertexArray = new VertexArray(iconVertexBuffer);
		}

		private void InitializeBaseShaders()
		{
			commonVertexShaderSource = GetEmbeddedShaderSource("Vertex.glsl");
			commonFragmentShaderBaseSource = GetEmbeddedShaderSource("FragmentBase.glsl");

			iconShaderProgram = new ShaderProgram(
				ShaderFactory.FromSource(ShaderType.VertexShader, commonVertexShaderSource),
				ShaderFactory.FromSource(ShaderType.FragmentShader, GetEmbeddedShaderSource("IconFragment.glsl")));
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

			for (var i = 0; i < mainBundleManifest.Samplers; i++)
				displayTextures[i] = new Texture(ScreenSize.X, ScreenSize.Y, textureMinFilter, textureMagFilter, textureWrapMode);

			mainShaderProgram.Bind();
			for (var i = 0; i < mainBundleManifest.Samplers; i++)
				GL.Uniform1(mainShaderProgram.GetUniformLocation($"textureSamplers[{i}]"), i);

			lastTextureUpdate = 0;
		}

		public void ChangeShader(string name)
		{
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

			outputViewport.Value = new Vector4(adjustedX, adjustedY, adjustedWidth, adjustedHeight);
			inputViewport.Value = new Vector4(0, 0, IsVerticalOrientation ? ScreenSize.Y : ScreenSize.X, IsVerticalOrientation ? ScreenSize.X : ScreenSize.Y);
		}

		public void Paint(object sender, EventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			mainShaderProgram.Bind();

			projectionMatrix.SubmitToProgram(mainShaderProgram);
			textureMatrix.SubmitToProgram(mainShaderProgram);
			displayModelviewMatrix.SubmitToProgram(mainShaderProgram);

			outputViewport.SubmitToProgram(mainShaderProgram);
			inputViewport.SubmitToProgram(mainShaderProgram);

			for (var i = 0; i < mainBundleManifest.Samplers; i++)
				displayTextures[i].Bind((lastTextureUpdate + i) % mainBundleManifest.Samplers);
			displayVertexArray.Draw(PrimitiveType.Triangles);

			var activeIcons = metadata["machine/display/icons/active"].Value?.StringArray;
			if (activeIcons != null)
			{
				iconShaderProgram.Bind();

				projectionMatrix.SubmitToProgram(iconShaderProgram);
				textureMatrix.SubmitToProgram(iconShaderProgram);

				foreach (var icon in activeIcons)
				{
					iconModelviewMatrices[icon].SubmitToProgram(iconShaderProgram);

					iconTextures[icon].Bind();
					iconVertexArray.Draw(PrimitiveType.Triangles);
				}
			}
		}
	}
}
