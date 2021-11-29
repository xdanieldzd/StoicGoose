﻿namespace StoicGoose
{
	public static class GlobalVariables
	{
		public static readonly bool IsAuthorsMachine = System.Environment.MachineName == "KAMIKO";
#if DEBUG
		public static readonly bool IsDebugBuild = true;
#else
		public static readonly bool IsDebugBuild = false;
#endif
		public static readonly bool EnableLocalDebugIO = IsAuthorsMachine;

		public static readonly bool EnableConsoleOutput = IsAuthorsMachine;
		public static readonly bool EnableOpenGLDebug = false;

		public static readonly bool EnableDebugSoundRecording = false;
		public static readonly bool EnableAutostartLastRom = true;

		public static readonly bool EnableSkipBootstrapIfFound = false;
		public static readonly bool EnableKindaSlowCPULogger = false;

		public static readonly bool EnableRenderSCR1DebugColors = false;
		public static readonly bool EnableRenderSCR2DebugColors = false;
		public static readonly bool EnableRenderSPRDebugColors = false;

		public static readonly bool EnableDebugNewUIStuffs = true;
	}
}
