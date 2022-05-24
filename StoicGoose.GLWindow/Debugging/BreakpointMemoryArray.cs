using StoicGoose.Core.Interfaces;

namespace StoicGoose.GLWindow.Debugging
{
	public sealed class BreakpointMemoryArray
	{
		readonly IMachine machine = default;

		public byte this[uint addr] => machine.ReadMemory(addr);

		public BreakpointMemoryArray(IMachine machine) => this.machine = machine;
	}
}
