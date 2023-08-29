namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		internal ushort POP()
		{
			var value = ReadMemory16(ss, sp);
			sp += 2;
			return value;
		}

		internal void PUSH(ushort value)
		{
			sp -= 2;
			WriteMemory16(ss, sp, value);
		}
	}
}
