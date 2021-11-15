using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StoicGoose.Emulation.Cartridges;
using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.EEPROMs;
using StoicGoose.Emulation.Sound;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Machines
{
	public partial class WonderSwan
	{
		// http://daifukkat.su/docs/wsman/

		public const double MasterClock = 12288000; /* 12.288 MHz xtal */
		public const double CpuClock = MasterClock / 4.0; /* /4 = 3.072 MHz */

		const int internalRamSize = 16 * 1024;
		const uint internalRamMask = internalRamSize - 1;

		public event EventHandler<RenderScreenEventArgs> RenderScreen
		{
			add { display.RenderScreen += value; }
			remove { display.RenderScreen -= value; }
		}

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { sound.EnqueueSamples += value; }
			remove { sound.EnqueueSamples -= value; }
		}

		public event EventHandler<PollInputEventArgs> PollInput;
		public void OnPollInput(PollInputEventArgs e) { PollInput?.Invoke(this, e); }

		public event EventHandler<StartOfFrameEventArgs> StartOfFrame;
		public void OnStartOfFrame(StartOfFrameEventArgs e) { StartOfFrame?.Invoke(this, e); }

		public event EventHandler<EventArgs> EndOfFrame;
		public void OnEndOfFrame(EventArgs e) { EndOfFrame?.Invoke(this, e); }

		readonly byte[] internalRam = new byte[internalRamSize];

		readonly Cartridge cartridge = new Cartridge();
		readonly V30MZ cpu = default;
		readonly DisplayController display = default;
		readonly SoundController sound = default;
		readonly EEPROM eeprom = default;

		byte[] bootstrapRom = default;

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		/* REG_HW_FLAGS */
		bool hwCartEnable, hw16BitExtBus, hwCartRom1CycleSpeed, hwSelfTestOk;
		/* REG_KEYPAD */
		bool keypadButtonEnable, keypadXEnable, keypadYEnable;
		/* REG_INT_xxx */
		byte intBase, intEnable, intStatus;
		/* REG_SER_xxx */
		byte serData;
		bool serEnable, serBaudRateSelect, serOverrunReset, serSendBufferEmpty, serOverrun, serDataReceived;

		public bool IsBootstrapLoaded => bootstrapRom != null;

		public WonderSwan()
		{
			cpu = new V30MZ(ReadMemory, WriteMemory, ReadRegister, WriteRegister);
			display = new DisplayController(ReadMemory);
			sound = new SoundController(ReadMemory, 44100, 2);
			eeprom = new EEPROM(64 * 2, 6);

			InitializeEepromToDefaults();
		}

		public void Reset()
		{
			for (var i = 0; i < internalRam.Length; i++) internalRam[i] = 0;

			cartridge.Reset();
			cpu.Reset();
			display.Reset();
			sound.Reset();
			eeprom.Reset();

			currentMasterClockCyclesInFrame = 0;
			totalMasterClockCyclesInFrame = (int)Math.Round(MasterClock / DisplayController.VerticalClock);

			ResetRegisters();
		}

		private void ResetRegisters()
		{
			hwCartEnable = bootstrapRom == null;
			hw16BitExtBus = true;
			hwCartRom1CycleSpeed = false;
			hwSelfTestOk = true;

			keypadButtonEnable = keypadXEnable = keypadYEnable = false;

			intBase = intEnable = intStatus = 0;

			serData = 0;
			serEnable = serBaudRateSelect = serOverrunReset = serOverrun = serDataReceived = false;

			// TODO: hack for serial stub, always report buffer as empty (fixes ex. Puyo Puyo Tsuu hanging on boot)
			serSendBufferEmpty = true;
		}

		private void InitializeEepromToDefaults()
		{
			/* Not 100% verified, same caveats as ex. ares */

			var data = ConvertUsernameForEeprom("WONDERSWAN");

			for (var i = 0; i < data.Length; i++) eeprom.Program(0x60 + i, data[i]); // Username (0x60-0x6F, max 16 characters)

			eeprom.Program(0x70, 0x19); // Year of birth [just for fun, here set to original WS release date; new systems probably had no date set?]
			eeprom.Program(0x71, 0x99); // ""
			eeprom.Program(0x72, 0x03); // Month of birth [again, WS release for fun]
			eeprom.Program(0x73, 0x04); // Day of birth [and again]
			eeprom.Program(0x74, 0x00); // Sex [set to ?]
			eeprom.Program(0x75, 0x00); // Blood type [set to ?]

			eeprom.Program(0x76, 0x00); // Last game played, publisher ID [set to presumably none]
			eeprom.Program(0x77, 0x00); // ""
			eeprom.Program(0x78, 0x00); // Last game played, game ID [set to presumably none]
			eeprom.Program(0x79, 0x00); // ""
			eeprom.Program(0x7A, 0x00); // (unknown)
			eeprom.Program(0x7B, 0x00); // (unknown)
			eeprom.Program(0x7C, 0x00); // Number of different games played [set to presumably none]
			eeprom.Program(0x7D, 0x00); // Number of times settings were changed [set to presumably none]
			eeprom.Program(0x7E, 0x00); // Number of times powered on [set to presumably none]
			eeprom.Program(0x7F, 0x00); // ""
		}

		private byte[] ConvertUsernameForEeprom(string name)
		{
			var data = new byte[16];
			for (var i = 0; i < data.Length; i++)
			{
				var c = i < name.Length ? name[i] : ' ';
				if (c == ' ') data[i] = (byte)(c - ' ' + 0x00);
				else if (c >= '0' && c <= '9') data[i] = (byte)(c - '0' + 0x01);
				else if (c >= 'A' && c <= 'Z') data[i] = (byte)(c - 'A' + 0x0B);
				else if (c >= 'a' && c <= 'b') data[i] = (byte)(c - 'A' + 0x0B);
				else if (c == '♥') data[i] = (byte)(c - '♥' + 0x25);
				else if (c == '♪') data[i] = (byte)(c - '♪' + 0x26);
				else if (c == '+') data[i] = (byte)(c - '+' + 0x27);
				else if (c == '-') data[i] = (byte)(c - '-' + 0x28);
				else if (c == '?') data[i] = (byte)(c - '?' + 0x29);
				else if (c == '.') data[i] = (byte)(c - '.' + 0x2A);
				else data[i] = 0x00;
			}
			return data;
		}

		public void RunFrame()
		{
			var startOfFrameEventArgs = new StartOfFrameEventArgs();
			OnStartOfFrame(startOfFrameEventArgs);
			if (startOfFrameEventArgs.ToggleMasterVolume) sound.ToggleMasterVolume();

			while (currentMasterClockCyclesInFrame < totalMasterClockCyclesInFrame)
				RunStep();

			currentMasterClockCyclesInFrame -= totalMasterClockCyclesInFrame;

			UpdateMetadata();

			OnEndOfFrame(EventArgs.Empty);
		}

		private void RunStep()
		{
			double currentCpuClockCycles = 0.0;
			currentCpuClockCycles += cpu.Step();

			var displayInterrupt = display.Step((int)Math.Round(currentCpuClockCycles));

			if (displayInterrupt.HasFlag(DisplayController.DisplayInterrupts.LineCompare) && IsBitSet(intEnable, 4))
				ChangeBit(ref intStatus, 4, true);

			if (displayInterrupt.HasFlag(DisplayController.DisplayInterrupts.VBlankTimer) && IsBitSet(intEnable, 5))
				ChangeBit(ref intStatus, 5, true);

			if (displayInterrupt.HasFlag(DisplayController.DisplayInterrupts.VBlank) && IsBitSet(intEnable, 6))
				ChangeBit(ref intStatus, 6, true);

			if (displayInterrupt.HasFlag(DisplayController.DisplayInterrupts.HBlankTimer) && IsBitSet(intEnable, 7))
				ChangeBit(ref intStatus, 7, true);

			CheckAndRaiseInterrupts();

			sound.Step((int)Math.Round(currentCpuClockCycles));

			currentMasterClockCyclesInFrame += (int)Math.Round(currentCpuClockCycles);
		}

		private void CheckAndRaiseInterrupts()
		{
			for (var i = 7; i >= 0; i--)
			{
				if (!IsBitSet(intEnable, i) || !IsBitSet(intStatus, i)) continue;
				cpu.RaiseInterrupt(intBase + i);
				ChangeBit(ref intStatus, i, false);
				break;
			}
		}

		public void LoadBootstrap(byte[] data)
		{
			bootstrapRom = new byte[data.Length];
			Buffer.BlockCopy(data, 0, bootstrapRom, 0, data.Length);
		}

		public void LoadInternalEeprom(byte[] data)
		{
			eeprom.LoadContents(data);
		}

		public void LoadRom(byte[] data)
		{
			cartridge.LoadRom(data);

			Metadata["cartridge/id"].Value = cartridge.Metadata.GameIdString;
			Metadata["cartridge/publisher"].Value = cartridge.Metadata.PublisherName;
			Metadata["cartridge/orientation"].Value = cartridge.Metadata.Orientation.ToString().ToLowerInvariant();
			Metadata["cartridge/savetype"].Value = cartridge.Metadata.IsSramSave ? "sram" : (cartridge.Metadata.IsEepromSave ? "eeprom" : "none");
		}

		public void LoadSaveData(byte[] data)
		{
			if (Metadata["cartridge/savetype"] == "sram")
				cartridge.LoadSram(data);
			else if (Metadata["cartridge/savetype"] == "eeprom")
			{
				//TODO
			}
		}

		public byte[] GetInternalEeprom()
		{
			return eeprom.GetContents();
		}

		public byte[] GetSaveData()
		{
			if (Metadata["cartridge/savetype"] == "sram")
				return cartridge.GetSram();
			else if (Metadata["cartridge/savetype"] == "eeprom")
			{
				//TODO
			}
			return new byte[0];
		}

		public byte[] GetInternalRam()
		{
			return internalRam.Clone() as byte[];
		}

		public (int w, int h) GetScreenSize()
		{
			return (DisplayController.ScreenWidth, DisplayController.ScreenHeight);
		}

		public double GetRefreshRate()
		{
			return DisplayController.VerticalClock;
		}

		public byte ReadMemory(uint address)
		{
			if (!hwCartEnable && address >= (0x100000 - bootstrapRom.Length))
			{
				/* Bootstrap enabled */
				return bootstrapRom[address & (bootstrapRom.Length - 1)];
			}
			else
			{
				address &= 0xFFFFF;
				if ((address & 0xF0000) == 0x00000)
				{
					/* Internal RAM -- returns 0x90 if unmapped */
					if (address < internalRamSize)
						return internalRam[address & internalRamMask];
					else
						return 0x90;
				}
				else
				{
					/* Cartridge */
					return cartridge.ReadMemory(address);
				}
			}
		}

		public void WriteMemory(uint address, byte value)
		{
			address &= 0xFFFFF;
			if ((address & 0xF0000) == 0x00000)
			{
				/* Internal RAM -- no effect if unmapped */
				if (address < internalRamSize)
					internalRam[address & internalRamMask] = value;
			}
			else if ((address & 0xF0000) == 0x10000)
			{
				/* Cartridge -- SRAM only, other writes not emitted */
				cartridge.WriteMemory(address, value);
			}
		}

		public byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					retVal = display.ReadRegister(register);
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
					ChangeBit(ref retVal, 2, hw16BitExtBus);
					ChangeBit(ref retVal, 3, hwCartRom1CycleSpeed);
					ChangeBit(ref retVal, 7, hwSelfTestOk);
					break;

				case 0xB0:
					/* REG_INT_BASE */
					retVal = intBase;
					retVal |= 0b11;
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

		public void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				/* Display controller, etc. (H/V timers, DISP_MODE) */
				case var n when (n >= 0x00 && n < 0x40) || n == 0x60 || n == 0xA2 || (n >= 0xA4 && n <= 0xAB):
					display.WriteRegister(register, value);
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
					intBase = (byte)(value & 0b11111000);
					break;

				case 0xB1:
					/* REG_SER_DATA */
					serData = value;
					break;

				case 0xB2:
					/* REG_INT_ENABLE */
					intEnable = value;
					//intStatus &= (byte)~intEnable;	// TODO verify
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
