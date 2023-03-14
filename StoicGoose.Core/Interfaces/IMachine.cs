using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using StoicGoose.Core.Cartridges;
using StoicGoose.Core.CPU;
using StoicGoose.Core.Display;
using StoicGoose.Core.EEPROMs;
using StoicGoose.Core.Sound;

namespace StoicGoose.Core.Interfaces
{
	public interface IMachine
	{
		static IEnumerable<Type> GetMachineTypes() => Assembly.GetAssembly(typeof(IMachine)).GetTypes().Where(x => !x.IsInterface && !x.IsAbstract && x.IsAssignableTo(typeof(IMachine)));

		string Manufacturer { get; }
		string Model { get; }

		int ScreenWidth { get; }
		int ScreenHeight { get; }
		double RefreshRate { get; }
		string GameControls { get; }
		string HardwareControls { get; }
		string VerticalControlRemap { get; }

		string InternalEepromDefaultUsername { get; }
		Dictionary<ushort, byte> InternalEepromDefaultData { get; }

		Cartridge Cartridge { get; }
		V30MZ Cpu { get; }
		DisplayControllerCommon DisplayController { get; }
		SoundControllerCommon SoundController { get; }
		EEPROM InternalEeprom { get; }

		Func<(List<string> buttonsPressed, List<string> buttonsHeld)> ReceiveInput { get; set; }

		Func<uint, byte, byte> ReadMemoryCallback { get; set; }
		Action<uint, byte> WriteMemoryCallback { get; set; }
		Func<ushort, byte, byte> ReadPortCallback { get; set; }
		Action<ushort, byte> WritePortCallback { get; set; }
		Func<bool> RunStepCallback { get; set; }

		void Initialize();
		void Reset();
		void Shutdown();

		void RunFrame();
		void RunLine();
		void RunStep();

		void LoadBootstrap(byte[] data);
		bool IsBootstrapLoaded { get; }
		bool UseBootstrap { get; set; }
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
