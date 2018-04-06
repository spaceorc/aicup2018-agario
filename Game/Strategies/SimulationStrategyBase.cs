using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public abstract class SimulationStrategyBase : StateStrategyBase
	{
		protected Point globalTarget;

		protected SimulationStrategyBase(Config config) : base(config)
		{
		}

		protected sealed override Direct GetDirect()
		{
			var sim = new Simulator(state);
			PrepareGlobalTarget(sim);
			return GetDirect(sim);
		}

		protected abstract Direct GetDirect(Simulator sim);

		private void PrepareGlobalTarget(Simulator sim)
		{
			var globalTargetReached = false;
			if (globalTarget == null)
				globalTargetReached = true;
			else
			{
				foreach (var frag in sim.players[0])
				{
					if (frag.Distance(globalTarget) < 4 * frag.radius)
					{
						globalTargetReached = true;
						break;
					}
				}
			}

			if (globalTargetReached)
			{
				if (globalTarget == null)
				{
					globalTarget = new Point(
						config.GAME_WIDTH / 10 + random.NextDouble() * config.GAME_WIDTH * 8 / 10,
						config.GAME_HEIGHT / 10 + random.NextDouble() * config.GAME_HEIGHT * 8 / 10);
				}
				else
				{
					var minDiffQDist = (config.GAME_WIDTH * config.GAME_WIDTH + config.GAME_HEIGHT * config.GAME_HEIGHT) * 0.09;
					while (true)
					{
						var nextGlobalTarget = new Point(
							config.GAME_WIDTH / 10 + random.NextDouble() * config.GAME_WIDTH * 8 / 10,
							config.GAME_HEIGHT / 10 + random.NextDouble() * config.GAME_HEIGHT * 8 / 10);
						if (nextGlobalTarget.QDistance(globalTarget) > minDiffQDist)
						{
							globalTarget = nextGlobalTarget;
							break;
						}
					}
				}
			}
		}
	}
}