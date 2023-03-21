using System;

using StoicGoose.Common.Attributes;
using StoicGoose.Core.Interfaces;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Serial
{
	public class SerialPort : IPortAccessComponent
	{
		// https://github.com/ares-emulator/ares/tree/709afef820914e3399425bb77cbc1baf895028a1/ares/ws/serial

		[Flags]
		public enum SerialInterrupts
		{
			None = 0,
			SerialSend = 1 << 0,
			SerialRecieve = 1 << 1
		}

		int baudClock, txBitClock;

		/* REG_SER_DATA */
		protected byte serialData;
		/* REG_SER_STATUS */
		protected bool enable, baudRateSelect, txEmpty, rxOverrun, rxFull;

		public void Reset()
		{
			serialData = 0;
			enable = baudRateSelect = rxOverrun = rxFull = false;
		}

		public void Shutdown()
		{
			/* Nothing to do... */
		}

		public SerialInterrupts Step()
		{
			if (!enable) return SerialInterrupts.None;

			if (!baudRateSelect && ++baudClock < 4) return SerialInterrupts.None;
			baudClock = 0;

			// STUB
			if (!txEmpty)
			{
				if (++txBitClock == 9)
				{
					txBitClock = 0;
					txEmpty = true;
				}
			}

			var interrupt = SerialInterrupts.None;
			if (txEmpty) interrupt |= SerialInterrupts.SerialSend;
			if (rxFull) interrupt |= SerialInterrupts.SerialRecieve;
			return interrupt;
		}

		public virtual byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
			{
				case 0xB1:
					/* REG_SER_DATA */
					retVal = serialData;
					rxFull = false;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					ChangeBit(ref retVal, 7, enable);
					ChangeBit(ref retVal, 6, baudRateSelect);
					ChangeBit(ref retVal, 2, txEmpty);
					ChangeBit(ref retVal, 1, rxOverrun);
					ChangeBit(ref retVal, 0, rxFull);
					break;
			}

			return retVal;
		}

		public virtual void WritePort(ushort port, byte value)
		{
			switch (port)
			{
				case 0xB1:
					/* REG_SER_DATA */
					if (txEmpty)
					{
						serialData = value;
						txEmpty = false;
					}
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					enable = IsBitSet(value, 7);
					baudRateSelect = IsBitSet(value, 6);
					rxOverrun = !IsBitSet(value, 5);
					break;
			}
		}

		[Port("REG_SER_DATA", 0x0B1)]
		[BitDescription("Serial data")]
		[Format("X2")]
		public byte SerialData => serialData;
		[Port("REG_SER_STATUS", 0x0B3)]
		[BitDescription("Serial enabled?", 7)]
		public bool Enable => enable;
		[Port("REG_SER_STATUS", 0x0B3)]
		[BitDescription("Baud rate; is 38400 baud?", 6)]
		public bool BaudRateSelect => baudRateSelect;
		[Port("REG_SER_STATUS", 0x0B3)]
		[BitDescription("TX empty?", 2)]
		public bool TxEmpty => txEmpty;
		[Port("REG_SER_STATUS", 0x0B3)]
		[BitDescription("RX overrun?", 1)]
		public bool RxOverrun => rxOverrun;
		[Port("REG_SER_STATUS", 0x0B3)]
		[BitDescription("RX full?", 0)]
		public bool RxFull => rxFull;
	}
}
