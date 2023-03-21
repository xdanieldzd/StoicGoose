using System.Collections.Generic;

using StoicGoose.Common.Attributes;
using StoicGoose.Core.Display;
using StoicGoose.Core.DMA;
using StoicGoose.Core.Serial;
using StoicGoose.Core.Sound;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Machines
{
	public partial class WonderSwanColor : MachineCommon
	{
		public override string Manufacturer => "Bandai";
		public override string Model => "WonderSwan Color";

		public override string InternalEepromDefaultUsername => "WONDERSWANCOLOR";
		public override Dictionary<ushort, byte> InternalEepromDefaultData => new()
		{
			{ 0x70, 0x20 }, // Year of birth [set to WSC release date for fun]
			{ 0x71, 0x00 }, // ""
			{ 0x72, 0x12 }, // Month of birth [again]
			{ 0x73, 0x09 }, // Day of birth [again]
			{ 0x74, 0x00 }, // Sex [?]
			{ 0x75, 0x00 }, // Blood type [?]

			{ 0x76, 0x00 }, // Last game played, publisher ID [none]
			{ 0x77, 0x00 }, // ""
			{ 0x78, 0x00 }, // Last game played, game ID [none]
			{ 0x79, 0x00 }, // ""
			{ 0x7A, 0x00 }, // Swan ID (see Mama Mitte) -- TODO: set to valid/random value?
			{ 0x7B, 0x00 }, // ""
			{ 0x7C, 0x00 }, // Number of different games played [none]
			{ 0x7D, 0x00 }, // Number of times settings were changed [none]
			{ 0x7E, 0x00 }, // Number of times powered on [none]
			{ 0x7F, 0x00 }  // ""
		};

		public override int InternalRamSize => 64 * 1024;

		public override int InternalEepromSize => 1024 * 2;
		public override int InternalEepromAddressBits => 10;

		public override int BootstrapRomAddress => 0xFE000;
		public override int BootstrapRomSize => 0x2000;

		public SphinxGeneralDMAController DmaController { get; protected set; } = default;
		public SphinxSoundDMAController SoundDmaController { get; protected set; } = default;

		public override void Initialize()
		{
			DisplayController = new SphinxDisplayController(this);
			SoundController = new SphinxSoundController(this, 44100, 2);
			DmaController = new SphinxGeneralDMAController(this);
			SoundDmaController = new SphinxSoundDMAController(this);

			base.Initialize();
		}

		public override void Reset()
		{
			DmaController.Reset();

			base.Reset();
		}

		public override void ResetRegisters()
		{
			isWSCOrGreater = true;

			base.ResetRegisters();
		}

		public override void Shutdown()
		{
			DmaController.Shutdown();

			base.Shutdown();
		}

		protected override void InitializeEepromToDefaults()
		{
			base.InitializeEepromToDefaults();

			InternalEeprom.Program(0x83, (byte)(0b0 << 6 | SoundController.MaxMasterVolume & 0b11)); // Flags (low contrast, max volume)
		}

		public override void RunStep()
		{
			if (RunStepCallback == null || RunStepCallback.Invoke() == false)
			{
				HandleInterrupts();

				var currentCpuClockCycles = DmaController.IsActive ? DmaController.Step() : Cpu.Step();

				var displayInterrupt = DisplayController.Step(currentCpuClockCycles);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.LineCompare)) RaiseInterrupt(4);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.VBlankTimer)) RaiseInterrupt(5);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.VBlank)) RaiseInterrupt(6);
				if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.HBlankTimer)) RaiseInterrupt(7);

				if (SoundDmaController.IsActive)
					SoundDmaController.Step(currentCpuClockCycles);

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

				/* DMA controller */
				case var n when n >= 0x40 && n < 0x4A:
					retVal = DmaController.ReadPort(port);
					break;

				/* Sound DMA controller */
				case var n when n >= 0x4A && n < 0x53:
					retVal = SoundDmaController.ReadPort(port);
					break;

				/* Sound controller */
				case var n when (n >= 0x80 && n < 0xA0) || (n >= 0x6A && n <= 0x6B):
					retVal = SoundController.ReadPort(port);
					break;

				/* Serial port */
				case var n when n == 0xB1 || n == 0xB3:
					retVal = Serial.ReadPort(port);
					break;

				/* Misc system registers */
				case 0x62:
					/* REG_WSC_SYSTEM */
					ChangeBit(ref retVal, 7, false); // not SwanCrystal
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					ChangeBit(ref retVal, 0, cartEnable);
					ChangeBit(ref retVal, 1, true);
					ChangeBit(ref retVal, 2, is16BitExtBus);
					ChangeBit(ref retVal, 3, cartRom1CycleSpeed);
					ChangeBit(ref retVal, 7, builtInSelfTestOk);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					retVal = interruptBase;
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

				/* DMA controller */
				case var n when n >= 0x40 && n < 0x4A:
					DmaController.WritePort(port, value);
					break;

				/* Sound DMA controller */
				case var n when n >= 0x4A && n < 0x53:
					SoundDmaController.WritePort(port, value);
					break;

				/* Sound controller */
				case var n when (n >= 0x80 && n < 0xA0) || (n >= 0x6A && n <= 0x6B):
					SoundController.WritePort(port, value);
					break;

				/* Serial port */
				case var n when n == 0xB1 || n == 0xB3:
					Serial.WritePort(port, value);
					break;

				/* Misc system registers */
				case 0x62:
					/* REG_WSC_SYSTEM */
					if (IsBitSet(value, 0))
					{
						// TODO: power-off bit, stop emulation?
					}
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
					interruptBase = (byte)(value & 0b11111110);
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
		[BitDescription("Interrupt base address", 1, 7)]
		[Format("X4", 0)]
		public override byte InterruptBase => interruptBase;
	}
}
