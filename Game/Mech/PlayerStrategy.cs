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

		public PlayerStrategy(Config config, Func<Config, IStrategy> strategyFactory)
		{
			this.config = config;
			strategy = strategyFactory(config);
		}

		public Direct TickEvent(List<Player> fragments, List<Circle> visibles)
		{
			var turnInput = new TurnInput
			{
				Mine = fragments.Select(x => x.ToMineData()).ToArray(),
				Objects = visibles.Select(x => x.ToObjectData()).ToArray(),
			};
			var turnOutput = strategy.OnTick(turnInput);
			return new Direct(turnOutput.X, turnOutput.Y, config, turnOutput.Split, turnOutput.Eject);
		}

		public override string ToString()
		{
			return strategy.ToString();
		}
	}
}