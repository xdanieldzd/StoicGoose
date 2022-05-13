namespace StoicGoose.Core.Interfaces
{
	interface IMemoryAccessComponent : IComponent
	{
		byte ReadMemory(uint address);
		void WriteMemory(uint address, byte value);
	}
}
