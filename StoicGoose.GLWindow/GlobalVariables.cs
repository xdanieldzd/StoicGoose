using System.Collections.Generic;
using System.Reflection;

namespace StoicGoose
{
	public static class GlobalVariables
	{
		public static readonly bool IsAuthorsMachine = System.Environment.MachineName == "KAMIKO";
#if DEBUG
		public static readonly bool IsDebugBuild = true;
#else
		public static readonly bool IsDebugBuild = false;
#endif
		public static string[] Dump()
		{
			var vars = new List<string>();
			foreach (var fieldInfo in typeof(GlobalVariables).GetFields(BindingFlags.Static | BindingFlags.Public))
				vars.Add($"{fieldInfo.Name} == {fieldInfo.GetValue(null)}");
			return vars.ToArray();
		}
	}
}
