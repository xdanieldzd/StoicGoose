namespace StoicGoose.Core.Interfaces
{
	interface IPortAccessComponent : IComponent
	{
		byte ReadPort(ushort port);
		void WritePort(ushort port, byte value);
	}
}
