namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		internal void DBNZ()
		{
			Wait(1);
			var offset = (sbyte)Fetch8();
			if (--cw.Word != 0)
			{
				Wait(3);
				pc = (ushort)(pc + offset);
			}
		}

		internal void DBNZE()
		{
			Wait(2);
			var offset = (sbyte)Fetch8();
			if (--cw.Word != 0 && psw.Zero)
			{
				Wait(3);
				pc = (ushort)(pc + offset);
			}
		}

		internal void DBNZNE()
		{
			Wait(2);
			var offset = (sbyte)Fetch8();
			if (--cw.Word != 0 && !psw.Zero)
			{
				Wait(3);
				pc = (ushort)(pc + offset);
			}
		}
	}
}
