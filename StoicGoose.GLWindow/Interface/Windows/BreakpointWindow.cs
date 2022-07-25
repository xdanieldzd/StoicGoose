using System;
using System.Linq;
using System.Reflection;
using System.Text;

using ImGuiNET;

using StoicGoose.GLWindow.Debugging;
using StoicGoose.ImGuiCommon.Windows;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Windows
{
	public class BreakpointWindow : WindowBase
	{
		const string invalidBreakpointMsgBoxTitleId = "Error##invalid-bp";
		const int maxExpressionLength = 512;

		string expressionFormatHelpText = string.Empty;

		Breakpoint newBreakpointToAdd = default;
		int breakpointToEditIdx = -1, breakpointToDeleteIdx = -1;
		string newBreakpointExpression = string.Empty;
		bool applyBreakpointEdit = false;

		public BreakpointWindow() : base("Breakpoints", new NumericsVector2(800f, 300f), ImGuiCond.FirstUseEver) { }

		protected override void InitializeWindow(object userData)
		{
			var cpuRegisters = string.Join(", ", typeof(BreakpointVariables).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).Where(x => x.SetMethod == null).Select(x => x.Name));
			var arrays = string.Join(", ", typeof(BreakpointVariables).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty).Where(x => x.SetMethod?.IsPrivate == true).Select(x => x.Name + "[]"));

			var helpTextBuilder = new StringBuilder();
			helpTextBuilder.AppendLine("Type a boolean expression here. C#-style logical operators are supported. Hexadecimal (0x-prefix), binary (0b-prefix) and decimal numbers are supported. Changing evaluation order via parentheses ( and ) is supported.");
			helpTextBuilder.AppendLine();
			helpTextBuilder.AppendLine("Supported CPU registers:");
			helpTextBuilder.AppendLine($" {cpuRegisters}");
			helpTextBuilder.AppendLine();
			helpTextBuilder.AppendLine("Supported arrays:");
			helpTextBuilder.AppendLine($" {arrays}");
			helpTextBuilder.AppendLine();
			helpTextBuilder.AppendLine("Example:");
			helpTextBuilder.AppendLine($" {nameof(BreakpointVariables.cs)} == 0xD100 && {nameof(BreakpointVariables.ip)} == 0x0218 && {nameof(BreakpointVariables.memoryMap)}[0xBEEF] == 69");

			expressionFormatHelpText = helpTextBuilder.ToString();
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not (Breakpoint[] breakpoints, bool isRunning)) return;

			static void handleInvalidBreakpointMessageBox()
			{
				Helpers.ProcessMessageBox("The entered expression is invalid.", invalidBreakpointMsgBoxTitleId, "Okay");
			}

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				ImGui.BeginDisabled(!isRunning);

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

				ImGui.EndDisabled();

				if (ImGui.BeginChild("##controls-frame", NumericsVector2.Zero))
				{
					ImGui.BeginDisabled(!isRunning);

					if (ImGui.Button($"Add##add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 4f, 0f))) newBreakpointToAdd = new();
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
					ImGui.InputText("Expression##add-desc", ref newBreakpointToAdd.Expression, maxExpressionLength, ImGuiInputTextFlags.EnterReturnsTrue);
					ImGui.SameLine();
					Helpers.HelpMarker(expressionFormatHelpText);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					if (ImGui.Button("Add", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						if (!newBreakpointToAdd.UpdateDelegate())
							Helpers.OpenMessageBox(invalidBreakpointMsgBoxTitleId);
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
					if (string.IsNullOrEmpty(newBreakpointExpression))
						newBreakpointExpression = breakpoints[breakpointToEditIdx].Expression;

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.SetNextItemWidth(400f);
					if (ImGui.InputText("Expression##edit-desc", ref newBreakpointExpression, maxExpressionLength, ImGuiInputTextFlags.EnterReturnsTrue))
						applyBreakpointEdit = true;
					ImGui.SameLine();
					Helpers.HelpMarker(expressionFormatHelpText);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Apply", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
						applyBreakpointEdit = true;
					ImGui.SameLine();
					if (ImGui.Button("Close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						breakpointToEditIdx = -1;
						newBreakpointExpression = string.Empty;
						applyBreakpointEdit = false;
					}

					ImGui.PopStyleVar();

					if (applyBreakpointEdit && !string.IsNullOrEmpty(newBreakpointExpression))
					{
						var oldExpression = breakpoints[breakpointToEditIdx].Expression;
						breakpoints[breakpointToEditIdx].Expression = newBreakpointExpression;

						if (!breakpoints[breakpointToEditIdx].UpdateDelegate())
						{
							breakpoints[breakpointToEditIdx].Expression = oldExpression;
							Helpers.OpenMessageBox(invalidBreakpointMsgBoxTitleId);

							applyBreakpointEdit = false;
						}
						else
						{
							ImGui.CloseCurrentPopup();
							breakpointToEditIdx = -1;
							newBreakpointExpression = string.Empty;
							applyBreakpointEdit = false;
						}
					}

					handleInvalidBreakpointMessageBox();

					ImGui.EndPopup();
				}

				if (!popupDummy)
				{
					breakpointToEditIdx = -1;
					newBreakpointExpression = string.Empty;
					applyBreakpointEdit = false;
				}
			}
		}
	}
}
