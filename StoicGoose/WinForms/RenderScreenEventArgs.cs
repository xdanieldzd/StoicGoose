using System;

namespace StoicGoose.WinForms
{
	public class RenderScreenEventArgs : EventArgs
	{
		public byte[] Framebuffer { get; set; } = default;

		public RenderScreenEventArgs(byte[] buffer)
		{
			Framebuffer = buffer;
		}
	}
}
