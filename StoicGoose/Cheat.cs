namespace StoicGoose
{
	public class Cheat
	{
		public bool IsEnabled { get; set; } = false;
		public string Description { get; set; } = string.Empty;
		public uint Address { get; set; } = 0;
		public CheatCondition Condition { get; set; } = CheatCondition.Always;
		public byte CompareValue { get; set; } = 0;
		public byte PatchedValue { get; set; } = 0;
	}

	public enum CheatCondition : int { Always = 0, LessThan, LessThanOrEqual, GreaterThanOrEqual, GreaterThan }
}
