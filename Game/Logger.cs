using System;
using System.Diagnostics;
using System.IO;

namespace Game
{
	public static class Logger
	{
		public enum Level
		{
			Info,
			Error
		}

		private static readonly string logFile;

		static Logger()
		{
#if LOGGING
			logFile = Path.Combine(FileHelper.PatchDirectoryName("logs"), $"log{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
#endif
		}

		[Conditional("LOGGING")]
		public static void Log(Level level, string msg)
		{
			using (var fileStream = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
			using (var w = new StreamWriter(fileStream))
				w.WriteLine($"{DateTime.Now:s} {level.ToString().ToUpper()} {msg}");
		}

		[Conditional("LOGGING")]
		public static void Info(string msg)
		{
			Log(Level.Info, msg);
		}

		[Conditional("LOGGING")]
		public static void Error(string msg)
		{
			Log(Level.Error, msg);
		}

		[Conditional("LOGGING")]
		public static void Error(Exception exception, string msg)
		{
			Error($"{msg.TrimEnd('.')}. {exception}");
		}

		[Conditional("LOGGING")]
		public static void Error(Exception exception)
		{
			Error(exception.ToString());
		}
	}
}