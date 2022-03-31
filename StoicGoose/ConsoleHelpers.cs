using System;
using System.Collections.Generic;

namespace StoicGoose
{
	public enum ConsoleLogSeverity { Success, Information, Warning, Error }

	public static class ConsoleHelpers
	{
		readonly static Dictionary<ConsoleLogSeverity, string> logSeverityAnsiColors = new()
		{
			{ ConsoleLogSeverity.Success, Ansi.Green },
			{ ConsoleLogSeverity.Information, Ansi.Cyan },
			{ ConsoleLogSeverity.Warning, Ansi.Yellow },
			{ ConsoleLogSeverity.Error, Ansi.Red }
		};

		public static void WriteLog(ConsoleLogSeverity severity, object source, string message)
		{
			Console.WriteLine($"{logSeverityAnsiColors[severity]}[{source.GetType().Name}]{Ansi.Reset}: {message}");
		}
	}
}
