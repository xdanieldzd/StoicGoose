namespace StoicGoose.Core.Display
{
	public class DisplayTimer
	{
		public bool Enable { get; set; }
		public bool Repeating { get; set; }
		public ushort Frequency { get; set; }

		public ushort Counter { get; set; }

		public DisplayTimer()
		{
			Reset();
		}

		public void Reset()
		{
			Enable = Repeating = false;
			Frequency = Counter = 0;
		}

		public void Reload()
		{
			Counter = Frequency;
		}

		public bool Step()
		{
			if (Enable && Counter != 0)
			{
				Counter--;
				if (Counter == 0)
				{
					if (Repeating) Reload();
					return true;
				}
			}
			return false;
		}
	}
}
