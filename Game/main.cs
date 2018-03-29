using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game
{
	public static class main
	{
		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException +=
				(sender, eventArgs) => Logger.Error((Exception)eventArgs.ExceptionObject);
			Logger.Info("Waiting for config...");

			var config = FixedConsole.ReadLine();
			Logger.Info($"Config: {config}");

			var strategy = new Strategy(JObject.Parse(config));
			while (true)
			{
				Logger.Info("Waiting for data...");
				var data = FixedConsole.ReadLine();
				Logger.Info($"Data: {data}");
				var parsed = JObject.Parse(data);
				var command = strategy.OnTick(parsed).ToString(Formatting.None);
				Logger.Info($"Command: {command}");
				Console.WriteLine(command);
			}
		}
	}
}