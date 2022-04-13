namespace StoicGoose.Emulation.Sound
{
	public interface ISoundChannel
	{
		byte OutputLeft { get; set; }
		byte OutputRight { get; set; }

		void Reset();
		abstract void Step();

		ushort Pitch { get; set; }
		byte VolumeLeft { get; set; }
		byte VolumeRight { get; set; }
		bool Enable { get; set; }
		bool Mode { get; set; }
	}
}
