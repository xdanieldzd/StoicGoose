using System;

namespace StoicGoose.WinForms
{
	public class EnqueueSamplesEventArgs : EventArgs
	{
		public int NumChannels { get; set; } = 0;
		public short[] MixedSamples { get; set; } = default;

		public EnqueueSamplesEventArgs(int numChannels, short[] mixedSamples)
		{
			NumChannels = numChannels;
			MixedSamples = mixedSamples;
		}
	}
}
