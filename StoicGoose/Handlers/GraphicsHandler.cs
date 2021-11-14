using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using StoicGoose.DataStorage;
using StoicGoose.OpenGL;
using StoicGoose.OpenGL.Shaders;
using StoicGoose.OpenGL.Uniforms;
using StoicGoose.OpenGL.Vertices;
using StoicGoose.WinForms;

using Buffer = StoicGoose.OpenGL.Buffer;
using ShaderProgram = StoicGoose.OpenGL.Shaders.Program;

namespace StoicGoose.Handlers
{
	public class GraphicsHandler
	{
		readonly static string[] vertexShader = new[]
		{
			"#version 460",
			"layout(location = 0) in vec2 inPosition;",
			"layout(location = 1) in vec2 inTexCoord;",
			"out vec2 texCoord;",
			"uniform mat4 projectionMatrix;",
			"uniform mat4 modelviewMatrix;",
			"void main(){",
			"texCoord = inTexCoord;",
			"gl_Position = projectionMatrix * modelviewMatrix * vec4(inPosition, 0.0, 1.0);",
			"}"
		};

		readonly static string[] fragmentShader = new[]
		{
			"#version 460",
			"in vec2 texCoord;",
			"out vec4 fragColor;",
			"uniform sampler2D textureSampler;",
			"uniform mat2 textureMatrix;",
			"void main(){",
			"fragColor = texture(textureSampler, textureMatrix * texCoord);",
			"}"
		};

		readonly static string defaultModelviewMatrixName = "modelviewMatrix";

		readonly ObjectStorage metadata = default;

		readonly Dictionary<string, Texture> iconTextures = new Dictionary<string, Texture>();

		readonly Matrix4Uniform projectionMatrix = new Matrix4Uniform(nameof(projectionMatrix));
		readonly Matrix2Uniform textureMatrix = new Matrix2Uniform(nameof(textureMatrix));

		readonly Matrix4Uniform displayModelviewMatrix = new Matrix4Uniform(defaultModelviewMatrixName);
		readonly Dictionary<string, Matrix4Uniform> iconModelviewMatrices = new Dictionary<string, Matrix4Uniform>();

		readonly Texture displayTexture = default;
		readonly VertexArray displayVertexArray = default, iconVertexArray = default;

		readonly ShaderProgram mainShaderProgram = default; // TODO: split once external shaders are introduced?

		Vector2 displayPosition = default, displaySize = default;

		public Vector2i ScreenSize => displayTexture.Size;
		public bool IsVerticalOrientation { get; set; } = false;

		public GraphicsHandler(ObjectStorage metadata)
		{
			this.metadata = metadata;

			GL.ClearColor(Color.White);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);

			foreach (var (name, data) in metadata["interface/icons"].GetStorage().Select(x => (x.Key, (x.Value as ObjectStorage).GetStorage())))
			{
				if (data.ContainsKey("resource") && data["resource"] is ObjectStorage resource)
				{
					iconTextures.Add(name, new Texture(GetEmbeddedBitmap($"Assets.Icons.{resource.Get<string>()}"), TextureMinFilter.Linear, TextureMagFilter.Linear));
					iconModelviewMatrices.Add(name, new Matrix4Uniform(defaultModelviewMatrixName));
				}
			}

			displayTexture = new Texture(metadata["machine/display/width"].Get<int>(), metadata["machine/display/height"].Get<int>());

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

			mainShaderProgram = new ShaderProgram(
				ShaderFactory.FromSource(ShaderType.VertexShader, vertexShader),
				ShaderFactory.FromSource(ShaderType.FragmentShader, fragmentShader));
		}

		private Bitmap GetEmbeddedBitmap(string name)
		{
			var assembly = Assembly.GetExecutingAssembly();
			name = $"{Application.ProductName}.{name}";
			using var stream = assembly.GetManifestResourceStream(name);
			return new Bitmap(stream);
		}

		public void RenderScreen(object sender, RenderScreenEventArgs e)
		{
			displayTexture.Update(e.Framebuffer);
		}

		public void Resize(object sender, EventArgs e)
		{
			var clientRect = (sender as Control).ClientRectangle;
			var screenIconSize = metadata["interface/icons/size"].Value.Integer;

			GL.Viewport(clientRect);

			var screenWidth = IsVerticalOrientation ? (displayTexture.Size.Y + screenIconSize) : displayTexture.Size.X;
			var screenHeight = IsVerticalOrientation ? displayTexture.Size.X : (displayTexture.Size.Y + screenIconSize);

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
			textureMatrix.Value = IsVerticalOrientation ? Matrix2.CreateRotation(MathHelper.DegreesToRadians(90f)) : Matrix2.Identity;
			displayModelviewMatrix.Value = Matrix4.CreateScale(displaySize.X, displaySize.Y, 1f) * Matrix4.CreateTranslation(displayPosition.X, displayPosition.Y, 0f);
		}

		public void Paint(object sender, EventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			mainShaderProgram.Bind();

			projectionMatrix.SubmitToProgram(mainShaderProgram);
			textureMatrix.SubmitToProgram(mainShaderProgram);
			displayModelviewMatrix.SubmitToProgram(mainShaderProgram);

			displayTexture.Bind();
			displayVertexArray.Draw(PrimitiveType.Triangles);

			var activeIcons = metadata["machine/display/icons/active"].Value?.StringArray;
			//activeIcons = iconTextures.Select(x => x.Key).Reverse().ToArray();
			if (activeIcons != null)
			{
				foreach (var icon in activeIcons)
				{
					iconModelviewMatrices[icon].SubmitToProgram(mainShaderProgram);

					iconTextures[icon].Bind();
					iconVertexArray.Draw(PrimitiveType.Triangles);
				}
			}
		}
	}
}
