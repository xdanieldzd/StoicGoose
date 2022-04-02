using System;
using System.Collections.Generic;

using StoicGoose.Debugging;
using StoicGoose.Emulation.Cartridges;
using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.EEPROMs;
using StoicGoose.Emulation.Sound;
using StoicGoose.Interface.Windows;
using StoicGoose.WinForms;

namespace StoicGoose.Emulation.Machines
{
	public interface IMachine
	{
		MetadataBase Metadata { get; }

		Breakpoint[] Breakpoints { get; }
		BreakpointVariables BreakpointVariables { get; }

		event EventHandler<PollInputEventArgs> PollInput;
		event EventHandler<StartOfFrameEventArgs> StartOfFrame;
		event EventHandler<EventArgs> EndOfFrame;
		event EventHandler<EventArgs> BreakpointHit;

		Cartridge Cartridge { get; }
		V30MZ Cpu { get; }
		DisplayControllerCommon DisplayController { get; }
		SoundController SoundController { get; }
		EEPROM InternalEeprom { get; }
		// TODO: Sphinx DMA controller?

		ImGuiCheatWindow CheatsWindow { get; }
		ImGuiBreakpointWindow BreakpointWindow { get; }

		void Initialize();
		void Reset();
		void Shutdown();

		void RunFrame(bool isManual);
		void RunLine(bool isManual);
		void RunStep(bool isManual);

		void ThreadHasPaused(object sender, EventArgs e);
		void ThreadHasUnpaused(object sender, EventArgs e);

		void LoadBootstrap(byte[] data);
		bool IsBootstrapLoaded { get; }
		void LoadInternalEeprom(byte[] data);
		void LoadRom(byte[] data);
		void LoadSaveData(byte[] data);
		void LoadCheatList(List<MachineCommon.Cheat> cheatList);
		void LoadBreakpoints(List<Breakpoint> breakpoints);

		byte[] GetInternalEeprom();
		byte[] GetSaveData();
		List<MachineCommon.Cheat> GetCheatList();
		List<Breakpoint> GetBreakpoints();

		byte ReadMemory(uint address);
		void WriteMemory(uint address, byte value);
		byte ReadRegister(ushort register);
		void WriteRegister(ushort register, byte value);

		void BeginTraceLog(string filename);
		void EndTraceLog();

		void DrawCheatsAndBreakpointWindows();
	}
}
