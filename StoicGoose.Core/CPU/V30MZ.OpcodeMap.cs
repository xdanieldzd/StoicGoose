using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		Action[] instructions = default;

		private void GenerateInstructionHandlers()
		{
			instructions = new Action[256];

			instructions[0x00] = () => { Wait(1); modRM = Fetch8(); SetMemory8(ADD(GetMemory8(), GetRegister8())); };
			instructions[0x01] = () => { Wait(1); modRM = Fetch8(); SetMemory16(ADD(GetMemory16(), GetRegister16())); };
			instructions[0x02] = () => { Wait(1); modRM = Fetch8(); SetRegister8(ADD(GetRegister8(), GetMemory8())); };
			instructions[0x03] = () => { Wait(1); modRM = Fetch8(); SetRegister16(ADD(GetRegister16(), GetMemory16())); };

			instructions[0x26] = () => { EnqueuePrefix(); };
			instructions[0x2E] = () => { EnqueuePrefix(); };
			instructions[0x36] = () => { EnqueuePrefix(); };
			instructions[0x3E] = () => { EnqueuePrefix(); };

			instructions[0xEA] = () => { Wait(6); var pc = Fetch16(); var ps = Fetch16(); this.pc = pc; this.ps = ps; Flush(); };

			instructions[0xF0] = () => { EnqueuePrefix(); };

			instructions[0xF2] = () => { Wait(4); EnqueuePrefix(); };
			instructions[0xF3] = () => { Wait(4); EnqueuePrefix(); };
		}
	}
}
