using System;
using System.Drawing;
using System.Runtime.InteropServices;

using ImagingPixelFormat = System.Drawing.Imaging.PixelFormat;
using DrawingGraphics = System.Drawing.Graphics;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.OpenGL
{
	public sealed class Texture : IDisposable
	{
		public int Handle { get; } = GL.GenTexture();

		public Vector2i Size { get; private set; } = Vector2i.Zero;

		byte[] pixelData = default;
		bool isDirty = false;

		bool disposed = false;

		public Texture(Bitmap bitmap, TextureMinFilter textureMinFilter = TextureMinFilter.Nearest, TextureMagFilter textureMagFilter = TextureMagFilter.Nearest, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat)
		{
			FromBitmap(bitmap, textureMinFilter, textureMagFilter, textureWrapMode);
		}

		public Texture(IntPtr data, int width, int height, TextureMinFilter textureMinFilter = TextureMinFilter.Nearest, TextureMagFilter textureMagFilter = TextureMagFilter.Nearest, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat)
		{
			Size = new Vector2i(width, height);

			Generate(data, textureMinFilter, textureMagFilter, textureWrapMode);
		}

		public Texture(int width, int height, TextureMinFilter textureMinFilter = TextureMinFilter.Nearest, TextureMagFilter textureMagFilter = TextureMagFilter.Nearest, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat)
		{
			Size = new Vector2i(width, height);

			var data = new byte[width * height * 4];
			for (var i = 0; i < data.Length; i += 4)
			{
				data[i + 0] = 255;
				data[i + 1] = 255;
				data[i + 2] = 255;
				data[i + 3] = 255;
			}

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var pointer = handle.AddrOfPinnedObject();
			Generate(pointer, textureMinFilter, textureMagFilter, textureWrapMode);
			handle.Free();
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

		private void FromBitmap(Bitmap bitmap, TextureMinFilter textureMinFilter, TextureMagFilter textureMagFilter, TextureWrapMode textureWrapMode)
		{
			if (bitmap.PixelFormat != ImagingPixelFormat.Format32bppArgb)
			{
				var newBitmap = new Bitmap(bitmap.Width, bitmap.Height, ImagingPixelFormat.Format32bppArgb);
				using var g = DrawingGraphics.FromImage(newBitmap);
				g.DrawImageUnscaled(bitmap, 0, 0);
				bitmap = newBitmap;
			}

			Size = new Vector2i(bitmap.Width, bitmap.Height);

			var bmpData = bitmap.LockBits(new Rectangle(0, 0, Size.X, Size.Y), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
			Generate(bmpData.Scan0, textureMinFilter, textureMagFilter, textureWrapMode);
			bitmap.UnlockBits(bmpData);
		}

		private void Generate(IntPtr pixels, TextureMinFilter textureMinFilter, TextureMagFilter textureMagFilter, TextureWrapMode textureWrapMode)
		{
			GL.BindTexture(TextureTarget.Texture2D, Handle);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Size.X, Size.Y, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)textureWrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)textureWrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)textureMinFilter);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)textureMagFilter);

			GL.BindTexture(TextureTarget.Texture2D, 0);
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
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Size.X, Size.Y, PixelFormat.Bgra, PixelType.UnsignedByte, pixelData);
				isDirty = false;
			}
		}

		public void Update(byte[] data)
		{
			isDirty = true;
			pixelData = data;
		}
	}
}
