using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Handlers
{
	public class FileDialogHandler
	{
		readonly List<FileDialog> fileDialogs = new();

		DriveInfo[] driveInfos = default;
		List<string> driveLabels = default;
		int selectedDriveInfo = -1;

		readonly List<(string label, string[] filter)> filters = new();
		int selectedFilter = -1;

		DirectoryInfo workingDirectory = default;
		List<DirectoryInfo> currentSubDirectories = new();
		List<FileInfo> currentFiles = new();

		readonly List<(string label, bool isDirectory, int index)> currentFileDirList = new();
		int selectedFileDir = -1;

		string selectedFilePath = string.Empty;

		public bool IsAnyDialogOpen => fileDialogs.Any(x => x.IsOpen);

		public FileDialogHandler(params FileDialog[] fileDialogs)
		{
			this.fileDialogs.AddRange(fileDialogs);
		}

		public void AddFileDialog(FileDialog fileDialog)
		{
			fileDialogs.Add(fileDialog);
		}

		public void Draw()
		{
			foreach (var messageBox in fileDialogs.Where(x => x.IsOpen))
				ImGui.OpenPopup(messageBox.Title);

			for (var i = 0; i < fileDialogs.Count; i++)
			{
				if (!fileDialogs[i].IsOpen) continue;

				void selectFile(int idx)
				{
					selectedFileDir = idx;
					if (selectedFileDir == -1) return;

					if (!currentFileDirList[selectedFileDir].isDirectory)
						selectedFilePath = currentFiles[currentFileDirList[selectedFileDir].index].FullName;
				};

				if (driveInfos == default)
				{
					driveInfos = DriveInfo.GetDrives();
					driveLabels = driveInfos.Select(x => $"{x.Name} {(x.IsReady ? $"[{x.VolumeLabel}]" : string.Empty)}").ToList();
				}

				if (filters.Count == 0)
				{
					var filterSplit = fileDialogs[i].Filter.Split('|');
					if ((filterSplit.Length % 2) != 0) throw new Exception("Invalid file filter");

					foreach (var (label, filter) in Enumerable.Range(0, filterSplit.Length / 2).Select(x => (filterSplit[x * 2 + 0], filterSplit[x * 2 + 1].ToLower().Replace("*", string.Empty).Split(';'))))
						filters.Add((label, filter));
				}

				if (selectedDriveInfo == -1)
				{
					var initialRoot = new DirectoryInfo(fileDialogs[i].InitialDirectory).Root;
					selectedDriveInfo = Array.FindIndex(driveInfos, x => x.RootDirectory.FullName == initialRoot.FullName);
				}

				if (selectedFilter == -1)
					selectedFilter = 0;

				if (workingDirectory == default)
				{
					UpdateCurrentFilesAndDirs(new(fileDialogs[i].InitialDirectory));
					selectFile(currentFileDirList.FindIndex(x => x.label == fileDialogs[i].InitialFilename));
				}

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				ImGui.SetNextWindowSize(new(500f, 500f));
				if (ImGui.BeginPopupModal(fileDialogs[i].Title, ref fileDialogs[i].IsOpen, ImGuiWindowFlags.NoResize))
				{
					var windowContentAvailWidth = ImGui.GetContentRegionAvail().X;

					bool readyToOpen = false;

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGui.SetNextItemWidth(windowContentAvailWidth);
					if (ImGui.Combo("##drives", ref selectedDriveInfo, driveLabels.ToArray(), driveLabels.Count))
						UpdateCurrentFilesAndDirs(driveInfos[selectedDriveInfo].RootDirectory);

					ImGui.Dummy(new NumericsVector2(0f, 3f));

					ImGui.Text(workingDirectory.FullName);

					ImGui.Dummy(new NumericsVector2(0f, 3f));

					ImGui.BeginListBox("##filesdirs", new(windowContentAvailWidth, 300f));
					for (var j = -1; j < currentFileDirList.Count; j++)
					{
						/* Check for ex. empty drives */
						if (!workingDirectory.Exists) break;

						var isSelected = selectedFileDir == j;

						if (j == -1)
						{
							/* Add (..) directory up item */
							if (workingDirectory.Parent != null)
							{
								ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00E0FF);
								if (ImGui.Selectable("[..]", isSelected, ImGuiSelectableFlags.AllowDoubleClick) &&
									ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
								{
									UpdateCurrentFilesAndDirs(workingDirectory.Parent);
								}
								ImGui.PopStyleColor();
							}
						}
						else
						{
							var (label, isDir, _) = currentFileDirList[j];

							/* Color dirs yellow-ish */
							if (isDir) ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00E0FF);
							if (ImGui.Selectable(isDir ? $"[{label}]" : label, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
								selectFile(j);
							if (isDir) ImGui.PopStyleColor();

							if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && selectedFileDir != -1)
							{
								if (!currentFileDirList[selectedFileDir].isDirectory)
									readyToOpen = selectedFilePath != string.Empty;
								else
									UpdateCurrentFilesAndDirs(currentSubDirectories[currentFileDirList[selectedFileDir].index]);
							}
						}

						if (isSelected)
						{
							ImGui.SetItemDefaultFocus();
							if (fileDialogs[i].IsFirstOpen)
								ImGui.SetScrollHereY(0.5f);
						}
					}
					ImGui.EndListBox();

					ImGui.Dummy(new NumericsVector2(0f, 3f));

					ImGui.Text(!string.IsNullOrEmpty(selectedFilePath) ? Path.GetFileName(selectedFilePath) : string.Empty);

					ImGui.Dummy(new NumericsVector2(0f, 3f));

					ImGui.SetNextItemWidth(windowContentAvailWidth);
					if (ImGui.Combo("##filters", ref selectedFilter, filters.Select(x => x.label).ToArray(), filters.Count))
						UpdateCurrentFilesAndDirs(workingDirectory);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					var buttonWidth = (windowContentAvailWidth - ImGui.GetStyle().ItemSpacing.X) / 2f;

					if (ImGui.Button("Open", new NumericsVector2(buttonWidth, 0f)) || readyToOpen)
					{
						ImGui.CloseCurrentPopup();

						fileDialogs[i].Callback?.Invoke(ImGuiFileDialogResult.Okay, selectedFilePath);
						fileDialogs[i].IsOpen = false;

						ResetHandlerState();
					}
					ImGui.SameLine();
					if (ImGui.Button("Cancel", new NumericsVector2(buttonWidth, 0f)))
					{
						ImGui.CloseCurrentPopup();

						fileDialogs[i].Callback?.Invoke(ImGuiFileDialogResult.Cancel, string.Empty);
						fileDialogs[i].IsOpen = false;

						ResetHandlerState();
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();

					fileDialogs[i].IsFirstOpen = false;
				}
			}
		}

		private void UpdateCurrentFilesAndDirs(DirectoryInfo directory)
		{
			workingDirectory = directory;

			if (workingDirectory.Exists)
			{
				currentFiles = workingDirectory.GetFiles().Where(x => filters[selectedFilter].filter.Contains(x.Extension.ToLower())).ToList();
				currentSubDirectories = workingDirectory.GetDirectories().ToList();

				currentFileDirList.Clear();
				currentFileDirList.AddRange(currentSubDirectories.Select((x, i) => (x.Name, true, i)));
				currentFileDirList.AddRange(currentFiles.Select((x, i) => (x.Name, false, i)));
			}
			else
			{
				currentFiles.Clear();
				currentSubDirectories.Clear();

				currentFileDirList.Clear();
			}

			selectedFileDir = -1;
			selectedFilePath = string.Empty;
		}

		private void ResetHandlerState()
		{
			selectedDriveInfo = -1;

			filters.Clear();
			selectedFilter = -1;

			workingDirectory = default;
			currentSubDirectories.Clear();
			currentFiles.Clear();

			currentFileDirList.Clear();
			selectedFileDir = -1;

			selectedFilePath = string.Empty;
		}
	}

	public enum ImGuiFileDialogType { Open, Save }
	public enum ImGuiFileDialogResult { None, Cancel, Okay }

	public class FileDialog
	{
		public ImGuiFileDialogType DialogType { get; set; } = ImGuiFileDialogType.Open;
		public string Title { get; set; } = string.Empty;
		public string InitialFilename { get; set; } = string.Empty;
		public string InitialDirectory { get; set; } = string.Empty;
		public string Filter { get; set; } = string.Empty;
		public Action<ImGuiFileDialogResult, string> Callback { get; set; } = null;

		public bool IsOpen = false;
		public bool IsFirstOpen = true;

		public FileDialog(ImGuiFileDialogType dialogType, string title)
		{
			DialogType = dialogType;
			Title = title;
		}
	}
}
