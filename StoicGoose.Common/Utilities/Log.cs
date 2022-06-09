using System;
using System.Collections.Generic;
using System.IO;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

using StoicGoose.Common.Extensions;

namespace StoicGoose.Common.Utilities
{
	public enum LogSeverity { Verbose, Debug, Information, Warning, Error, Fatal }

	public static class Log
	{
		const string defaultTemplate = "{Message}{NewLine}{Exception}";

		readonly static Dictionary<LogSeverity, LogEventLevel> severityToEventLevelMapping = new()
		{
			{ LogSeverity.Verbose, LogEventLevel.Verbose },
			{ LogSeverity.Debug, LogEventLevel.Debug },
			{ LogSeverity.Information, LogEventLevel.Information },
			{ LogSeverity.Warning, LogEventLevel.Warning },
			{ LogSeverity.Error, LogEventLevel.Error },
			{ LogSeverity.Fatal, LogEventLevel.Fatal }
		};

		readonly static Dictionary<LogSeverity, string> logSeverityAnsiColors = new()
		{
			{ LogSeverity.Verbose, Ansi.White },
			{ LogSeverity.Debug, Ansi.Cyan },
			{ LogSeverity.Information, Ansi.Green },
			{ LogSeverity.Warning, Ansi.Yellow },
			{ LogSeverity.Error, Ansi.Magenta },
			{ LogSeverity.Fatal, Ansi.Red }
		};

		static Logger mainLogger = default;
		static Logger fileLogger = default;

		public static string LogPath { get; private set; } = string.Empty;

		public static void Initialize(string logPath)
		{
			if (File.Exists(logPath)) File.Delete(logPath);

			mainLogger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.Console(outputTemplate: defaultTemplate)
				.CreateLogger();

			fileLogger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.File(LogPath = logPath, restrictedToMinimumLevel: LogEventLevel.Verbose)
				.CreateLogger();
		}

		public static void AttachTextWriter(TextWriter writer)
		{
			mainLogger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.Sink(mainLogger)
				.WriteTo.Sink(new TextWriterSink(writer, new MessageTemplateTextFormatter(defaultTemplate)))
				.CreateLogger();
		}

		public static void WriteLine(string message) => Write(LogEventLevel.Information, message);
		public static void WriteFatal(string message) => Write(LogEventLevel.Fatal, message);

		private static void Write(LogEventLevel logEventLevel, string message)
		{
			mainLogger?.Write(logEventLevel, message);
			fileLogger?.Write(logEventLevel, message.RemoveAnsi());
		}

		public static void WriteEvent(LogSeverity severity, object source, string message)
		{
			if (mainLogger == null && fileLogger == null) return;

			var eventLevel = severityToEventLevelMapping.ContainsKey(severity) ? severityToEventLevelMapping[severity] : LogEventLevel.Verbose;
			var logMessage = $"{logSeverityAnsiColors[severity]}[{source?.GetType().Name ?? string.Empty}]{Ansi.Reset}: {message}";
			mainLogger?.Write(eventLevel, logMessage);
			fileLogger?.Write(eventLevel, logMessage.RemoveAnsi());
		}
	}

	class TextWriterSink : ILogEventSink
	{
		readonly TextWriter textWriter = default;
		readonly ITextFormatter textFormatter = default;

		readonly object syncRoot = new();

		public TextWriterSink(TextWriter writer, ITextFormatter formatter)
		{
			textWriter = writer;
			textFormatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		}

		public void Emit(LogEvent logEvent)
		{
			lock (syncRoot)
			{
				textFormatter.Format(logEvent ?? throw new ArgumentNullException(nameof(logEvent)), textWriter);
				textWriter.Flush();
			}
		}
	}
}
