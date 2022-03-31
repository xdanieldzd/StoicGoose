using System;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiDisplayStatusWindow : ImGuiComponentRegisterWindow
	{
		public ImGuiDisplayStatusWindow(string title, Type displayControllerType) : base(title, displayControllerType) { }
	}
}
