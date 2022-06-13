using StoicGoose.Core.Interfaces;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.DMA
{
	public class SphinxGeneralDMAController : IPortAccessComponent
	{
		// TODO: verify behavior!

		readonly IMachine machine = default;

		/* REG_DMA_SRC(_HI) */
		uint dmaSource;
		/* REG_DMA_DST */
		ushort dmaDestination;
		/* REG_DMA_LEN */
		ushort dmaLength;
		/* REG_DMA_CTRL */
		byte dmaControl;

		public bool IsActive => IsBitSet(dmaControl, 7);

		bool isDecrementMode => IsBitSet(dmaControl, 6);

		public SphinxGeneralDMAController(IMachine machine)
		{
			this.machine = machine;
		}

		public void Reset()
		{
			//

			ResetRegisters();
		}

		private void ResetRegisters()
		{
			dmaSource = dmaDestination = dmaLength = dmaControl = 0;
		}

		public void Shutdown()
		{
			//
		}

		public int Step()
		{
			if (dmaLength == 0 || ((dmaSource >> 16) & 0x0F) == 0x01)
			{
				/* Disable DMA if length is zero OR source is SRAM */
				ChangeBit(ref dmaControl, 7, false);
				return 5;
			}
			else
			{
				if (((dmaSource >> 16) & 0x0F) != 0x01)
				{
					/* Perform DMA if source is not SRAM */
					machine.WriteMemory((uint)(dmaDestination + 0), machine.ReadMemory(dmaSource + 0));
					machine.WriteMemory((uint)(dmaDestination + 1), machine.ReadMemory(dmaSource + 1));
				}

				dmaSource += (uint)(isDecrementMode ? -2 : 2);
				dmaDestination += (ushort)(isDecrementMode ? -2 : 2);
				dmaLength -= 2;

				return 2;
			}
		}

		public byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
			{
				case 0x40:
					/* REG_DMA_SRC (low) */
					retVal |= (byte)((dmaSource >> 0) & 0xFE);
					break;
				case 0x41:
					/* REG_DMA_SRC (mid) */
					retVal |= (byte)((dmaSource >> 8) & 0xFF);
					break;
				case 0x42:
					/* REG_DMA_SRC_HI */
					retVal |= (byte)((dmaSource >> 16) & 0x0F);
					break;

				case 0x44:
					/* REG_DMA_DST (low) */
					retVal |= (byte)((dmaDestination >> 0) & 0xFE);
					break;
				case 0x45:
					/* REG_DMA_DST (high) */
					retVal |= (byte)((dmaDestination >> 8) & 0xFF);
					break;

				case 0x46:
					/* REG_DMA_LEN */
					retVal |= (byte)((dmaLength >> 0) & 0xFE);
					break;
				case 0x47:
					/* REG_DMA_LEN */
					retVal |= (byte)((dmaLength >> 8) & 0xFF);
					break;

				case 0x48:
					/* REG_DMA_CTRL */
					retVal |= (byte)(dmaControl & 0b11000000);
					break;
			}

			return retVal;
		}

		public void WritePort(ushort port, byte value)
		{
			switch (port)
			{
				case 0x40:
					/* REG_DMA_SRC (low) */
					dmaSource &= 0xFFF00;
					dmaSource |= (uint)((value << 0) & 0x000FE);
					break;
				case 0x41:
					/* REG_DMA_SRC (high) */
					dmaSource &= 0xF00FE;
					dmaSource |= (uint)((value << 8) & 0x0FF00);
					break;
				case 0x42:
					/* REG_DMA_SRC_HI */
					dmaSource &= 0x0FFFE;
					dmaSource |= (uint)((value << 16) & 0xF0000);
					break;

				case 0x44:
					/* REG_DMA_DST (low) */
					dmaDestination &= 0xFF00;
					dmaDestination |= (ushort)((value << 0) & 0x00FE);
					break;
				case 0x45:
					/* REG_DMA_DST (high) */
					dmaDestination &= 0x00FE;
					dmaDestination |= (ushort)((value << 8) & 0xFF00);
					break;

				case 0x46:
					/* REG_DMA_LEN (low) */
					dmaLength &= 0xFF00;
					dmaLength |= (ushort)((value << 0) & 0x00FE);
					break;
				case 0x47:
					/* REG_DMA_LEN (high) */
					dmaLength &= 0x00FE;
					dmaLength |= (ushort)((value << 8) & 0xFF00);
					break;

				case 0x48:
					/* REG_DMA_CTRL */
					dmaControl = (byte)(value & 0b11000000);
					break;
			}
		}
	}
}
