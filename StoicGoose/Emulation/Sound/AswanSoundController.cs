namespace StoicGoose.Emulation.Sound
{
	public class AswanSoundController : SoundControllerCommon
	{
		public override byte MaxMasterVolume => 2;

		/* REG_SND_9697 */
		protected ushort unknown9697;
		/* REG_SND_9899 */
		protected ushort unknown9899;

		public AswanSoundController(MemoryReadDelegate memoryRead, int rate, int outChannels) : base(memoryRead, rate, outChannels) { }

		public override void ResetRegisters()
		{
			base.ResetRegisters();

			unknown9697 = 0;
			unknown9899 = 0;
		}

		public override int[] GenerateSample()
		{
			var mixedLeft = 0;
			if (channel1.IsEnabled) mixedLeft += channel1.OutputLeft;
			if (channel2.IsEnabled) mixedLeft += channel2.OutputLeft;
			if (channel3.IsEnabled) mixedLeft += channel3.OutputLeft;
			if (channel4.IsEnabled) mixedLeft += channel4.OutputLeft;
			mixedLeft = (mixedLeft & 0x07FF) << 5;

			var mixedRight = 0;
			if (channel1.IsEnabled) mixedRight += channel1.OutputRight;
			if (channel2.IsEnabled) mixedRight += channel2.OutputRight;
			if (channel3.IsEnabled) mixedRight += channel3.OutputRight;
			if (channel4.IsEnabled) mixedRight += channel4.OutputRight;
			mixedRight = (mixedRight & 0x07FF) << 5;

			return new[] { mixedLeft, mixedRight };
		}

		public override byte ReadRegister(ushort register)
		{
			return register switch
			{
				/* REG_SND_9697 (low) */
				0x96 => (byte)((unknown9697 >> 0) & 0b11111111),
				/* REG_SND_9697 (high) */
				0x97 => (byte)((unknown9697 >> 8) & 0b00000011),
				/* REG_SND_9899 (low) */
				0x98 => (byte)((unknown9899 >> 0) & 0b11111111),
				/* REG_SND_9899 (high) */
				0x99 => (byte)((unknown9899 >> 8) & 0b00000011),
				/* REG_SND_9A */
				0x9A => 0b111,
				/* REG_SND_9B */
				0x9B => 0b11111110,
				/* REG_SND_9C */
				0x9C => 0b11111111,
				/* REG_SND_9D */
				0x9D => 0b11111111,
				/* Fall through to common */
				_ => base.ReadRegister(register)
			};
		}

		public override void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				case 0x96:
					/* REG_SND_9697 (low) */
					unknown9697 = (ushort)((unknown9697 & 0x0300) | (value << 0));
					break;

				case 0x97:
					/* REG_SND_9697 (high) */
					unknown9697 = (ushort)((unknown9697 & 0x00FF) | (value << 8));
					break;

				case 0x98:
					/* REG_SND_9899 (low) */
					unknown9899 = (ushort)((unknown9899 & 0x0300) | (value << 0));
					break;

				case 0x99:
					/* REG_SND_9899 (high) */
					unknown9899 = (ushort)((unknown9899 & 0x00FF) | (value << 8));
					break;

				case 0x9A:
				case 0x9B:
				case 0x9C:
				case 0x9D:
					/* REG_SND_9x */
					break;

				default:
					/* Fall through to common */
					base.WriteRegister(register, value);
					break;
			}
		}
	}
}
