using System;
using System.Linq;

using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.DMA;
using StoicGoose.Emulation.EEPROMs;
using StoicGoose.Emulation.Sound;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Machines
{
	public partial class WonderSwanColor : MachineCommon
	{
		SphinxDMAController dma = default;

		public override void Initialize()
		{
			internalRamSize = 64 * 1024;
			internalRamMask = (uint)(internalRamSize - 1);
			internalRam = new byte[internalRamSize];

			cpu = new V30MZ(ReadMemory, WriteMemory, ReadRegister, WriteRegister);
			display = new SphinxDisplayController(ReadMemory);
			sound = new SoundController(ReadMemory, 44100, 2);
			eeprom = new EEPROM(1024 * 2, 10);
			dma = new SphinxDMAController(ReadMemory, WriteMemory);

			InitializeEepromToDefaults("WONDERSWANCOLOR");
		}

		public override void Reset()
		{
			for (var i = 0; i < internalRam.Length; i++) internalRam[i] = 0;

			cartridge.Reset();
			cpu.Reset();
			display.Reset();
			sound.Reset();
			eeprom.Reset();
			dma.Reset();

			currentClockCyclesInFrame = 0;
			totalClockCyclesInFrame = (int)Math.Round(CpuClock / DisplayControllerCommon.VerticalClock);

			ResetRegisters();
		}

		public override void Shutdown()
		{
			cartridge.Shutdown();
			cpu.Shutdown();
			display.Shutdown();
			sound.Shutdown();
			eeprom.Shutdown();
			dma.Shutdown();
		}

		public override void RunStep()
		{
			var currentCpuClockCycles = dma.IsActive ? dma.Step() : cpu.Step();

			var displayInterrupt = display.Step(currentCpuClockCycles);
			if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.LineCompare)) ChangeBit(ref intStatus, 4, true);
			if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.VBlankTimer)) ChangeBit(ref intStatus, 5, true);
			if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.VBlank)) ChangeBit(ref intStatus, 6, true);
			if (displayInterrupt.HasFlag(DisplayControllerCommon.DisplayInterrupts.HBlankTimer)) ChangeBit(ref intStatus, 7, true);

			CheckAndRaiseInterrupts();

			sound.Step(currentCpuClockCycles);

			currentClockCyclesInFrame += currentCpuClockCycles;
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					retVal = display.ReadRegister(register);
					break;

				/* DMA controller */
				case var n when n >= 0x40 && n < 0x4A:
					retVal = dma.ReadRegister(register);
					break;

				/* Misc system registers */
				case var n when n >= 0x60 && n < 0x80:
					// TODO?
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					retVal = sound.ReadRegister(register);
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					ChangeBit(ref retVal, 0, hwCartEnable);
					ChangeBit(ref retVal, 1, true);
					ChangeBit(ref retVal, 2, hw16BitExtBus);
					ChangeBit(ref retVal, 3, hwCartRom1CycleSpeed);
					ChangeBit(ref retVal, 7, hwSelfTestOk);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					retVal = intBase;
					break;

				case 0xB1:
					/* REG_SER_DATA */
					retVal = serData;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					retVal = intEnable;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					//TODO: Puyo Puyo Tsuu accesses this, stub properly?
					ChangeBit(ref retVal, 7, serEnable);
					ChangeBit(ref retVal, 6, serBaudRateSelect);
					ChangeBit(ref retVal, 5, serOverrunReset);
					ChangeBit(ref retVal, 2, serSendBufferEmpty);
					ChangeBit(ref retVal, 1, serOverrun);
					ChangeBit(ref retVal, 0, serDataReceived);
					break;

				case 0xB4:
					/* REG_INT_STATUS */
					retVal = intStatus;
					break;

				case 0xB5:
					/* REG_KEYPAD */

					/* Get input from UI */
					var eventArgs = new PollInputEventArgs();
					OnPollInput(eventArgs);

					ChangeBit(ref retVal, 4, keypadYEnable);
					ChangeBit(ref retVal, 5, keypadXEnable);
					ChangeBit(ref retVal, 6, keypadButtonEnable);

					if (eventArgs.ButtonsHeld.Any())
						ChangeBit(ref intStatus, 1, true);

					if (keypadYEnable)
					{
						if (eventArgs.ButtonsHeld.Contains("y1")) ChangeBit(ref retVal, 0, true);
						if (eventArgs.ButtonsHeld.Contains("y2")) ChangeBit(ref retVal, 1, true);
						if (eventArgs.ButtonsHeld.Contains("y3")) ChangeBit(ref retVal, 2, true);
						if (eventArgs.ButtonsHeld.Contains("y4")) ChangeBit(ref retVal, 3, true);
					}

					if (keypadXEnable)
					{
						if (eventArgs.ButtonsHeld.Contains("x1")) ChangeBit(ref retVal, 0, true);
						if (eventArgs.ButtonsHeld.Contains("x2")) ChangeBit(ref retVal, 1, true);
						if (eventArgs.ButtonsHeld.Contains("x3")) ChangeBit(ref retVal, 2, true);
						if (eventArgs.ButtonsHeld.Contains("x4")) ChangeBit(ref retVal, 3, true);
					}

					if (keypadButtonEnable)
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
					retVal = eeprom.ReadRegister((ushort)(register - 0xBA));
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					retVal = cartridge.ReadRegister(register);
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
					display.WriteRegister(register, value);
					break;

				/* DMA controller */
				case var n when n >= 0x40 && n < 0x4A:
					dma.WriteRegister(register, value);
					break;

				/* Misc system registers */
				case var n when n >= 0x60 && n < 0x80:
					// TODO?
					break;

				/* Sound controller */
				case var n when n >= 0x80 && n < 0xA0:
					sound.WriteRegister(register, value);
					break;

				/* System controller */
				case 0xA0:
					/* REG_HW_FLAGS */
					if (!hwCartEnable)
						hwCartEnable = IsBitSet(value, 0);
					hw16BitExtBus = IsBitSet(value, 2);
					hwCartRom1CycleSpeed = IsBitSet(value, 3);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					intBase = (byte)(value & 0b11111110);
					break;

				case 0xB1:
					/* REG_SER_DATA */
					serData = value;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					intEnable = value;
					break;

				case 0xB3:
					/* REG_SER_STATUS */
					serEnable = IsBitSet(value, 7);
					serBaudRateSelect = IsBitSet(value, 6);
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
					intStatus &= (byte)~(value & 0b11110010);
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
					eeprom.WriteRegister((ushort)(register - 0xBA), value);
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					cartridge.WriteRegister(register, value);
					break;
			}
		}
	}
}
