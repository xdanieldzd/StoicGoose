using System;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;
using Cheat = StoicGoose.Emulation.Machines.MachineCommon.Cheat;

namespace StoicGoose.Interface
{
	public class ImGuiCheatWindow : ImGuiWindowBase
	{
		Cheat newCheatToAdd = default;
		int cheatToEditIdx = -1, cheatToDeleteIdx = -1;

		public ImGuiCheatWindow() : base("Cheats", new NumericsVector2(500f, 300f), ImGuiCond.FirstUseEver) { }

		protected override void DrawWindow(params object[] args)
		{
			if (args.Length != 1 || args[0] is not Cheat[] cheats) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				var tableColumnFlags = ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoResize;

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
						ImGui.EndTable();
					}
					ImGui.EndChild();
				}

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
					if (ImGui.Button($"Close##close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f))) isWindowOpen = false;

					ImGui.EndChild();
				}

				ImGui.PopStyleVar();

				ImGui.End();
			}

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

					ImGui.EndPopup();
				}

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

					ImGui.EndPopup();
				}

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

					ImGui.EndPopup();
				}

				if (!popupDummy)
					cheatToEditIdx = -1;
			}
		}
	}
}
