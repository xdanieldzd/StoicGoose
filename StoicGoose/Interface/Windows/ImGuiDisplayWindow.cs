using StoicGoose.Emulation.Display;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiDisplayWindow<T> : ImGuiComponentRegisterWindow<T> where T : DisplayControllerCommon
	{
		public ImGuiDisplayWindow() : base("Display Controller Status") { }
	}
}
