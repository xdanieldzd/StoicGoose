using System;
using System.Collections.Generic;

using StoicGoose.Core.Cartridges;
using StoicGoose.Core.CPU;
using StoicGoose.Core.Display;
using StoicGoose.Core.EEPROMs;
using StoicGoose.Core.Sound;

using StoicGoose.Common;

using static StoicGoose.Common.Utilities;

namespace StoicGoose.Core.Machines
{
	public abstract class MachineCommon : IMachine, IComponent
	{
		// http://daifukkat.su/docs/wsman/

		public const double MasterClock = 12288000; /* 12.288 MHz xtal */
		public const double CpuClock = MasterClock / 4.0; /* /4 = 3.072 MHz */

		public abstract int InternalRamSize { get; }
		public uint InternalRamMask { get; protected set; } = 0;
		public byte[] InternalRam { get; protected set; } = default;

		public abstract int InternalEepromSize { get; }
		public abstract int InternalEepromAddressBits { get; }

		public Cartridge Cartridge { get; protected set; } = new();
		public V30MZ Cpu { get; protected set; } = default;
		public DisplayControllerCommon DisplayController { get; protected set; } = default;
		public SoundControllerCommon SoundController { get; protected set; } = default;
		public EEPROM InternalEeprom { get; protected set; } = default;
		public byte[] BootstrapRom { get; protected set; } = default;

		public Func<(List<string> buttonsPressed, List<string> buttonsHeld)> ReceiveInput { get; set; } = default;

		public int CurrentClockCyclesInLine { get; protected set; } = 0;
		public int CurrentClockCyclesInFrame { get; protected set; } = 0;

		public int TotalClockCyclesInFrame { get; protected set; } = 0;

		/* REG_HW_FLAGS */
		public bool CartEnable { get; protected set; } = false;
		public bool IsWSCOrGreater { get; protected set; } = false;
		public bool Is16BitExtBus { get; protected set; } = false;
		public bool CartRom1CycleSpeed { get; protected set; } = false;
		public bool BuiltInSelfTestOk { get; protected set; } = false;

		/* REG_KEYPAD */
		public bool KeypadYEnable { get; protected set; } = false;
		public bool KeypadXEnable { get; protected set; } = false;
		public bool KeypadButtonEnable { get; protected set; } = false;

		/* REG_INT_xxx */
		public abstract byte InterruptBase { get; protected set; }
		public byte InterruptEnable { get; protected set; } = 0x00;
		public byte InterruptStatus => interruptStatus;

		/* REG_SER_DATA */
		public byte SerialData { get; protected set; } = 0x00;

		/* REG_SER_STATUS */
		public bool SerialEnable { get; protected set; } = false;
		public bool SerialBaudRateSelect { get; protected set; } = false;
		public bool SerialOverrunReset { get; protected set; } = false;
		public bool SerialSendBufferEmpty { get; protected set; } = false;
		public bool SerialOverrun { get; protected set; } = false;
		public bool SerialDataReceived { get; protected set; } = false;

		/* Backing fields */
		protected byte interruptStatus;

		public bool IsBootstrapLoaded => BootstrapRom != null;

		public MetadataBase Metadata { get; protected set; } = default;

		public virtual void Initialize()
		{
			if (InternalRamSize == -1) throw new Exception("Internal RAM size not set");

			InternalRamMask = (uint)(InternalRamSize - 1);
			InternalRam = new byte[InternalRamSize];

			Cpu = new V30MZ(ReadMemory, WriteMemory, ReadRegister, WriteRegister);
			InternalEeprom = new EEPROM(InternalEepromSize, InternalEepromAddressBits);

			InitializeEepromToDefaults();

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Machine initialized.");
		}

		public virtual void Reset()
		{
			for (var i = 0; i < InternalRam.Length; i++) InternalRam[i] = 0;

			Cartridge?.Reset();
			Cpu?.Reset();
			DisplayController?.Reset();
			SoundController?.Reset();
			InternalEeprom?.Reset();

			CurrentClockCyclesInFrame = 0;
			CurrentClockCyclesInLine = 0;
			TotalClockCyclesInFrame = (int)Math.Round(CpuClock / DisplayControllerCommon.VerticalClock);

			ResetRegisters();

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Machine reset.");
		}

		public virtual void ResetRegisters()
		{
			CartEnable = BootstrapRom == null;
			Is16BitExtBus = true;
			CartRom1CycleSpeed = false;
			BuiltInSelfTestOk = true;

			KeypadYEnable = KeypadXEnable = KeypadButtonEnable = false;

			InterruptBase = InterruptEnable = interruptStatus = 0;

			SerialData = 0;
			SerialEnable = SerialBaudRateSelect = SerialOverrunReset = SerialOverrun = SerialDataReceived = false;

			// TODO: hack for serial stub, always report buffer as empty (fixes ex. Puyo Puyo Tsuu hanging on boot)
			SerialSendBufferEmpty = true;
		}

		public virtual void Shutdown()
		{
			Cartridge?.Shutdown();
			Cpu?.Shutdown();
			DisplayController?.Shutdown();
			SoundController?.Shutdown();
			InternalEeprom?.Shutdown();

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Machine shutdown.");
		}

		protected virtual void InitializeEepromToDefaults()
		{
			var data = ConvertUsernameForEeprom(Metadata.InternalEepromDefaultUsername);
			for (var i = 0; i < data.Length; i++) InternalEeprom.Program(0x60 + i, data[i]); // Username (0x60-0x6F, max 16 characters)
			foreach (var (address, value) in Metadata.InternalEepromDefaultData) InternalEeprom.Program(address, value);
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
			while (CurrentClockCyclesInFrame < TotalClockCyclesInFrame)
				RunLine();

			CurrentClockCyclesInFrame -= TotalClockCyclesInFrame;

			UpdateStatusIcons();

			_ = ReceiveInput?.Invoke();
		}

		public void RunLine()
		{
			while (CurrentClockCyclesInLine < DisplayControllerCommon.HorizontalTotal)
				RunStep();

			CurrentClockCyclesInFrame += CurrentClockCyclesInLine;
			CurrentClockCyclesInLine = 0;
		}

		public abstract void RunStep();

		protected void RaiseInterrupt(int number)
		{
			ChangeBit(ref interruptStatus, number, true);
		}

		protected void LowerInterrupt(int number)
		{
			ChangeBit(ref interruptStatus, number, false);
		}

		protected void HandleInterrupts()
		{
			if (!Cpu.IsFlagSet(V30MZ.Flags.InterruptEnable)) return;

			for (var i = 7; i >= 0; i--)
			{
				if (!IsBitSet(InterruptEnable, i) || !IsBitSet(interruptStatus, i)) continue;

				Cpu.IsHalted = false;
				Cpu.Interrupt((InterruptBase & 0b11111000) | i);
				return;
			}
		}

		public void LoadBootstrap(byte[] data)
		{
			BootstrapRom = new byte[data.Length];
			Buffer.BlockCopy(data, 0, BootstrapRom, 0, data.Length);
		}

		public void LoadInternalEeprom(byte[] data)
		{
			InternalEeprom.LoadContents(data);
		}

		public void LoadRom(byte[] data)
		{
			Cartridge.LoadRom(data);
		}

		public void LoadSaveData(byte[] data)
		{
			if (Cartridge.Metadata.IsSramSave)
				Cartridge.LoadSram(data);
			else if (Cartridge.Metadata.IsEepromSave)
				Cartridge.LoadEeprom(data);
		}

		public byte[] GetInternalEeprom()
		{
			return InternalEeprom.GetContents();
		}

		public byte[] GetSaveData()
		{
			if (Cartridge.Metadata != null)
			{
				if (Cartridge.Metadata.IsSramSave)
					return Cartridge.GetSram();
				else if (Cartridge.Metadata.IsEepromSave)
					return Cartridge.GetEeprom();
			}

			return Array.Empty<byte>();
		}

		public void BeginTraceLog(string filename)
		{
			Cpu.InitializeTraceLogger(filename);
		}

		public void EndTraceLog()
		{
			Cpu.CloseTraceLogger();
		}

		public abstract void UpdateStatusIcons();

		public byte ReadMemory(uint address)
		{
			if (!CartEnable && BootstrapRom != null && address >= (0x100000 - BootstrapRom.Length))
			{
				/* Bootstrap enabled */
				return BootstrapRom[address & (BootstrapRom.Length - 1)];
			}
			else
			{
				address &= 0xFFFFF;

				if ((address & 0xF0000) == 0x00000)
				{
					/* Internal RAM -- returns 0x90 if unmapped */
					if (address < InternalRamSize)
						return InternalRam[address & InternalRamMask];
					else
						return 0x90;
				}
				else
				{
					/* Cartridge */
					return Cartridge.ReadMemory(address);
				}
			}
		}

		public void WriteMemory(uint address, byte value)
		{
			address &= 0xFFFFF;
			if ((address & 0xF0000) == 0x00000)
			{
				/* Internal RAM -- no effect if unmapped */
				if (address < InternalRamSize)
					InternalRam[address & InternalRamMask] = value;
			}
			else if ((address & 0xF0000) == 0x10000)
			{
				/* Cartridge -- SRAM only, other writes not emitted */
				Cartridge.WriteMemory(address, value);
			}
		}

		public abstract byte ReadRegister(ushort register);
		public abstract void WriteRegister(ushort register, byte value);
	}
}
