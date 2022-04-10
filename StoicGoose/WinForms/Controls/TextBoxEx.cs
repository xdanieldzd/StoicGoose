using System;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Controls
{
	public class TextBoxEx : TextBox
	{
		protected Timer timer = new();

		public event EventHandler TimerTick;
		protected void OnTimerTick(object s, EventArgs e) { TimerTick(s, e); }

		public int TimerInterval { get => timer.Interval; set => timer.Interval = value; }
		public bool TimerEnabled { get => timer.Enabled; set => timer.Enabled = value; }

		public void StartTimer() => timer.Start();
		public void StopTimer() => timer.Stop();

		public TextBoxEx() : base()
		{
			timer.Tick += (s, e) => { OnTimerTick(this, EventArgs.Empty); };
		}
	}
}
