using System;
using System.Collections.Generic;

namespace StoicGoose.WinForms
{
	public class PollInputEventArgs : EventArgs
	{
		public List<string> ButtonsHeld { get; set; }
		public List<string> ButtonsPressed { get; set; }

		public PollInputEventArgs()
		{
			ButtonsHeld = new List<string>();
			ButtonsPressed = new List<string>();
		}
	}
}
