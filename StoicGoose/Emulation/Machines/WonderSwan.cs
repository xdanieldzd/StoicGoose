using System.Linq;

using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.Sound;
using StoicGoose.Interface.Attributes;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Machines
{
	public partial class WonderSwan : MachineCommon
	{
		[ImGuiRegister(0x0B0, "REG_INT_BASE")]
		[ImGuiBitDescription("Interrupt base address", 3, 7)]
		[ImGuiFormat("X4", 0)]
		public override byte InterruptBase { get; protected set; } = 0x00;

		public override int InternalRamSize => 16 * 1024;

		public override string DefaultUsername => "WONDERSWAN";

		public override int InternalEepromSize => 64 * 2;
		public override int InternalEepromAddressBits => 6;

		public WonderSwan() : base() => Metadata = new WonderSwanMetadata();

		public override void Initialize()
		{
			DisplayController = new AswanDisplayController(ReadMemory);
			SoundController = new SoundController(ReadMemory, 44100, 2);

			base.Initialize();
		}

		public override void ResetRegisters()
		{
			IsWSCOrGreater = false;

			base.ResetRegisters();
		}

		public override void RunStep(bool isManual)
		{
			HandleBreakpoints();

			if (lastBreakpointHit == null || isManual)
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
		}

		public override void UpdateStatusIcons()
		{
			Metadata.IsStatusIconActive["power"] = true;
			Metadata.IsStatusIconActive["initialized"] = BuiltInSelfTestOk;

			Metadata.IsStatusIconActive["sleep"] = DisplayController.IconSleep;
			Metadata.IsStatusIconActive["vertical"] = DisplayController.IconVertical;
			Metadata.IsStatusIconActive["horizontal"] = DisplayController.IconHorizontal;
			Metadata.IsStatusIconActive["aux1"] = DisplayController.IconAux1;
			Metadata.IsStatusIconActive["aux2"] = DisplayController.IconAux2;
			Metadata.IsStatusIconActive["aux3"] = DisplayController.IconAux3;

			Metadata.IsStatusIconActive["headphones"] = SoundController.HeadphonesConnected;
			Metadata.IsStatusIconActive["volume0"] = SoundController.MasterVolume == 0;
			Metadata.IsStatusIconActive["volume1"] = SoundController.MasterVolume == 1;
			Metadata.IsStatusIconActive["volume2"] = SoundController.MasterVolume == 2;
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

				/* Misc system registers */
				case var n when n >= 0x60 && n < 0x80:
					// TODO?
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					retVal = SoundController.ReadRegister(register);
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					ChangeBit(ref retVal, 0, CartEnable);
					ChangeBit(ref retVal, 1, false);
					ChangeBit(ref retVal, 2, Is16BitExtBus);
					ChangeBit(ref retVal, 3, CartRom1CycleSpeed);
					ChangeBit(ref retVal, 7, BuiltInSelfTestOk);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					retVal = InterruptBase;
					retVal |= 0b11;
					break;

				case 0xB1:
					/* REG_SER_DATA */
					retVal = SerialData;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					retVal = InterruptEnable;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					//TODO: Puyo Puyo Tsuu accesses this, stub properly?
					ChangeBit(ref retVal, 7, SerialEnable);
					ChangeBit(ref retVal, 6, SerialBaudRateSelect);
					ChangeBit(ref retVal, 5, SerialOverrunReset);
					ChangeBit(ref retVal, 2, SerialSendBufferEmpty);
					ChangeBit(ref retVal, 1, SerialOverrun);
					ChangeBit(ref retVal, 0, SerialDataReceived);
					break;

				case 0xB4:
					/* REG_INT_STATUS */
					retVal = interruptStatus;
					break;

				case 0xB5:
					/* REG_KEYPAD */

					/* Get input from UI */
					var eventArgs = new PollInputEventArgs();
					OnPollInput(eventArgs);

					ChangeBit(ref retVal, 4, KeypadYEnable);
					ChangeBit(ref retVal, 5, KeypadXEnable);
					ChangeBit(ref retVal, 6, KeypadButtonEnable);

					if (eventArgs.ButtonsHeld.Count > 0)
						RaiseInterrupt(1);

					if (KeypadYEnable)
					{
						if (eventArgs.ButtonsHeld.Contains("y1")) ChangeBit(ref retVal, 0, true);
						if (eventArgs.ButtonsHeld.Contains("y2")) ChangeBit(ref retVal, 1, true);
						if (eventArgs.ButtonsHeld.Contains("y3")) ChangeBit(ref retVal, 2, true);
						if (eventArgs.ButtonsHeld.Contains("y4")) ChangeBit(ref retVal, 3, true);
					}

					if (KeypadXEnable)
					{
						if (eventArgs.ButtonsHeld.Contains("x1")) ChangeBit(ref retVal, 0, true);
						if (eventArgs.ButtonsHeld.Contains("x2")) ChangeBit(ref retVal, 1, true);
						if (eventArgs.ButtonsHeld.Contains("x3")) ChangeBit(ref retVal, 2, true);
						if (eventArgs.ButtonsHeld.Contains("x4")) ChangeBit(ref retVal, 3, true);
					}

					if (KeypadButtonEnable)
					{
						if (eventArgs.ButtonsHeld.Contains("start")) ChangeBit(ref retVal, 1, true);
						if (eventArgs.ButtonsHeld.Contains("a")) ChangeBit(ref retVal, 2, true);
						if (eventArgs.ButtonsHeld.Contains("b")) ChangeBit(ref retVal, 3, true);
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

				/* Misc system registers */
				case var n when n >= 0x60 && n < 0x80:
					// TODO?
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					SoundController.WriteRegister(register, value);
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					if (!CartEnable)
						CartEnable = IsBitSet(value, 0);
					Is16BitExtBus = IsBitSet(value, 2);
					CartRom1CycleSpeed = IsBitSet(value, 3);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					InterruptBase = (byte)(value & 0b11111000);
					break;

				case 0xB1:
					/* REG_SER_DATA */
					SerialData = value;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					InterruptEnable = value;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					SerialEnable = IsBitSet(value, 7);
					SerialBaudRateSelect = IsBitSet(value, 6);
					break;

				case 0xB4:
					/* REG_INT_STATUS -- read-only */
					break;

				case 0xB5:
					/* REG_KEYPAD */
					KeypadYEnable = IsBitSet(value, 4);
					KeypadXEnable = IsBitSet(value, 5);
					KeypadButtonEnable = IsBitSet(value, 6);
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
	}
}
