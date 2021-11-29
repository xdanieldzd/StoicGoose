using System;

using StoicGoose.DataStorage;
using StoicGoose.WinForms;

namespace StoicGoose.Emulation.Machines
{
	public interface IMachine
	{
		ObjectStorage Metadata { get; }

		event EventHandler<RenderScreenEventArgs> RenderScreen;
		event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		event EventHandler<PollInputEventArgs> PollInput;
		event EventHandler<StartOfFrameEventArgs> StartOfFrame;
		event EventHandler<EventArgs> EndOfFrame;

		void Initialize();
		void Reset();
		void Shutdown();

		void RunFrame();

		void LoadBootstrap(byte[] data);
		bool IsBootstrapLoaded { get; }
		void LoadInternalEeprom(byte[] data);
		void LoadRom(byte[] data);
		void LoadSaveData(byte[] data);

		byte[] GetInternalEeprom();
		byte[] GetSaveData();

		byte[] GetInternalRam();

		(int w, int h) GetScreenSize();
		double GetRefreshRate();

		byte ReadMemory(uint address);
		void WriteMemory(uint address, byte value);
		byte ReadRegister(ushort register);
		void WriteRegister(ushort register, byte value);

		(ushort cs, ushort ip) GetProcessorStatus();
	}
}
