using System;

namespace StoicGoose.WinForms
{
	public class StartOfFrameEventArgs : EventArgs
	{
		public bool ToggleMasterVolume { get; set; } = false;
	}
}
