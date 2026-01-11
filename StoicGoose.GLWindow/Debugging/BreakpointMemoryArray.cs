using StoicGoose.Core.Interfaces;

namespace StoicGoose.GLWindow.Debugging
{
    public sealed class BreakpointMemoryArray(IMachine machine)
    {
        readonly IMachine machine = machine;

        public byte this[uint addr] => machine.ReadMemory(addr);
    }
}
