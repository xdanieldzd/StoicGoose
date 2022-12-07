namespace StoicGoose.Core.CPU
{
public class ProgramStatusWord
{
const ushort carryBit = 1 << 0;
bool carry;
public bool Carry { get => carry; set => carry = value; }
const ushort parityBit = 1 << 2;
bool parity;
public bool Parity { get => parity; set => parity = value; }
const ushort auxCarryBit = 1 << 4;
bool auxCarry;
public bool AuxiliaryCarry { get => auxCarry; set => auxCarry = value; }
const ushort zeroBit = 1 << 6;
bool zero;
public bool Zero { get => zero; set => zero = value; }
const ushort signBit = 1 << 7;
bool sign;
public bool Sign { get => sign; set => sign = value; }
const ushort brkBit = 1 << 8;
bool brk;
public bool Break { get => brk; set => brk = value; }
const ushort intEnableBit = 1 << 9;
bool intEnable;
public bool InterruptEnable { get => intEnable; set => intEnable = value; }
const ushort directionBit = 1 << 10;
bool direction;
public bool Direction { get => direction; set => direction = value; }
const ushort overflowBit = 1 << 11;
bool overflow;
public bool Overflow { get => overflow; set => overflow = value; }

public ushort Value
{
get => (ushort)(
(carry ? carryBit : 0) |
(parity ? parityBit : 0) |
(auxCarry ? auxCarryBit : 0) |
(zero ? zeroBit : 0) |
(sign ? signBit : 0) |
(brk ? brkBit : 0) |
(intEnable ? intEnableBit : 0) |
(direction ? directionBit : 0) |
(overflow ? overflowBit : 0) |
0b1111000000000010);
set
{
carry = (value & carryBit) == carryBit;
parity = (value & parityBit) == parityBit;
auxCarry = (value & auxCarryBit) == auxCarryBit;
zero = (value & zeroBit) == zeroBit;
sign = (value & signBit) == signBit;
brk = (value & brkBit) == brkBit;
intEnable = (value & intEnableBit) == intEnableBit;
direction = (value & directionBit) == directionBit;
overflow = (value & overflowBit) == overflowBit;
}
}
public static implicit operator ProgramStatusWord(ushort value) => new() { Value = value };
}
}
