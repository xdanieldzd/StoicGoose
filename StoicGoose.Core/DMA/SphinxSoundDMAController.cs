using StoicGoose.Core.Interfaces;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.DMA
{
	public class SphinxSoundDMAController : IPortAccessComponent
	{
		readonly static int[] cycleCounts = { 768, 512, 256, 128 };

		readonly static ushort destinationPortChannel2Volume = 0x089;
		readonly static ushort destinationPortHyperVoice = 0x095;

		readonly IMachine machine = default;

		/* REG_DMA_SRC(_HI) */
		uint dmaSource;
		/* REG_DMA_LEN(_HI) */
		uint dmaLength;
		/* REG_DMA_CTRL */
		byte dmaControl;

		public bool IsActive => IsBitSet(dmaControl, 7);

		bool isDecrementMode => IsBitSet(dmaControl, 6);
		bool isDestinationHyperVoice => IsBitSet(dmaControl, 4);
		bool isLoopingMode => IsBitSet(dmaControl, 3);
		int dmaRate => dmaControl & 0b11;

		uint initialSource, initialLength;

		int cycleCount;

		public SphinxSoundDMAController(IMachine machine)
		{
			this.machine = machine;
		}

		public void Reset()
		{
			initialSource = initialLength = 0;

			cycleCount = 0;

			ResetRegisters();
		}

		private void ResetRegisters()
		{
			dmaSource = dmaLength = dmaControl = 0;
		}

		public void Shutdown()
		{
			//
		}

		public void Step(int clockCyclesInStep)
		{
			cycleCount += clockCyclesInStep;

			if (cycleCount >= cycleCounts[dmaRate])
			{
				machine.WritePort(isDestinationHyperVoice ? destinationPortHyperVoice : destinationPortChannel2Volume, machine.ReadMemory(dmaSource));

				dmaSource += (uint)(isDecrementMode ? -1 : 1);
				dmaLength--;

				if (dmaLength == 0)
				{
					if (isLoopingMode)
					{
						dmaSource = initialSource;
						dmaLength = initialLength;
					}
					else
						ChangeBit(ref dmaControl, 7, false);
				}

				cycleCount = 0;
			}
		}

		public byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
			{
				case 0x4A:
					/* REG_SDMA_SRC (low) */
					retVal |= (byte)((dmaSource >> 0) & 0xFF);
					break;
				case 0x4B:
					/* REG_SDMA_SRC (mid) */
					retVal |= (byte)((dmaSource >> 8) & 0xFF);
					break;
				case 0x4C:
					/* REG_SDMA_SRC_HI */
					retVal |= (byte)((dmaSource >> 16) & 0x0F);
					break;

				case 0x4E:
					/* REG_SDMA_LEN (low) */
					retVal |= (byte)((dmaLength >> 0) & 0xFE);
					break;
				case 0x4F:
					/* REG_SDMA_LEN (mid) */
					retVal |= (byte)((dmaLength >> 8) & 0xFF);
					break;
				case 0x50:
					/* REG_SDMA_LEN_HI */
					retVal |= (byte)((dmaLength >> 16) & 0x0F);
					break;

				case 0x52:
					/* REG_SDMA_CTRL */
					retVal |= (byte)(dmaControl & 0b11011111);
					break;
			}

			return retVal;
		}

		public void WritePort(ushort port, byte value)
		{
			switch (port)
			{
				case 0x4A:
					/* REG_SDMA_SRC (low) */
					dmaSource &= 0xFFF00;
					dmaSource |= (uint)((value << 0) & 0x000FF);
					break;
				case 0x4B:
					/* REG_SDMA_SRC (mid) */
					dmaSource &= 0xF00FF;
					dmaSource |= (uint)((value << 8) & 0x0FF00);
					break;
				case 0x4C:
					/* REG_SDMA_SRC_HI */
					dmaSource &= 0x0FFFF;
					dmaSource |= (uint)((value << 16) & 0xF0000);
					break;

				case 0x4E:
					/* REG_SDMA_LEN (low) */
					dmaLength &= 0xFFF00;
					dmaLength |= (ushort)((value << 0) & 0x00FF);
					break;
				case 0x4F:
					/* REG_SDMA_LEN (mid) */
					dmaLength &= 0xF00FF;
					dmaLength |= (ushort)((value << 8) & 0xFF00);
					break;
				case 0x50:
					/* REG_SDMA_SRC_HI */
					dmaLength &= 0x0FFFF;
					dmaLength |= (uint)((value << 16) & 0xF0000);
					break;

				case 0x52:
					/* REG_SDMA_CTRL */
					if (!IsActive && IsBitSet(value, 7))
					{
						initialSource = dmaSource;
						initialLength = dmaLength;
					}
					dmaControl = (byte)(value & 0b11011111);
					break;
			}
		}
	}
}
