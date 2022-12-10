namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		//BR
		//BRK
		//BRKV
		//CALL

		internal void CHKIND()
		{
			var lo = GetMemory16(0);
			var hi = GetMemory16(2);
			var reg = GetRegister16();
			if (reg < lo || reg > hi) Interrupt(5);
		}

		//DISPOSE
		//HALT
		//NOT1
		//PREPARE
		//RET
		//RETI
	}
}
