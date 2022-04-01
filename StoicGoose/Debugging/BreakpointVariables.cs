using StoicGoose.Emulation.CPU;
using StoicGoose.Emulation.Machines;

namespace StoicGoose.Debugging
{
	public sealed class BreakpointVariables
	{
		readonly IMachine machine = default;

		public V30MZ.Register16 ax => machine.Cpu.Registers.AX;
		public V30MZ.Register16 bx => machine.Cpu.Registers.BX;
		public V30MZ.Register16 cx => machine.Cpu.Registers.CX;
		public V30MZ.Register16 dx => machine.Cpu.Registers.DX;
		public ushort sp => machine.Cpu.Registers.SP;
		public ushort bp => machine.Cpu.Registers.BP;
		public ushort si => machine.Cpu.Registers.SI;
		public ushort di => machine.Cpu.Registers.DI;
		public ushort cs => machine.Cpu.Registers.CS;
		public ushort ds => machine.Cpu.Registers.DS;
		public ushort ss => machine.Cpu.Registers.SS;
		public ushort es => machine.Cpu.Registers.ES;
		public ushort ip => machine.Cpu.Registers.IP;

		public BreakpointMemoryArray memory { get; private set; } = default;

		public BreakpointVariables(IMachine machine)
		{
			this.machine = machine;

			memory = new(this.machine);
		}
	}
}
