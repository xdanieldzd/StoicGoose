﻿using System.Collections.Generic;
using System.Reflection;

namespace StoicGoose.WinForms
{
	public static class GlobalVariables
	{
		public static readonly bool IsAuthorsMachine = System.Environment.MachineName == "RYO-RYZEN" || System.Environment.MachineName == "NADESHIKO-CORE";
#if DEBUG
		public static readonly bool IsDebugBuild = true;
#else
		public static readonly bool IsDebugBuild = false;
#endif
		public static readonly bool EnableLocalDebugIO = IsAuthorsMachine;

		public static readonly bool EnableSuperVerbosity = false;
		public static readonly bool EnableOpenGLDebug = false;

		public static readonly bool EnableSkipBootstrapIfFound = false;

		public static string[] Dump()
		{
			var vars = new List<string>();
			foreach (var fieldInfo in typeof(GlobalVariables).GetFields(BindingFlags.Static | BindingFlags.Public))
				vars.Add($"{fieldInfo.Name} == {fieldInfo.GetValue(null)}");
			return vars.ToArray();
		}
	}
}
