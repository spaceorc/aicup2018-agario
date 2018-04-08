using System;
using System.Linq;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public abstract class SimulationStrategyBase : StateStrategyBase
	{
		private const int GLOBAL_TARGETS_COUNT = 20;
		private const int GLOBAL_TARGETS_LAST_AWAY_COUNT = 2;
		protected readonly Point[] globalTargets;
		protected int globalTargetIndex;

		protected SimulationStrategyBase(Config config) : base(config)
		{
			globalTargets = new Point[GLOBAL_TARGETS_COUNT];
			for (int i = 0; i < GLOBAL_TARGETS_COUNT; i++)
				globalTargets[i] = GenerateGlobalTarget(i);
		}

		private Point GenerateGlobalTarget(int index)
		{
			var minDiffDist = Math.Sqrt(config.GAME_WIDTH * config.GAME_WIDTH + config.GAME_HEIGHT * config.GAME_HEIGHT) * 0.25;
			var awayPoints = Enumerable.Range(index + GLOBAL_TARGETS_COUNT - GLOBAL_TARGETS_LAST_AWAY_COUNT, GLOBAL_TARGETS_LAST_AWAY_COUNT * 2 + 1)
				.Select(ai => ai % GLOBAL_TARGETS_COUNT)
				.Where(ai => ai != index)
				.Select(ai => globalTargets[ai])
				.Where(p => p != null)
				.ToList();
			while (true)
			{
				var nextGlobalTarget = new Point(
					config.GAME_WIDTH / 10 + random.NextDouble() * config.GAME_WIDTH * 8 / 10,
					config.GAME_HEIGHT / 10 + random.NextDouble() * config.GAME_HEIGHT * 8 / 10);
				if (awayPoints.All(p => p.Distance(nextGlobalTarget) > minDiffDist))
					return nextGlobalTarget;
			}
		}

		protected sealed override Direct GetDirect()
		{
			var sim = new Simulator(state, globalTargets, new[] { globalTargetIndex, 0, 0, 0 });
			PrepareGlobalTarget(sim);
			return GetDirect(sim);
		}

		protected abstract Direct GetDirect(Simulator sim);

		private void PrepareGlobalTarget(Simulator sim)
		{
			foreach (var frag in sim.players[0])
			{
				if (frag.Distance(globalTargets[globalTargetIndex]) < 4 * frag.radius)
				{
					globalTargetIndex = (globalTargetIndex + 1) % globalTargets.Length;
					break;
				}
			}
		}
	}
}