using System;
using System.Collections.Generic;

using StoicGoose.Core.Cartridges;
using StoicGoose.Core.CPU;
using StoicGoose.Core.Display;
using StoicGoose.Core.EEPROMs;
using StoicGoose.Core.Machines;
using StoicGoose.Core.Sound;

namespace StoicGoose.Core.Interfaces
{
	public interface IMachine
	{
		MetadataBase Metadata { get; }

		Cartridge Cartridge { get; }
		V30MZ Cpu { get; }
		DisplayControllerCommon DisplayController { get; }
		SoundControllerCommon SoundController { get; }
		EEPROM InternalEeprom { get; }

		Func<(List<string> buttonsPressed, List<string> buttonsHeld)> ReceiveInput { get; set; }

		void Initialize();
		void Reset();
		void Shutdown();

		void RunFrame();
		void RunLine();
		void RunStep();

		void LoadBootstrap(byte[] data);
		bool IsBootstrapLoaded { get; }
		void LoadInternalEeprom(byte[] data);
		void LoadRom(byte[] data);
		void LoadSaveData(byte[] data);

		byte[] GetInternalEeprom();
		byte[] GetSaveData();

		byte ReadMemory(uint address);
		void WriteMemory(uint address, byte value);
		byte ReadPort(ushort port);
		void WritePort(ushort port, byte value);
	}
}
