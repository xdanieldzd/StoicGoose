using StoicGoose.Core.CPU;
using StoicGoose.Core.Machines;

namespace StoicGoose.Debugging
{
	public sealed class BreakpointVariables
	{
		readonly IMachine machine = default;

		public V30MZ.Register16 ax => machine.Cpu.AX;
		public V30MZ.Register16 bx => machine.Cpu.BX;
		public V30MZ.Register16 cx => machine.Cpu.CX;
		public V30MZ.Register16 dx => machine.Cpu.DX;
		public ushort sp => machine.Cpu.SP;
		public ushort bp => machine.Cpu.BP;
		public ushort si => machine.Cpu.SI;
		public ushort di => machine.Cpu.DI;
		public ushort cs => machine.Cpu.CS;
		public ushort ds => machine.Cpu.DS;
		public ushort ss => machine.Cpu.SS;
		public ushort es => machine.Cpu.ES;
		public ushort ip => machine.Cpu.IP;

		public BreakpointMemoryArray memory { get; private set; } = default;

		public BreakpointVariables(IMachine machine)
		{
			this.machine = machine;

			memory = new(this.machine);
		}
	}
}
