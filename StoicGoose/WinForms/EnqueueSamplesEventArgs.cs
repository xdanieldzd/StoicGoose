using System;

namespace StoicGoose.WinForms
{
	public class EnqueueSamplesEventArgs : EventArgs
	{
		public short[] Samples { get; set; } = default;

		public EnqueueSamplesEventArgs(short[] samples)
		{
			Samples = samples;
		}
	}
}
