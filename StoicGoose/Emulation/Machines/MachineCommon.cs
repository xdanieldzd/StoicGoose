using System;
using System.Collections.Generic;

using ImGuiNET;

using StoicGoose.DataStorage;
using StoicGoose.Emulation.Cartridges;
using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Display;
using StoicGoose.Emulation.EEPROMs;
using StoicGoose.Emulation.Sound;
using StoicGoose.WinForms;

using NumericsVector2 = System.Numerics.Vector2;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Machines
{
	public abstract class MachineCommon : IMachine
	{
		// http://daifukkat.su/docs/wsman/

		public const double MasterClock = 12288000; /* 12.288 MHz xtal */
		public const double CpuClock = MasterClock / 4.0; /* /4 = 3.072 MHz */

		public event EventHandler<UpdateScreenEventArgs> UpdateScreen
		{
			add { display.UpdateScreen += value; }
			remove { display.UpdateScreen -= value; }
		}

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { sound.EnqueueSamples += value; }
			remove { sound.EnqueueSamples -= value; }
		}

		public event EventHandler<PollInputEventArgs> PollInput = default;
		public void OnPollInput(PollInputEventArgs e) { PollInput?.Invoke(this, e); }

		public event EventHandler<StartOfFrameEventArgs> StartOfFrame = default;
		public void OnStartOfFrame(StartOfFrameEventArgs e) { StartOfFrame?.Invoke(this, e); }

		public event EventHandler<EventArgs> EndOfFrame = default;
		public void OnEndOfFrame(EventArgs e) { EndOfFrame?.Invoke(this, e); }

		protected int internalRamSize = -1;
		protected uint internalRamMask = 0;
		protected byte[] internalRam = default;

		protected readonly Cartridge cartridge = new();
		protected V30MZ cpu = default;
		protected DisplayControllerCommon display = default;
		protected SoundController sound = default;    //TODO "commonize"
		protected EEPROM eeprom = default;
		protected byte[] bootstrapRom = default;

		protected int currentClockCyclesInFrame = 0, totalClockCyclesInFrame = 0;

		/* REG_HW_FLAGS */
		protected bool hwCartEnable, hw16BitExtBus, hwCartRom1CycleSpeed, hwSelfTestOk;
		/* REG_KEYPAD */
		protected bool keypadButtonEnable, keypadXEnable, keypadYEnable;
		/* REG_INT_xxx */
		protected byte intBase, intEnable, intStatus;
		/* REG_SER_xxx */
		protected byte serData;
		protected bool serEnable, serBaudRateSelect, serOverrunReset, serSendBufferEmpty, serOverrun, serDataReceived;

		public bool IsBootstrapLoaded => bootstrapRom != null;

		public Dictionary<string, ObjectValue> Metadata { get; } = new();

		protected Cheat[] cheats = new Cheat[512];
		protected bool isCheatsUIOpen = true;
		protected Cheat newCheatToAdd = default;
		protected int cheatToEditIdx = -1, cheatToDeleteIdx = -1;

		public List<Action> ImGuiDrawFunctions { get; } = new();

		public MachineCommon()
		{
			FillMetadata();

			ImGuiDrawFunctions.Add(DrawCheatsUI);


			// cheat system test (Mr. Driller)
			// TODO: remove me
			cheats[0] = new() { Description = "Infinite lives", Address = 0xC983, Value = 5 };
			cheats[1] = new() { Description = "Infinite air", Address = 0xC986, Value = 100 };
		}

		protected abstract void FillMetadata();
		public abstract void UpdateMetadata();

		public abstract void Initialize();

		public virtual void Reset()
		{
			for (var i = 0; i < internalRam.Length; i++) internalRam[i] = 0;

			cartridge.Reset();
			cpu.Reset();
			display.Reset();
			sound.Reset();
			eeprom.Reset();

			currentClockCyclesInFrame = 0;
			totalClockCyclesInFrame = (int)Math.Round(CpuClock / DisplayControllerCommon.VerticalClock);

			ResetRegisters();
		}

		public void ResetRegisters()
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

		public abstract void Shutdown();

		protected void InitializeEepromToDefaults(string username)
		{
			/* Not 100% verified, same caveats as ex. ares */

			var data = ConvertUsernameForEeprom(username);

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
			eeprom.Program(0x7A, 0x00); // (unknown)  -- Swan ID? (see Mama Mitte)
			eeprom.Program(0x7B, 0x00); // (unknown)  -- ""
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

			while (currentClockCyclesInFrame < totalClockCyclesInFrame)
				RunStep();

			currentClockCyclesInFrame -= totalClockCyclesInFrame;

			UpdateMetadata();

			OnEndOfFrame(EventArgs.Empty);
		}

		public abstract void RunStep();

		protected void CheckAndRaiseInterrupts()
		{
			for (var i = 7; i >= 0; i--)
			{
				if (!IsBitSet(intEnable, i) || !IsBitSet(intStatus, i)) continue;
				cpu.RaiseInterrupt(intBase + i);
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

			Metadata["cartridge/id"] = cartridge.Metadata.GameIdString;
			Metadata["cartridge/publisher"] = cartridge.Metadata.PublisherName;
			Metadata["cartridge/orientation"] = cartridge.Metadata.Orientation.ToString().ToLowerInvariant();
			Metadata["cartridge/savetype"] = cartridge.Metadata.IsSramSave ? "sram" : (cartridge.Metadata.IsEepromSave ? "eeprom" : "none");
		}

		public void LoadSaveData(byte[] data)
		{
			if (Metadata.GetValueOrDefault("cartridge/savetype") == "sram")
				cartridge.LoadSram(data);
			else if (Metadata.GetValueOrDefault("cartridge/savetype") == "eeprom")
				cartridge.LoadEeprom(data);
		}

		public byte[] GetInternalEeprom()
		{
			return eeprom.GetContents();
		}

		public byte[] GetSaveData()
		{
			if (Metadata.GetValueOrDefault("cartridge/savetype") == "sram")
				return cartridge.GetSram();
			else if (Metadata.GetValueOrDefault("cartridge/savetype") == "eeprom")
				return cartridge.GetEeprom();

			return Array.Empty<byte>();
		}

		public (int w, int h) GetScreenSize()
		{
			return (DisplayControllerCommon.ScreenWidth, DisplayControllerCommon.ScreenHeight);
		}

		public double GetRefreshRate()
		{
			return DisplayControllerCommon.VerticalClock;
		}

		public Dictionary<string, ushort> GetProcessorStatus()
		{
			return cpu.GetStatus();
		}

		public void BeginTraceLog(string filename)
		{
			cpu.InitializeTraceLogger(filename);
		}

		public void EndTraceLog()
		{
			cpu.CloseTraceLogger();
		}

		// TODO: split out into separate class, or at least source file?
		public void DrawCheatsUI()
		{
			if (!isCheatsUIOpen) return;

			var tableColumnFlags = ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoResize;

			ImGui.SetNextWindowSize(new NumericsVector2(500, 300), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("Cheats", ref isCheatsUIOpen))
			{
				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

				if (ImGui.BeginChild("##list", new NumericsVector2(0f, -45f)))
				{
					if (ImGui.BeginTable("##list-table", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX))
					{
						ImGui.TableSetupScrollFreeze(0, 1);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableSetupColumn("Address", tableColumnFlags);
						ImGui.TableSetupColumn("Value", tableColumnFlags);
						ImGui.TableSetupColumn("Description", tableColumnFlags | ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableHeadersRow();

						for (var i = 0; i < cheats.Length; i++)
						{
							if (cheats[i] == null) break;

							ImGui.TableNextRow();
							ImGui.TableNextColumn();

							ImGui.AlignTextToFramePadding();

							ImGui.Checkbox($"##list-enabled{i}", ref cheats[i].Enabled);
							ImGui.TableNextColumn();

							ImGui.Text($"{cheats[i].Address:X6}");
							ImGui.TableNextColumn();

							ImGui.Text($"{cheats[i].Value:X2}");
							ImGui.TableNextColumn();

							ImGui.Text($"{cheats[i].Description}");
							ImGui.TableNextColumn();

							if (ImGui.Button($"Edit##list-edit{i}")) cheatToEditIdx = i;
							ImGui.SameLine();
							if (ImGui.Button($"Delete##list-delete{i}")) cheatToDeleteIdx = i;
						}
					}
					ImGui.EndTable();
				}
				ImGui.EndChild();

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				if (ImGui.BeginChild("##controls-frame", NumericsVector2.Zero))
				{
					if (ImGui.Button($"Add##add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 4f, 0f))) newCheatToAdd = new();
					ImGui.SameLine();
					ImGui.Dummy(new NumericsVector2(ImGui.GetContentRegionAvail().X / 3f, 0f));
					ImGui.SameLine();
					ImGui.Dummy(new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f));
					ImGui.SameLine();
					if (ImGui.Button($"Close##close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f))) isCheatsUIOpen = false;
				}
				ImGui.EndChild();

				ImGui.PopStyleVar();
			}
			ImGui.End();

			if (newCheatToAdd != null)
			{
				ImGui.OpenPopup("Add Cheat##add-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Add Cheat##add-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGuiHelpers.InputHex("Address##add-address", ref newCheatToAdd.Address, 6, false);
					ImGuiHelpers.InputHex("Value##add-value", ref newCheatToAdd.Value, 2, false);
					ImGui.InputText("Description##add-desc", ref newCheatToAdd.Description, 64);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						var cheatToAddIdx = Array.FindIndex(cheats, x => x == null);
						if (cheatToAddIdx != -1) cheats[cheatToAddIdx] = newCheatToAdd;

						ImGui.CloseCurrentPopup();
						newCheatToAdd = null;
					}
					ImGui.SameLine();
					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						newCheatToAdd = null;
					}

					ImGui.PopStyleVar();
				}
				ImGui.EndPopup();

				if (!popupDummy)
					newCheatToAdd = null;
			}

			if (cheatToDeleteIdx != -1)
			{
				ImGui.OpenPopup("Delete Cheat##delete-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Delete Cheat##delete-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.Text("Do you really want to delete this cheat?");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Yes", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						cheats[cheatToDeleteIdx] = null;
						for (var i = cheatToDeleteIdx; i < cheats.Length - 1; i++)
							cheats[i] = cheats[i + 1];

						ImGui.CloseCurrentPopup();
						cheatToDeleteIdx = -1;
					}
					ImGui.SameLine();
					if (ImGui.Button("No", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						cheatToDeleteIdx = -1;
					}

					ImGui.PopStyleVar();
				}
				ImGui.EndPopup();

				if (!popupDummy)
					cheatToDeleteIdx = -1;
			}

			if (cheatToEditIdx != -1)
			{
				ImGui.OpenPopup("Edit Cheat##edit-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Edit Cheat##edit-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGuiHelpers.InputHex("Address##edit-address", ref cheats[cheatToEditIdx].Address, 6, false);
					ImGuiHelpers.InputHex("Value##edit-value", ref cheats[cheatToEditIdx].Value, 2, false);
					ImGui.InputText("Description##edit-desc", ref cheats[cheatToEditIdx].Description, 64);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						cheatToEditIdx = -1;
					}

					ImGui.PopStyleVar();
				}
				ImGui.EndPopup();

				if (!popupDummy)
					cheatToEditIdx = -1;
			}
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

				/* Handle cheats */
				for (var i = 0; i < cheats.Length; i++)
				{
					if (cheats[i] == null) break;
					if (cheats[i].Address == address && cheats[i].Enabled)
						return cheats[i].Value;
				}

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

		public abstract byte ReadRegister(ushort register);
		public abstract void WriteRegister(ushort register, byte value);

		protected class Cheat
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
