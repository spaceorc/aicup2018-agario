using System;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public abstract class StateStrategyBase : IStrategy
	{
		public readonly Config config;
		public readonly SimState state;
		public readonly Random random;

		protected StateStrategyBase(Config config)
		{
			this.config = config;
			state = new SimState(config);
			random = new Random();
		}

		public TurnOutput OnTick(TurnInput turnInput)
		{
			state.Apply(turnInput);
			var direct = GetDirect();
			return direct.ToOutput();
		}

		protected abstract Direct GetDirect();
	}
}