using System.Collections.Generic;

using StoicGoose.Common.Attributes;
using StoicGoose.Core.Display;
using StoicGoose.Core.Serial;
using StoicGoose.Core.Sound;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Machines
{
	public partial class WonderSwan : MachineCommon
	{
		public override string Manufacturer => "Bandai";
		public override string Model => "WonderSwan";

		public override string InternalEepromDefaultUsername => "WONDERSWAN";
		public override Dictionary<ushort, byte> InternalEepromDefaultData => new()
		{
			{ 0x70, 0x19 }, // Year of birth [just for fun, here set to original WS release date; new systems probably had no date set?]
			{ 0x71, 0x99 }, // ""
			{ 0x72, 0x03 }, // Month of birth [again, WS release for fun]
			{ 0x73, 0x04 }, // Day of birth [and again]
			{ 0x74, 0x00 }, // Sex [set to ?]
			{ 0x75, 0x00 }, // Blood type [set to ?]

			{ 0x76, 0x00 }, // Last game played, publisher ID [set to presumably none]
			{ 0x77, 0x00 }, // ""
			{ 0x78, 0x00 }, // Last game played, game ID [set to presumably none]
			{ 0x79, 0x00 }, // ""
			{ 0x7A, 0x00 }, // Swan ID (see Mama Mitte) -- TODO: set to valid/random value?
			{ 0x7B, 0x00 }, // ""
			{ 0x7C, 0x00 }, // Number of different games played [set to presumably none]
			{ 0x7D, 0x00 }, // Number of times settings were changed [set to presumably none]
			{ 0x7E, 0x00 }, // Number of times powered on [set to presumably none]
			{ 0x7F, 0x00 }  // ""
		};

		public override int InternalRamSize => 16 * 1024;

		public override int InternalEepromSize => 64 * 2;
		public override int InternalEepromAddressBits => 6;

		public override int BootstrapRomAddress => 0xFF000;
		public override int BootstrapRomSize => 0x1000;

		public override void Initialize()
		{
			DisplayController = new AswanDisplayController(this);
			SoundController = new AswanSoundController(this, 44100, 2);

			base.Initialize();
		}

		public override void ResetRegisters()
		{
			isWSCOrGreater = false;

			base.ResetRegisters();
		}

		public override void RunStep()
		{
			if (RunStepCallback == null || RunStepCallback.Invoke() == false)
			{
				HandleInterrupts();

				var currentCpuClockCycles = Cpu.Step();

				var displayInterrupt = DisplayController.Step(currentCpuClockCycles);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.LineCompare)) RaiseInterrupt(4);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.VBlankTimer)) RaiseInterrupt(5);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.VBlank)) RaiseInterrupt(6);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.HBlankTimer)) RaiseInterrupt(7);

				SoundController.Step(currentCpuClockCycles);

				if (Cartridge.Step(currentCpuClockCycles)) RaiseInterrupt(2);
				else LowerInterrupt(2);

				var serialInterrupt = Serial.Step();
				if (serialInterrupt.HasFlag(SerialPort.SerialInterrupts.SerialSend)) RaiseInterrupt(0);
				if (serialInterrupt.HasFlag(SerialPort.SerialInterrupts.SerialRecieve)) RaiseInterrupt(3);

				CurrentClockCyclesInLine += currentCpuClockCycles;

				cancelFrameExecution = false;
			}
			else
				cancelFrameExecution = true;
		}

		public override byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					retVal = DisplayController.ReadPort(port);
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					retVal = SoundController.ReadPort(port);
					break;

				/* Serial port */
				case var n when n == 0xB1 || n == 0xB3:
					retVal = Serial.ReadPort(port);
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					ChangeBit(ref retVal, 0, cartEnable);
					ChangeBit(ref retVal, 1, false);
					ChangeBit(ref retVal, 2, is16BitExtBus);
					ChangeBit(ref retVal, 3, cartRom1CycleSpeed);
					ChangeBit(ref retVal, 7, builtInSelfTestOk);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					retVal = interruptBase;
					retVal |= 0b11;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					retVal = interruptEnable;
					break;

				case 0xB4:
					/* REG_INT_STATUS */
					retVal = interruptStatus;
					break;

				case 0xB5:
					/* REG_KEYPAD */
					ChangeBit(ref retVal, 4, keypadYEnable);
					ChangeBit(ref retVal, 5, keypadXEnable);
					ChangeBit(ref retVal, 6, keypadButtonEnable);

					/* Get input from UI */
					var buttonsHeld = ReceiveInput?.Invoke().buttonsHeld;
					if (buttonsHeld != null)
					{
						if (buttonsHeld.Count > 0)
							RaiseInterrupt(1);

						if (keypadYEnable)
						{
							if (buttonsHeld.Contains("Y1")) ChangeBit(ref retVal, 0, true);
							if (buttonsHeld.Contains("Y2")) ChangeBit(ref retVal, 1, true);
							if (buttonsHeld.Contains("Y3")) ChangeBit(ref retVal, 2, true);
							if (buttonsHeld.Contains("Y4")) ChangeBit(ref retVal, 3, true);
						}

						if (keypadXEnable)
						{
							if (buttonsHeld.Contains("X1")) ChangeBit(ref retVal, 0, true);
							if (buttonsHeld.Contains("X2")) ChangeBit(ref retVal, 1, true);
							if (buttonsHeld.Contains("X3")) ChangeBit(ref retVal, 2, true);
							if (buttonsHeld.Contains("X4")) ChangeBit(ref retVal, 3, true);
						}

						if (keypadButtonEnable)
						{
							if (buttonsHeld.Contains("Start")) ChangeBit(ref retVal, 1, true);
							if (buttonsHeld.Contains("A")) ChangeBit(ref retVal, 2, true);
							if (buttonsHeld.Contains("B")) ChangeBit(ref retVal, 3, true);
						}
					}
					break;

				case 0xB6:
					/* REG_INT_ACK */
					retVal = 0x00;
					break;

				case 0xB7:
					/* ??? */
					retVal = 0x00;
					break;

				case 0xBA:
				case 0xBB:
				case 0xBC:
				case 0xBD:
				case 0xBE:
					/* REG_IEEP_DATA (low) */
					/* REG_IEEP_DATA (high) */
					/* REG_IEEP_ADDR (low) */
					/* REG_IEEP_ADDR (high) */
					/* REG_IEEP_CMD (write) */
					retVal = InternalEeprom.ReadPort((ushort)(port - 0xBA));
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					retVal = Cartridge.ReadPort(port);
					break;

				/* Unmapped */
				default:
					retVal = 0x90;
					break;
			}

			if (ReadPortCallback != null)
				retVal = ReadPortCallback.Invoke(port, retVal);

			return retVal;
		}

		public override void WritePort(ushort port, byte value)
		{
			WritePortCallback?.Invoke(port, value);

			switch (port)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					DisplayController.WritePort(port, value);
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					SoundController.WritePort(port, value);
					break;

				/* Serial port */
				case var n when n == 0xB1 || n == 0xB3:
					Serial.WritePort(port, value);
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					if (!cartEnable)
						cartEnable = IsBitSet(value, 0);
					is16BitExtBus = IsBitSet(value, 2);
					cartRom1CycleSpeed = IsBitSet(value, 3);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					interruptBase = (byte)(value & 0b11111000);
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					interruptEnable = value;
					break;

				case 0xB4:
					/* REG_INT_STATUS -- read-only */
					break;

				case 0xB5:
					/* REG_KEYPAD */
					keypadYEnable = IsBitSet(value, 4);
					keypadXEnable = IsBitSet(value, 5);
					keypadButtonEnable = IsBitSet(value, 6);
					break;

				case 0xB6:
					/* REG_INT_ACK */
					interruptStatus &= (byte)~(value & (0b11110010 | ~interruptEnable));
					break;

				case 0xB7:
					/* ??? */
					break;

				case 0xBA:
				case 0xBB:
				case 0xBC:
				case 0xBD:
				case 0xBE:
					/* REG_IEEP_DATA (low) */
					/* REG_IEEP_DATA (high) */
					/* REG_IEEP_ADDR (low) */
					/* REG_IEEP_ADDR (high) */
					/* REG_IEEP_STATUS (read) */
					InternalEeprom.WritePort((ushort)(port - 0xBA), value);
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					Cartridge.WritePort(port, value);
					break;
			}
		}

		[Port("REG_INT_BASE", 0x0B0)]
		[BitDescription("Interrupt base address", 3, 7)]
		[Format("X4", 0)]
		public override byte InterruptBase => interruptBase;
	}
}
