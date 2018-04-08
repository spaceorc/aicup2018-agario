using System;
using Game.Helpers;
using Game.Protocol;
using Game.Strategies;
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
			Logger.Info($"Config: {config.ToJson()}");

			var strategy = StrategiesRegistry.Create(Settings.DefaultStrategy, config);
			while (true)
			{
				Logger.Info("Waiting for data...");
				var data = ConsoleProtocol.ReadTurnInput();
				Logger.Info($"Data: {data.ToJson()}");
				var command = strategy.OnTick(data);
				Logger.Info($"Command: {command.ToJson()}");
				ConsoleProtocol.WriteTurnInput(command);
			}
		}
	}
}