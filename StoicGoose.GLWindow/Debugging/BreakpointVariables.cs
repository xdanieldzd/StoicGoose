using StoicGoose.Core.CPU;
using StoicGoose.Core.Interfaces;

namespace StoicGoose.GLWindow.Debugging
{
    public sealed class BreakpointVariables
    {
        readonly IMachine machine = default;

        public V30MZ.Register16 Ax => machine.Cpu.AX;
        public V30MZ.Register16 Bx => machine.Cpu.BX;
        public V30MZ.Register16 Cx => machine.Cpu.CX;
        public V30MZ.Register16 Dx => machine.Cpu.DX;
        public ushort Sp => machine.Cpu.SP;
        public ushort Bp => machine.Cpu.BP;
        public ushort Si => machine.Cpu.SI;
        public ushort Di => machine.Cpu.DI;
        public ushort Cs => machine.Cpu.CS;
        public ushort Ds => machine.Cpu.DS;
        public ushort Ss => machine.Cpu.SS;
        public ushort Es => machine.Cpu.ES;
        public ushort Ip => machine.Cpu.IP;

        public BreakpointMemoryArray MemoryMap { get; private set; } = default;

        public BreakpointVariables(IMachine machine)
        {
            this.machine = machine;
            MemoryMap = new(this.machine);
        }
    }
}
