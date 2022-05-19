namespace StoicGoose.GLWindow.Debugging
{
	public class MemoryPatch
	{
		// ... basically, a cheat, but MemoryPatch sounds more like something belonging in the Debugging namespace :P

		public bool IsEnabled { get; set; } = false;
		public string Description { get; set; } = string.Empty;
		public uint Address { get; set; } = 0;
		public MemoryPatchCondition Condition { get; set; } = MemoryPatchCondition.Always;
		public byte CompareValue { get; set; } = 0;
		public byte PatchedValue { get; set; } = 0;
	}

	public enum MemoryPatchCondition { Always = 0, LessThan, LessThanOrEqual, GreaterThanOrEqual, GreaterThan }
}
