using StoicGoose.Common.Attributes;
using StoicGoose.Core.Display;
using StoicGoose.Core.Sound;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Machines
{
	public partial class WonderSwan : MachineCommon
	{
		public override int InternalRamSize => 16 * 1024;

		public override int InternalEepromSize => 64 * 2;
		public override int InternalEepromAddressBits => 6;

		public WonderSwan() : base() => Metadata = new WonderSwanMetadata();

		public override void Initialize()
		{
			DisplayController = new AswanDisplayController(ReadMemory);
			SoundController = new AswanSoundController(ReadMemory, 44100, 2);

			base.Initialize();
		}

		public override void ResetRegisters()
		{
			isWSCOrGreater = false;

			base.ResetRegisters();
		}

		public override void RunStep()
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

			CurrentClockCyclesInLine += currentCpuClockCycles;
		}

		public override void UpdateStatusIcons()
		{
			Metadata.IsStatusIconActive["Power"] = true;
			Metadata.IsStatusIconActive["Initialized"] = builtInSelfTestOk;

			Metadata.IsStatusIconActive["Sleep"] = DisplayController.IconSleep;
			Metadata.IsStatusIconActive["Vertical"] = DisplayController.IconVertical;
			Metadata.IsStatusIconActive["Horizontal"] = DisplayController.IconHorizontal;
			Metadata.IsStatusIconActive["Aux1"] = DisplayController.IconAux1;
			Metadata.IsStatusIconActive["Aux2"] = DisplayController.IconAux2;
			Metadata.IsStatusIconActive["Aux3"] = DisplayController.IconAux3;

			Metadata.IsStatusIconActive["Headphones"] = SoundController.HeadphonesConnected;
			Metadata.IsStatusIconActive["Volume0"] = SoundController.MasterVolume == 0;
			Metadata.IsStatusIconActive["Volume1"] = SoundController.MasterVolume == 1;
			Metadata.IsStatusIconActive["Volume2"] = SoundController.MasterVolume == 2;
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					retVal = DisplayController.ReadRegister(register);
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					retVal = SoundController.ReadRegister(register);
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

				case 0xB1:
					/* REG_SER_DATA */
					retVal = serialData;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					retVal = interruptEnable;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					//TODO: Puyo Puyo Tsuu accesses this, stub properly?
					ChangeBit(ref retVal, 7, serialEnable);
					ChangeBit(ref retVal, 6, serialBaudRateSelect);
					ChangeBit(ref retVal, 5, serialOverrunReset);
					ChangeBit(ref retVal, 2, serialSendBufferEmpty);
					ChangeBit(ref retVal, 1, serialOverrun);
					ChangeBit(ref retVal, 0, serialDataReceived);
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
					retVal = InternalEeprom.ReadRegister((ushort)(register - 0xBA));
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					retVal = Cartridge.ReadRegister(register);
					break;

				/* Unmapped */
				default:
					retVal = 0x90;
					break;
			}

			return retVal;
		}

		public override void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					DisplayController.WriteRegister(register, value);
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					SoundController.WriteRegister(register, value);
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

				case 0xB1:
					/* REG_SER_DATA */
					serialData = value;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					interruptEnable = value;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					serialEnable = IsBitSet(value, 7);
					serialBaudRateSelect = IsBitSet(value, 6);
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
					interruptStatus &= (byte)~(value & 0b11110010);
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
					InternalEeprom.WriteRegister((ushort)(register - 0xBA), value);
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					Cartridge.WriteRegister(register, value);
					break;
			}
		}

		[Register("REG_INT_BASE", 0x0B0)]
		[BitDescription("Interrupt base address", 3, 7)]
		[Format("X4", 0)]
		public override byte InterruptBase => interruptBase;
	}
}
