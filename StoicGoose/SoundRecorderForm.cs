using StoicGoose.IO;
using StoicGoose.WinForms;
using System;
using System.Windows.Forms;

namespace StoicGoose
{
	public partial class SoundRecorderForm : Form
	{
		WaveFileWriter waveFileWriter = default;

		readonly int sampleRate = 0, numChannels = 0;

		public bool IsRecording { get; private set; } = false;

		public SoundRecorderForm(int sampleRate, int numChannels)
		{
			InitializeComponent();

			if (GlobalVariables.EnableLocalDebugIO) ftbWaveFile.FileName = @"D:\Temp\Goose\sound.wav";

			this.sampleRate = sampleRate;
			this.numChannels = numChannels;

			btnStart.Enabled = ftbWaveFile.IsFileSelected;
			btnStop.Enabled = false;
		}

		private void SoundRecorderForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (IsRecording)
				waveFileWriter?.Save();
		}

		private void ftbWaveFile_FileSelected(object sender, EventArgs e)
		{
			btnStart.Enabled = ftbWaveFile.IsFileSelected;
		}

		private void btnStart_Click(object sender, EventArgs e)
		{
			if (waveFileWriter != null)
			{
				waveFileWriter.Dispose();
				waveFileWriter = null;
			}

			waveFileWriter = new(ftbWaveFile.FileName, sampleRate, numChannels);

			btnStart.Enabled = false;
			btnStop.Enabled = true;

			IsRecording = true;
		}

		private void btnStop_Click(object sender, EventArgs e)
		{
			if (IsRecording)
				waveFileWriter?.Save();

			btnStart.Enabled = ftbWaveFile.IsFileSelected;
			btnStop.Enabled = false;

			IsRecording = false;
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Hide();
		}

		public void EnqueueSamples(object sender, EnqueueSamplesEventArgs e)
		{
			waveFileWriter.Write(e.Samples);
		}
	}
}
