namespace StoicGoose.GLWindow.Debugging
{
    public class MemoryPatch
    {
        // ... basically, a cheat, but MemoryPatch sounds more like something belonging in the Debugging namespace :P

        public bool IsEnabled = false;
        public string Description = string.Empty;
        public uint Address = 0;
        public MemoryPatchCondition Condition = MemoryPatchCondition.Always;
        public byte CompareValue = 0;
        public byte PatchedValue = 0;
    }

    public enum MemoryPatchCondition : int { Always = 0, LessThan, LessThanOrEqual, GreaterThanOrEqual, GreaterThan }
}
