using System;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using StoicGoose.Common.Drawing;

namespace StoicGoose.Common.OpenGL
{
	public sealed class Texture : IDisposable
	{
		const TextureMinFilter defaultMinFilter = TextureMinFilter.Nearest;
		const TextureMagFilter defaultMagFilter = TextureMagFilter.Nearest;
		const TextureWrapMode defaultWrapModeS = TextureWrapMode.Repeat;
		const TextureWrapMode defaultWrapModeT = TextureWrapMode.Repeat;

		public int Handle { get; } = GL.GenTexture();
		public Vector2i Size { get; private set; } = Vector2i.Zero;

		byte[] pixelData = default;
		bool isDirty = false;

		bool disposed = false;

		public Texture(int width, int height) : this(width, height, 255, 255, 255, 255) { }

		public Texture(int width, int height, byte r, byte g, byte b, byte a)
		{
			Size = new Vector2i(width, height);
			var data = new byte[width * height * 4];
			for (var i = 0; i < data.Length; i += 4)
			{
				data[i + 0] = r;
				data[i + 1] = g;
				data[i + 2] = b;
				data[i + 3] = a;
			}
			SetInitialTexImage(data);
		}

		public Texture(RgbaFile rgbaFile)
		{
			Size = new Vector2i((int)rgbaFile.Width, (int)rgbaFile.Height);
			SetInitialTexImage(rgbaFile.PixelData);
		}

		public Texture(int width, int height, byte[] data)
		{
			Size = new Vector2i(width, height);
			SetInitialTexImage(data);
		}

		public Texture(int width, int height, IntPtr data)
		{
			Size = new Vector2i(width, height);
			SetInitialTexImage(data);
		}

		~Texture()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				if (GL.IsTexture(Handle))
					GL.DeleteTexture(Handle);
			}

			disposed = true;
		}

		private void SetInitialTexImage(byte[] data)
		{
			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var pointer = handle.AddrOfPinnedObject();
			SetInitialTexImage(pointer);
			handle.Free();
		}

		private void SetInitialTexImage(IntPtr pixels)
		{
			ChangeTextureParams(() =>
			{
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)defaultMinFilter);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)defaultMagFilter);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)defaultWrapModeS);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)defaultWrapModeT);
			});
		}

		public void SetTextureFilter(TextureMinFilter textureMinFilter, TextureMagFilter textureMagFilter)
		{
			ChangeTextureParams(() =>
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)textureMinFilter);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)textureMagFilter);
			});
		}

		public void SetTextureWrapMode(TextureWrapMode textureWrapModeS, TextureWrapMode textureWrapModeT)
		{
			ChangeTextureParams(() =>
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)textureWrapModeS);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)textureWrapModeT);
			});
		}

		private void ChangeTextureParams(Action action)
		{
			var lastTextureSet = GL.GetInteger(GetPName.Texture2D);
			if (Handle != lastTextureSet) GL.BindTexture(TextureTarget.Texture2D, Handle);
			action?.Invoke();
			GL.BindTexture(TextureTarget.Texture2D, lastTextureSet);
		}

		public void Bind()
		{
			Bind(0);
		}

		public void Bind(int textureUnit)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, Handle);

			if (isDirty)
			{
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Size.X, Size.Y, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
				isDirty = false;
			}
		}

		public void Update(byte[] data)
		{
			isDirty = true;
			pixelData = data;
		}

		public void Fill(byte r, byte g, byte b, byte a)
		{
			isDirty = true;

			var data = new byte[Size.X * Size.Y * 4];
			for (var i = 0; i < data.Length; i += 4)
			{
				data[i + 0] = r;
				data[i + 1] = g;
				data[i + 2] = b;
				data[i + 3] = a;
			}
			pixelData = data;
		}
	}
}
