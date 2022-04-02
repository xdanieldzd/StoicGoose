using System;

using ImGuiNET;

using StoicGoose.Debugging;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiBreakpointWindow : ImGuiWindowBase
	{
		const string expressionFormatHelpText = "TODO: Help text goes here!";
		const string invalidBreakpointMsgBoxTitleId = "Error##invalid-bp";

		Breakpoint newBreakpointToAdd = default;
		int breakpointToEditIdx = -1, breakpointToDeleteIdx = -1;

		public ImGuiBreakpointWindow() : base("Breakpoints", new NumericsVector2(800f, 300f), ImGuiCond.FirstUseEver) { }

		protected override void DrawWindow(object userData)
		{
			if (userData is not Breakpoint[] breakpoints) return;

			static void handleInvalidBreakpointMessageBox()
			{
				ImGuiHelpers.ProcessMessageBox("The entered expression is invalid.", invalidBreakpointMsgBoxTitleId, "Okay");
			}

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				var tableFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX;
				var tableColumnFlags = ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoResize;

				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

				if (ImGui.BeginChild("##list", new NumericsVector2(0f, -45f)))
				{
					if (ImGui.BeginTable("##list-table", 3, tableFlags))
					{
						ImGui.TableSetupScrollFreeze(0, 1);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableSetupColumn("Expression", tableColumnFlags | ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn(string.Empty, tableColumnFlags);
						ImGui.TableHeadersRow();

						for (var i = 0; i < breakpoints.Length; i++)
						{
							if (breakpoints[i] == null) break;

							ImGui.TableNextRow();
							ImGui.TableNextColumn();

							ImGui.AlignTextToFramePadding();

							ImGui.Checkbox($"##list-enabled{i}", ref breakpoints[i].Enabled);
							ImGui.TableNextColumn();

							ImGui.Text($"{breakpoints[i].Expression}");
							ImGui.TableNextColumn();

							if (ImGui.Button($"Edit##list-edit{i}")) breakpointToEditIdx = i;
							ImGui.SameLine();
							if (ImGui.Button($"Delete##list-delete{i}")) breakpointToDeleteIdx = i;
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
					if (ImGui.Button($"Add##add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 4f, 0f))) newBreakpointToAdd = new();
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

			if (newBreakpointToAdd != null)
			{
				ImGui.OpenPopup("Add Breakpoint##add-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Add Breakpoint##add-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.SetNextItemWidth(400f);
					ImGui.InputText("Expression##add-desc", ref newBreakpointToAdd.Expression, 512, ImGuiInputTextFlags.EnterReturnsTrue);
					ImGui.SameLine();
					ImGuiHelpers.HelpMarker(expressionFormatHelpText);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					if (ImGui.Button("Add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						if (!newBreakpointToAdd.UpdateDelegate())
							ImGuiHelpers.OpenMessageBox(invalidBreakpointMsgBoxTitleId);
						else
						{
							var breakpointToAddIdx = Array.FindIndex(breakpoints, x => x == null);
							if (breakpointToAddIdx != -1) breakpoints[breakpointToAddIdx] = newBreakpointToAdd;

							ImGui.CloseCurrentPopup();
							newBreakpointToAdd = null;
						}
					}
					ImGui.SameLine();
					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						newBreakpointToAdd = null;
					}

					ImGui.PopStyleVar();

					handleInvalidBreakpointMessageBox();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					newBreakpointToAdd = null;
			}

			if (breakpointToDeleteIdx != -1)
			{
				ImGui.OpenPopup("Delete Breakpoint##delete-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Delete Breakpoint##delete-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.Text("Do you really want to delete this breakpoint?");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					if (ImGui.Button("Yes", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						breakpoints[breakpointToDeleteIdx] = null;
						for (var i = breakpointToDeleteIdx; i < breakpoints.Length - 1; i++)
							breakpoints[i] = breakpoints[i + 1];

						ImGui.CloseCurrentPopup();
						breakpointToDeleteIdx = -1;
					}
					ImGui.SameLine();
					if (ImGui.Button("No", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						breakpointToDeleteIdx = -1;
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					breakpointToDeleteIdx = -1;
			}

			if (breakpointToEditIdx != -1)
			{
				ImGui.OpenPopup("Edit Breakpoint##edit-popup");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal("Edit Breakpoint##edit-popup", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.SetNextItemWidth(400f);
					if (ImGui.InputText("Expression##edit-desc", ref breakpoints[breakpointToEditIdx].Expression, 512, ImGuiInputTextFlags.EnterReturnsTrue))
						if (!breakpoints[breakpointToEditIdx].UpdateDelegate())
							ImGuiHelpers.OpenMessageBox(invalidBreakpointMsgBoxTitleId);
					ImGui.SameLine();
					ImGuiHelpers.HelpMarker(expressionFormatHelpText);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						breakpointToEditIdx = -1;
					}

					ImGui.PopStyleVar();

					handleInvalidBreakpointMessageBox();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					breakpointToEditIdx = -1;
			}
		}
	}
}
