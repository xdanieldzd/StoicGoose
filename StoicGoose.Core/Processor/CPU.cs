using StoicGoose.Core.Interfaces;

namespace StoicGoose.Core.Processor
{
	public class CPU : V30MZ
	{
		readonly IMachine machine = default;

		public CPU(IMachine machine)
		{
			this.machine = machine;
		}

		protected override byte ReadMemory(uint address) => machine.ReadMemory(address);
		protected override void WriteMemory(uint address, byte value) => machine.WriteMemory(address, value);
		protected override byte ReadPort(ushort address) => machine.ReadPort(address);
		protected override void WritePort(ushort address, byte value) => machine.WritePort(address, value);
	}
}
