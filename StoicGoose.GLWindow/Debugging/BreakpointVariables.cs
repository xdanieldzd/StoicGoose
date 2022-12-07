using StoicGoose.Core.CPU;
using StoicGoose.Core.Interfaces;

namespace StoicGoose.GLWindow.Debugging
{
	public sealed class BreakpointVariables
	{
		readonly IMachine machine = default;

		public Register16 ax => machine.Cpu.AW;
		public Register16 bx => machine.Cpu.BW;
		public Register16 cx => machine.Cpu.CW;
		public Register16 dx => machine.Cpu.DW;
		public ushort sp => machine.Cpu.SP;
		public ushort bp => machine.Cpu.BP;
		public ushort si => machine.Cpu.IX;
		public ushort di => machine.Cpu.IY;
		public ushort cs => machine.Cpu.PS;
		public ushort ds => machine.Cpu.DS0;
		public ushort ss => machine.Cpu.SS;
		public ushort es => machine.Cpu.DS1;
		public ushort ip => machine.Cpu.PC;

		public BreakpointMemoryArray memoryMap { get; private set; } = default;

		public BreakpointVariables(IMachine machine)
		{
			this.machine = machine;
			memoryMap = new(this.machine);
		}
	}
}
