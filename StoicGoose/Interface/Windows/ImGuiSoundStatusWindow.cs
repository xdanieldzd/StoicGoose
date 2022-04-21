using System;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiSoundStatusWindow : ImGuiComponentRegisterWindow
	{
		public ImGuiSoundStatusWindow(string title, Type soundControllerType) : base(title, soundControllerType) { }
	}
}
