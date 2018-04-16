using System;
using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace Game.Mech
{
	public class PlayerStrategy
	{
		private readonly Config config;
		private readonly IStrategy strategy;
		private readonly TimeManager timeManager;

		public PlayerStrategy(Config config, Func<Config, IStrategy> strategyFactory)
		{
			this.config = config;
			strategy = strategyFactory(config);
			timeManager = new TimeManager(config);
		}

		public Direct TickEvent(List<Player> fragments, List<Circle> visibles)
		{
			if (timeManager.IsExpiredGlobal)
				return null;
			var turnInput = new TurnInput
			{
				Mine = fragments.Select(x => x.ToMineData()).ToArray(),
				Objects = visibles.Select(x => x.ToObjectData()).ToArray(),
			};
			timeManager.TickStarted();
			var turnOutput = strategy.OnTick(turnInput, timeManager);
			timeManager.TickFinished();
			if (timeManager.IsExpiredGlobal)
			{
				Logger.Info($"{ToString()}: global timeout is expired - strategy is disconnected");
				return null;
			}
			return new Direct(turnOutput.X, turnOutput.Y, config, turnOutput.Split, turnOutput.Eject);
		}

		public override string ToString()
		{
			return strategy.ToString();
		}
	}
}