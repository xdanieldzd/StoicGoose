using System;
using System.Collections.Generic;

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

		event EventHandler<PollInputEventArgs> PollInput;
		event EventHandler<StartOfFrameEventArgs> StartOfFrame;
		event EventHandler<EventArgs> EndOfFrame;

		Cartridge Cartridge { get; }
		V30MZ Cpu { get; }
		DisplayControllerCommon DisplayController { get; }
		SoundController SoundController { get; }
		EEPROM InternalEeprom { get; }
		// TODO: Sphinx DMA controller?

		ImGuiCheatWindow CheatsWindow { get; }

		void Initialize();
		void Reset();
		void Shutdown();

		void RunFrame();
		void RunStep();

		void LoadBootstrap(byte[] data);
		bool IsBootstrapLoaded { get; }
		void LoadInternalEeprom(byte[] data);
		void LoadRom(byte[] data);
		void LoadSaveData(byte[] data);
		void LoadCheatList(List<MachineCommon.Cheat> cheatList);

		byte[] GetInternalEeprom();
		byte[] GetSaveData();
		List<MachineCommon.Cheat> GetCheatList();

		byte ReadMemory(uint address);
		void WriteMemory(uint address, byte value);
		byte ReadRegister(ushort register);
		void WriteRegister(ushort register, byte value);

		void BeginTraceLog(string filename);
		void EndTraceLog();

		void DrawCheatsWindow();
	}
}
