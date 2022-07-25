using System;

using ImGuiNET;

using StoicGoose.GLWindow.Debugging;
using StoicGoose.ImGuiCommon.Windows;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Windows
{
	public class MemoryPatchWindow : WindowBase
	{
		readonly string[] patchConditionSymbols = new string[] { "^", "<", "<=", "=>", ">" };
		readonly string[] patchConditionDescriptive = new string[] { "^ (always)", "< (less than)", "<= (less or equal)", "=> (greater or equal)", "> (greater than)" };
		readonly string[] patchConditionNames = new string[] { "always", "less than", "less or equal", "greater or equal", "greater than" };

		MemoryPatch newPatchToAdd = default;
		int patchToEditIdx = -1, patchToDeleteIdx = -1;

		public MemoryPatchWindow() : base("Memory Patches", new NumericsVector2(700f, 400f), ImGuiCond.FirstUseEver) { }

		protected override void DrawWindow(object userData)
		{
			if (userData is not (MemoryPatch[] patches, bool isRunning)) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				ImGui.BeginDisabled(!isRunning);

				var tableFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX;
				var tableColumnFlags = ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoResize;

				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

				if (ImGui.BeginChild("##list", new NumericsVector2(0f, -45f)))
				{
					if (ImGui.BeginTable("##list-table", 8, tableFlags))
					{
						ImGui.TableSetupScrollFreeze(0, 1);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableSetupColumn("Address", tableColumnFlags);
						ImGui.TableSetupColumn("Condition", tableColumnFlags);
						ImGui.TableSetupColumn("Compare", tableColumnFlags);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableSetupColumn("Patch", tableColumnFlags);
						ImGui.TableSetupColumn("Description", tableColumnFlags | ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableHeadersRow();

						for (var i = 0; i < patches.Length; i++)
						{
							if (patches[i] == null) break;

							ImGui.TableNextRow();
							ImGui.TableNextColumn();

							ImGui.AlignTextToFramePadding();

							ImGui.Checkbox($"##list-enabled{i}", ref patches[i].IsEnabled);
							ImGui.TableNextColumn();

							ImGui.Text($"0x{patches[i].Address:X6}");
							ImGui.TableNextColumn();

							ImGui.Text(patchConditionSymbols[(int)patches[i].Condition]);
							ImGui.TableNextColumn();

							ImGui.Text($"0x{patches[i].CompareValue:X2}");
							ImGui.TableNextColumn();

							ImGui.Text("=");
							ImGui.TableNextColumn();

							ImGui.Text($"0x{patches[i].PatchedValue:X2}");
							ImGui.TableNextColumn();

							ImGui.Text($"{patches[i].Description}");
							ImGui.TableNextColumn();

							if (ImGui.Button($"Edit##list-edit{i}")) patchToEditIdx = i;
							ImGui.SameLine();
							if (ImGui.Button($"Delete##list-delete{i}")) patchToDeleteIdx = i;
						}
						ImGui.EndTable();
					}
					ImGui.EndChild();
				}

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				ImGui.EndDisabled();

				if (ImGui.BeginChild("##controls-frame", NumericsVector2.Zero))
				{
					ImGui.BeginDisabled(!isRunning);

					if (ImGui.Button($"Add##add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 4f, 0f))) newPatchToAdd = new();
					ImGui.SameLine();
					ImGui.Dummy(new NumericsVector2(ImGui.GetContentRegionAvail().X / 3f, 0f));
					ImGui.SameLine();
					ImGui.Dummy(new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f));
					ImGui.SameLine();

					ImGui.EndDisabled();

					if (ImGui.Button($"Close##close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f))) isWindowOpen = false;

					ImGui.EndChild();
				}

				ImGui.PopStyleVar();

				ImGui.End();
			}

			if (newPatchToAdd != null)
			{
				ImGui.OpenPopup("Add Patch##add-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Add Patch##add-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.Checkbox($"Enabled?##add-enabled", ref newPatchToAdd.IsEnabled);
					Helpers.InputHex("Address##add-address", ref newPatchToAdd.Address, 6, false);
					int condition = (int)newPatchToAdd.Condition;
					ImGui.Combo($"Condition##add-condition", ref condition, patchConditionDescriptive, patchConditionDescriptive.Length);
					newPatchToAdd.Condition = (MemoryPatchCondition)condition;
					Helpers.InputHex("Compare Value##add-compvalue", ref newPatchToAdd.CompareValue, 2, false);
					Helpers.InputHex("Patched Value##add-patchvalue", ref newPatchToAdd.PatchedValue, 2, false);
					ImGui.InputText("Description##add-desc", ref newPatchToAdd.Description, 64);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					if (newPatchToAdd.Condition != MemoryPatchCondition.Always)
						ImGui.Text($"If value at 0x{newPatchToAdd.Address:X6} is {patchConditionNames[(int)newPatchToAdd.Condition]} 0x{newPatchToAdd.CompareValue:X2},\npatch to 0x{newPatchToAdd.PatchedValue:X2}.");
					else
						ImGui.Text($"Always patch value at 0x{newPatchToAdd.Address:X6} to 0x{newPatchToAdd.PatchedValue:X2}.");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						var patchToAddIdx = Array.FindIndex(patches, x => x == null);
						if (patchToAddIdx != -1) patches[patchToAddIdx] = newPatchToAdd;

						ImGui.CloseCurrentPopup();
						newPatchToAdd = null;
					}
					ImGui.SameLine();
					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						newPatchToAdd = null;
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					newPatchToAdd = null;
			}

			if (patchToDeleteIdx != -1)
			{
				ImGui.OpenPopup("Delete Patch##delete-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Delete Patch##delete-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.Text("Do you really want to delete this patch?");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Yes", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						patches[patchToDeleteIdx] = null;
						for (var i = patchToDeleteIdx; i < patches.Length - 1; i++)
							patches[i] = patches[i + 1];

						ImGui.CloseCurrentPopup();
						patchToDeleteIdx = -1;
					}
					ImGui.SameLine();
					if (ImGui.Button("No", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						patchToDeleteIdx = -1;
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					patchToDeleteIdx = -1;
			}

			if (patchToEditIdx != -1)
			{
				ImGui.OpenPopup("Edit Patch##edit-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Edit Patch##edit-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.Checkbox($"Enabled?##edit-enabled", ref patches[patchToEditIdx].IsEnabled);
					Helpers.InputHex("Address##edit-address", ref patches[patchToEditIdx].Address, 6, false);
					int condition = (int)patches[patchToEditIdx].Condition;
					ImGui.Combo($"Condition##edit-condition", ref condition, patchConditionDescriptive, patchConditionDescriptive.Length);
					patches[patchToEditIdx].Condition = (MemoryPatchCondition)condition;
					Helpers.InputHex("Compare Value##edit-compvalue", ref patches[patchToEditIdx].CompareValue, 2, false);
					Helpers.InputHex("Patched Value##edit-patchvalue", ref patches[patchToEditIdx].PatchedValue, 2, false);
					ImGui.InputText("Description##edit-desc", ref patches[patchToEditIdx].Description, 64);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					if (patches[patchToEditIdx].Condition != MemoryPatchCondition.Always)
						ImGui.Text($"If value at 0x{patches[patchToEditIdx].Address:X6} is {patchConditionNames[(int)patches[patchToEditIdx].Condition]} 0x{patches[patchToEditIdx].CompareValue:X2},\npatch to 0x{patches[patchToEditIdx].PatchedValue:X2}.");
					else
						ImGui.Text($"Always patch value at 0x{patches[patchToEditIdx].Address:X6} to 0x{patches[patchToEditIdx].PatchedValue:X2}.");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						patchToEditIdx = -1;
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					patchToEditIdx = -1;
			}
		}
	}
}
