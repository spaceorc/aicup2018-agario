// todo fix simple strategy to account eating mindist to enemy
using System;
using Game.Helpers;
using Game.Protocol;
using Game.Sim.Types;
using Game.Strategies;

namespace Game
{
	public static class main
	{
		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException +=
				(sender, eventArgs) => Logger.Error((Exception)eventArgs.ExceptionObject);

			// check all sizes
			new FastEjection();
			new FastMovingPoint();
			new FastDirect();
			new FastFragment();
			new FastPoint();
			new FastVirus();

			Logger.Info("Waiting for config...");

			var config = ConsoleProtocol.ReadConfig();
			Logger.Info($"Config: {config.ToJson()}");

			var strategy = StrategiesRegistry.Create(Settings.DefaultStrategy, config);
			var timeManager = new TimeManager(config);
			while (true)
			{
				Logger.Info("Waiting for data...");
				var data = ConsoleProtocol.ReadTurnInput();
				timeManager.TickStarted();
				Logger.Info($"Data: {data.ToJson()}");
				var command = strategy.OnTick(data, timeManager);
				Logger.Info($"Command: {command.ToJson()}");
				timeManager.TickFinished();
				ConsoleProtocol.WriteTurnInput(command);
			}
		}
	}
}