using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StoicGoose.Emulation.Cartridges;
using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.Sound;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Machines
{
	public partial class WonderSwan
	{
		// http://daifukkat.su/docs/wsman/

		public const double MasterClock = 12288000;
		public const double CpuClock = MasterClock / 4.0;

		const int internalRamSize = 16 * 1024;
		const uint internalRamMask = internalRamSize - 1;

		const int internalEepromSize = 64;

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
		readonly ushort[] internalEeprom = new ushort[internalEepromSize];

		readonly Cartridge cartridge = new Cartridge();
		readonly V30MZ cpu = default;
		readonly DisplayController display = default;
		readonly SoundController sound = default;

		byte[] bootstrapRom = default;

		int currentMasterClockCyclesInFrame, totalMasterClockCyclesInFrame;

		/* REG_HW_FLAGS */
		bool hwCartEnable, hw16BitExtBus, hwCartRom1CycleSpeed, hwSelfTestOk;
		/* REG_KEYPAD */
		bool keypadButtonEnable, keypadXEnable, keypadYEnable;
		/* REG_IEEP_xxx */
		ushort eepData, eepAddressHiLo;
		bool eepStart, eepWriteEnable;
		byte eepCommand, eepAddress, eepStatus;
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
			sound = new SoundController(ReadMemory);
		}

		public void Reset()
		{
			for (var i = 0; i < internalRam.Length; i++) internalRam[i] = 0;
			InitializeEeprom();

			cartridge.Reset();
			cpu.Reset();
			display.Reset();
			sound.Reset();

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

			eepData = eepAddressHiLo = 0;
			eepStart = eepWriteEnable = false;
			eepCommand = eepAddress = eepStatus = 0;

			intBase = intEnable = intStatus = 0;

			serData = 0;
			serEnable = serBaudRateSelect = serOverrunReset = serOverrun = serDataReceived = false;

			// TODO: hack for serial stub, always report buffer as empty (fixes ex. Puyo Puyo Tsuu hanging on boot)
			serSendBufferEmpty = true;
		}

		private void InitializeEeprom()
		{
			// TODO

			var username = false ? "STOICGOOSE♥♪+-?." : "WONDERSWAN";
			InitializeEepromUsername(username);
			internalEeprom[0x38] = 0x9919;  // Birth year BCD (ex. 87 19)
			internalEeprom[0x39] = 0x0403;  // Birth day + month BCD (ex. 18 02)
			internalEeprom[0x3A] = 0x0000;  // Sex + blood type
		}

		private void InitializeEepromUsername(string name)
		{
			// TODO

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
			}

			for (var i = 0; i < data.Length; i += 2)
				internalEeprom[0x30 + (i / 2)] = (ushort)(data[i + 1] << 8 | data[i + 0]);
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
					/* REG_IEEP_DATA (low) */
					retVal = (byte)(eepData & 0xFF);
					break;

				case 0xBB:
					/* REG_IEEP_DATA (high) */
					retVal = (byte)((eepData >> 8) & 0xFF);
					break;

				case 0xBC:
					/* REG_IEEP_ADDR (low) */
					retVal = (byte)(eepAddressHiLo & 0xFF);
					break;

				case 0xBD:
					/* REG_IEEP_ADDR (high) */
					retVal = (byte)((eepAddressHiLo >> 8) & 0xFF);
					break;

				case 0xBE:
					/* REG_IEEP_STATUS (read) */
					retVal = (byte)(eepStatus & 0b11);
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
					/* REG_IEEP_DATA (low) */
					eepData = (ushort)((eepData & 0xFF00) | value);
					break;

				case 0xBB:
					/* REG_IEEP_DATA (high) */
					eepData = (ushort)((eepData & 0x00FF) | (value << 8));
					break;

				case 0xBC:
					/* REG_IEEP_ADDR (low) */
					eepAddressHiLo = (ushort)((eepAddressHiLo & 0xFF00) | value);

					eepStart = ((eepAddressHiLo >> 8) & 0b1) == 0b1;
					eepAddress = (byte)((eepAddressHiLo >> 0) & 0b111111);
					eepCommand = (byte)((eepAddressHiLo >> 6) & 0b11);
					break;

				case 0xBD:
					/* REG_IEEP_ADDR (high) */
					eepAddressHiLo = (ushort)((eepAddressHiLo & 0x00FF) | (value << 8));

					eepStart = ((eepAddressHiLo >> 8) & 0b1) == 0b1;
					eepAddress = (byte)((eepAddressHiLo >> 0) & 0b111111);
					eepCommand = (byte)((eepAddressHiLo >> 6) & 0b11);
					break;

				case 0xBE:
					/* REG_IEEP_CMD (write) */

					// TODO
					if (!eepStart) break;   // ????

					switch ((value >> 4) & 0b111)
					{
						case 0b001:
							/* READ */
							// TODO
							eepData = internalEeprom[eepAddress];
							eepStatus = 3;
							break;

						case 0b010:
							/* WRITE/WRAL */
							// TODO
							eepStatus = 2;
							break;

						case 0b100:
							/* EWEN/EWDS/ERAL/ERASE */
							// TODO
							if (eepCommand == 0)
							{
								var extCommand = (eepAddress >> 4) & 0b11;
								switch (extCommand)
								{
									case 0:
										/* EWDS */
										eepWriteEnable = false;
										break;
									case 2:
										/* ERAL */
										if (eepWriteEnable)
											for (var i = 0; i < internalEeprom.Length; i++)
												internalEeprom[i] = 0xFFFF;
										break;
									case 3:
										/* EWEN */
										eepWriteEnable = true;
										break;
								}
							}
							else
							{
								if (eepWriteEnable)
									internalEeprom[eepAddress] = 0;
							}

							eepStatus = 2;
							break;
					}
					break;

				/* Cartridge */
				case var n when n >= 0xC0 && n < 0x100:
					cartridge.WriteRegister(register, value);
					break;
			}
		}
	}
}
