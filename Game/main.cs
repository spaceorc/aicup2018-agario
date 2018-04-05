using System;
using Game.Protocol;
using Newtonsoft.Json;

namespace Game
{
	public static class main
	{
		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException +=
				(sender, eventArgs) => Logger.Error((Exception)eventArgs.ExceptionObject);
			Logger.Info("Waiting for config...");

			var config = ConsoleProtocol.ReadConfig();
			Logger.Info($"Config: {JsonConvert.SerializeObject(config)}");

			var strategy = new NearestFoodStrategy(config, true);
			while (true)
			{
				Logger.Info("Waiting for data...");
				var data = ConsoleProtocol.ReadTurnInput();
				Logger.Info($"Data: {JsonConvert.SerializeObject(data)}");
				var command = strategy.OnTick(data);
				Logger.Info($"Command: {JsonConvert.SerializeObject(command)}");
				ConsoleProtocol.WriteTurnInput(command);
			}
		}
	}
}