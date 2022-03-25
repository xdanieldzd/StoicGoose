using System;
using System.Collections.Generic;

using StoicGoose.DataStorage;
using StoicGoose.WinForms;

namespace StoicGoose.Emulation.Machines
{
	public interface IMachine
	{
		Dictionary<string, ObjectValue> Metadata { get; }

		event EventHandler<UpdateScreenEventArgs> UpdateScreen;
		event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		event EventHandler<PollInputEventArgs> PollInput;
		event EventHandler<StartOfFrameEventArgs> StartOfFrame;
		event EventHandler<EventArgs> EndOfFrame;

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

		(int w, int h) GetScreenSize();
		double GetRefreshRate();

		byte ReadMemory(uint address);
		void WriteMemory(uint address, byte value);
		byte ReadRegister(ushort register);
		void WriteRegister(ushort register, byte value);

		Dictionary<string, ushort> GetProcessorStatus();

		void BeginTraceLog(string filename);
		void EndTraceLog();

		void DrawImGuiWindows();
	}
}
