using System;

namespace StoicGoose.WinForms
{
	public class UpdateScreenEventArgs : EventArgs
	{
		public byte[] Framebuffer { get; set; } = default;

		public UpdateScreenEventArgs(byte[] buffer)
		{
			Framebuffer = buffer;
		}
	}
}
