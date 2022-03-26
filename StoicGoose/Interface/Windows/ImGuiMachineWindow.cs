using StoicGoose.Emulation.Machines;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiMachineWindow<T> : ImGuiComponentRegisterWindow<T> where T : MachineCommon
	{
		public ImGuiMachineWindow() : base("Machine Status") { }
	}
}
