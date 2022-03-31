using System;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiMachineStatusWindow : ImGuiComponentRegisterWindow
	{
		public ImGuiMachineStatusWindow(string title, Type machineType) : base(title, machineType) { }
	}
}
