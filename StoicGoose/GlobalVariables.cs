namespace StoicGoose
{
	public static class GlobalVariables
	{
#if DEBUG
		public static readonly bool IsDebugBuild = true;
#else
		public static readonly bool IsDebugBuild = false;
#endif
		public static readonly bool EnableConsoleOutput = true;
		public static readonly bool EnableOpenGLDebug = false;

		public static readonly bool EnableDebugSoundRecording = false;
		public static readonly bool EnableAutostartLastRom = false;

		public static readonly bool EnableSkipBootstrapIfFound = false;
		public static readonly bool EnableSuperSlowCPULogger = false;

		public static readonly bool EnableRenderSCR1DebugColors = false;
		public static readonly bool EnableRenderSCR2DebugColors = false;
		public static readonly bool EnableRenderSPRDebugColors = false;
	}
}
