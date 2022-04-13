using System;
using System.Linq;
using System.Collections.Generic;

using StoicGoose.Debugging;
using StoicGoose.Emulation.Cartridges;
using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.EEPROMs;
using StoicGoose.Emulation.Sound;
using StoicGoose.Interface.Attributes;
using StoicGoose.Interface.Windows;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Machines
{
	public abstract class MachineCommon : IMachine, IComponent
	{
		// TODO: tighter breakpoint hit timing, from check & init to disasm display

		// http://daifukkat.su/docs/wsman/

		public const double MasterClock = 12288000; /* 12.288 MHz xtal */
		public const double CpuClock = MasterClock / 4.0; /* /4 = 3.072 MHz */

		public Breakpoint[] Breakpoints { get; private set; } = new Breakpoint[512];
		public BreakpointVariables BreakpointVariables { get; private set; } = default;

		public event EventHandler<PollInputEventArgs> PollInput = default;
		public void OnPollInput(PollInputEventArgs e) { PollInput?.Invoke(this, e); }

		public event EventHandler<StartOfFrameEventArgs> StartOfFrame = default;
		public void OnStartOfFrame(StartOfFrameEventArgs e) { StartOfFrame?.Invoke(this, e); }

		public event EventHandler<EventArgs> EndOfFrame = default;
		public void OnEndOfFrame(EventArgs e) { EndOfFrame?.Invoke(this, e); }

		public event EventHandler<EventArgs> BreakpointHit = default;
		public void OnBreakpointHit(EventArgs e) { BreakpointHit?.Invoke(this, e); }

		public abstract int InternalRamSize { get; }
		public uint InternalRamMask { get; protected set; } = 0;
		public byte[] InternalRam { get; protected set; } = default;

		public abstract string DefaultUsername { get; }
		public abstract int InternalEepromSize { get; }
		public abstract int InternalEepromAddressBits { get; }

		public Cartridge Cartridge { get; protected set; } = new();
		public V30MZ Cpu { get; protected set; } = default;
		public DisplayControllerCommon DisplayController { get; protected set; } = default;
		public SoundControllerCommon SoundController { get; protected set; } = default;    //TODO "commonize"
		public EEPROM InternalEeprom { get; protected set; } = default;
		public byte[] BootstrapRom { get; protected set; } = default;

		public int CurrentClockCyclesInLine { get; protected set; } = 0;
		public int CurrentClockCyclesInFrame { get; protected set; } = 0;

		public int TotalClockCyclesInFrame { get; protected set; } = 0;

		/* REG_HW_FLAGS */
		[ImGuiRegister(0x0A0, "REG_HW_FLAGS")]
		[ImGuiBitDescription("BIOS lockout; is cartridge mapped?", 0)]
		public bool CartEnable { get; protected set; } = false;
		[ImGuiRegister(0x0A0, "REG_HW_FLAGS")]
		[ImGuiBitDescription("System type; is WSC or greater?", 1)]
		public bool IsWSCOrGreater { get; protected set; } = false;
		[ImGuiRegister(0x0A0, "REG_HW_FLAGS")]
		[ImGuiBitDescription("External bus width; is 16-bit bus?", 2)]
		public bool Is16BitExtBus { get; protected set; } = false;
		[ImGuiRegister(0x0A0, "REG_HW_FLAGS")]
		[ImGuiBitDescription("Cartridge ROM speed; is 1-cycle?", 3)]
		public bool CartRom1CycleSpeed { get; protected set; } = false;
		[ImGuiRegister(0x0A0, "REG_HW_FLAGS")]
		[ImGuiBitDescription("Built-in self test passed", 7)]
		public bool BuiltInSelfTestOk { get; protected set; } = false;

		/* REG_KEYPAD */
		[ImGuiRegister(0x0B5, "REG_KEYPAD")]
		[ImGuiBitDescription("Y keys check enabled", 4)]
		public bool KeypadYEnable { get; protected set; } = false;
		[ImGuiRegister(0x0B5, "REG_KEYPAD")]
		[ImGuiBitDescription("X keys check enabled", 5)]
		public bool KeypadXEnable { get; protected set; } = false;
		[ImGuiRegister(0x0B5, "REG_KEYPAD")]
		[ImGuiBitDescription("Button check enabled", 6)]
		public bool KeypadButtonEnable { get; protected set; } = false;

		/* REG_INT_xxx */
		[ImGuiRegister(0x0B0, "REG_INT_BASE")]
		public abstract byte InterruptBase { get; protected set; }
		[ImGuiRegister(0x0B2, "REG_INT_ENABLE")]
		[ImGuiBitDescription("Interrupt enable bitmask", 4)]
		[ImGuiFormat("X2")]
		public byte InterruptEnable { get; protected set; } = 0x00;
		[ImGuiRegister(0x0B4, "REG_INT_STATUS")]
		[ImGuiBitDescription("Interrupt status bitmask", 4)]
		[ImGuiFormat("X2")]
		public byte InterruptStatus => interruptStatus;

		/* REG_SER_DATA */
		[ImGuiRegister(0x0B1, "REG_SER_DATA")]
		[ImGuiBitDescription("Serial data TX/RX")]
		[ImGuiFormat("X2")]
		public byte SerialData { get; protected set; } = 0x00;

		/* REG_SER_STATUS */
		[ImGuiRegister(0x0B3, "REG_SER_STATUS")]
		[ImGuiBitDescription("Serial enabled", 7)]
		public bool SerialEnable { get; protected set; } = false;
		[ImGuiRegister(0x0B3, "REG_SER_STATUS")]
		[ImGuiBitDescription("Baud rate; is 38400 baud?", 6)]
		public bool SerialBaudRateSelect { get; protected set; } = false;
		[ImGuiRegister(0x0B3, "REG_SER_STATUS")]
		[ImGuiBitDescription("Overrun reset", 5)]
		public bool SerialOverrunReset { get; protected set; } = false;
		[ImGuiRegister(0x0B3, "REG_SER_STATUS")]
		[ImGuiBitDescription("Serial buffer empty?", 2)]
		public bool SerialSendBufferEmpty { get; protected set; } = false;
		[ImGuiRegister(0x0B3, "REG_SER_STATUS")]
		[ImGuiBitDescription("Overrun", 1)]
		public bool SerialOverrun { get; protected set; } = false;
		[ImGuiRegister(0x0B3, "REG_SER_STATUS")]
		[ImGuiBitDescription("Data received", 0)]
		public bool SerialDataReceived { get; protected set; } = false;

		/* Backing fields */
		protected byte interruptStatus;

		public bool IsBootstrapLoaded => BootstrapRom != null;

		public MetadataBase Metadata { get; protected set; } = default;

		protected Cheat[] cheats = new Cheat[512];

		// TODO: move out of machine, into ImGuiHandler, requires userdata writeback!
		public ImGuiCheatWindow CheatsWindow { get; protected set; } = new();
		public ImGuiBreakpointWindow BreakpointWindow { get; protected set; } = new();

		protected Breakpoint lastBreakpointHit = default;
		protected bool breakpointHitReady = false;

		public virtual void Initialize()
		{
			BreakpointVariables = new(this);

			if (InternalRamSize == -1) throw new Exception("Internal RAM size not set");
			if (string.IsNullOrEmpty(DefaultUsername)) throw new Exception("Default username not set");

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

			lastBreakpointHit = default;
			breakpointHitReady = false;

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

		protected void InitializeEepromToDefaults()
		{
			/* Not 100% verified, same caveats as ex. ares */

			var data = ConvertUsernameForEeprom(DefaultUsername);

			for (var i = 0; i < data.Length; i++) InternalEeprom.Program(0x60 + i, data[i]); // Username (0x60-0x6F, max 16 characters)

			InternalEeprom.Program(0x70, 0x19); // Year of birth [just for fun, here set to original WS release date; new systems probably had no date set?]
			InternalEeprom.Program(0x71, 0x99); // ""
			InternalEeprom.Program(0x72, 0x03); // Month of birth [again, WS release for fun]
			InternalEeprom.Program(0x73, 0x04); // Day of birth [and again]
			InternalEeprom.Program(0x74, 0x00); // Sex [set to ?]
			InternalEeprom.Program(0x75, 0x00); // Blood type [set to ?]

			InternalEeprom.Program(0x76, 0x00); // Last game played, publisher ID [set to presumably none]
			InternalEeprom.Program(0x77, 0x00); // ""
			InternalEeprom.Program(0x78, 0x00); // Last game played, game ID [set to presumably none]
			InternalEeprom.Program(0x79, 0x00); // ""
			InternalEeprom.Program(0x7A, 0x00); // (unknown)  -- Swan ID? (see Mama Mitte)
			InternalEeprom.Program(0x7B, 0x00); // (unknown)  -- ""
			InternalEeprom.Program(0x7C, 0x00); // Number of different games played [set to presumably none]
			InternalEeprom.Program(0x7D, 0x00); // Number of times settings were changed [set to presumably none]
			InternalEeprom.Program(0x7E, 0x00); // Number of times powered on [set to presumably none]
			InternalEeprom.Program(0x7F, 0x00); // ""
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

		public void RunFrame(bool isManual)
		{
			if (lastBreakpointHit == null)
			{
				var startOfFrameEventArgs = new StartOfFrameEventArgs();
				OnStartOfFrame(startOfFrameEventArgs);
				if (startOfFrameEventArgs.ToggleMasterVolume) SoundController.ToggleMasterVolume();

				while (CurrentClockCyclesInFrame < TotalClockCyclesInFrame && lastBreakpointHit == null)
					RunLine(isManual);

				CurrentClockCyclesInFrame -= TotalClockCyclesInFrame;

				UpdateStatusIcons();

				OnEndOfFrame(EventArgs.Empty);
			}
		}

		public void RunLine(bool isManual)
		{
			if (lastBreakpointHit == null)
			{
				while (CurrentClockCyclesInLine < DisplayController.ClockCyclesPerLine && lastBreakpointHit == null)
					RunStep(isManual);

				CurrentClockCyclesInFrame += CurrentClockCyclesInLine;
				CurrentClockCyclesInLine = 0;
			}
		}

		public abstract void RunStep(bool isManual);

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

		protected void HandleBreakpoints()
		{
			if (Program.Configuration.Debugging.EnableBreakpoints && lastBreakpointHit == null)
			{
				for (var i = 0; i < Breakpoints.Length; i++)
				{
					if (Breakpoints[i] == null) break;

					if (Breakpoints[i] != lastBreakpointHit && Breakpoints[i].Enabled && Breakpoints[i].Runner(BreakpointVariables).Result)
					{
						OnBreakpointHit(EventArgs.Empty);

						lastBreakpointHit = Breakpoints[i];

						ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, $"Breakpoint hit: ({Breakpoints[i].Expression})");
						break;
					}
				}

				if (breakpointHitReady)
				{
					lastBreakpointHit = null;
					breakpointHitReady = false;
				}
			}
		}

		public void ThreadHasPaused(object sender, EventArgs e)
		{
			if (lastBreakpointHit != null)
				breakpointHitReady = true;
		}

		public void ThreadHasUnpaused(object sender, EventArgs e)
		{
			RunStep(true);

			lastBreakpointHit = null;
			breakpointHitReady = false;
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

		public void LoadCheatList(List<Cheat> cheatList)
		{
			for (var i = 0; i < cheats.Length; i++)
				cheats[i] = (cheatList != null && i < cheatList.Count) ? cheatList[i] : null;
		}

		public void LoadBreakpoints(List<Breakpoint> bpList)
		{
			var invalidIdxs = new List<int>();

			for (var i = 0; i < Breakpoints.Length; i++)
			{
				Breakpoints[i] = (bpList != null && i < bpList.Count) ? bpList[i] : null;
				if (Breakpoints[i] != null && !Breakpoints[i].UpdateDelegate())
					invalidIdxs.Add(i);
			}

			foreach (var idx in invalidIdxs)
			{
				Breakpoints[idx] = null;
				for (var i = idx; i < Breakpoints.Length - 1; i++)
					Breakpoints[i] = Breakpoints[i + 1];
			}
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

		public List<Cheat> GetCheatList()
		{
			return cheats.Where(x => x != null).ToList();
		}

		public List<Breakpoint> GetBreakpoints()
		{
			return Breakpoints.Where(x => x != null).ToList();
		}

		public void BeginTraceLog(string filename)
		{
			Cpu.InitializeTraceLogger(filename);
		}

		public void EndTraceLog()
		{
			Cpu.CloseTraceLogger();
		}

		public void DrawCheatsAndBreakpointWindows()
		{
			CheatsWindow.Draw(cheats);
			BreakpointWindow.Draw(Breakpoints);
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

				/* Handle cheats */
				if (Program.Configuration.General.EnableCheats)
				{
					for (var i = 0; i < cheats.Length; i++)
					{
						if (cheats[i] == null) break;
						if (cheats[i].Address == address && cheats[i].Enabled)
							return cheats[i].Value;
					}
				}

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

		public class Cheat
		{
			public string Description;
			public uint Address;
			public byte Value;
			public bool Enabled;

			public Cheat()
			{
				Description = string.Empty;
				Address = 0;
				Value = 0;
				Enabled = true;
			}
		}
	}
}
