﻿using System;
using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Sim.Fast;
using Game.Types;

namespace Game.Strategies
{
	public unsafe class FastAiStrategy : StateStrategyBase
	{
		private readonly IFastAi ai;
		private const int CHECKPOINTS_COUNT = FastPoint.List.capacity;
		private const int CHECKPOINTS_LAST_AWAY_COUNT = 2;
		private FastGlobalState global;
		private int nextCheckpoint;

		public FastAiStrategy(Config config, IFastAi ai) : base(config)
		{
			this.ai = ai;
			for (int i = 0; i < CHECKPOINTS_COUNT; i++)
				global.checkpoints.Add(GenerateCheckPoint(i));
		}

		public static void Register()
		{
			StrategiesRegistry.Register("FastAi", c => new FastAiStrategy(c, new SimpleFastAi(c)));
		}

		private FastPoint GenerateCheckPoint(int index)
		{
			fixed (FastGlobalState* g = &global)
			{
				var minDiffDist = Math.Sqrt(config.GAME_WIDTH * config.GAME_WIDTH + config.GAME_HEIGHT * config.GAME_HEIGHT) * 0.25;
				var awayPoints = new List<Point>();
				for (int i = index + CHECKPOINTS_COUNT - CHECKPOINTS_LAST_AWAY_COUNT; i <= index + CHECKPOINTS_COUNT + CHECKPOINTS_LAST_AWAY_COUNT; i++)
				{
					var ai = i % CHECKPOINTS_COUNT;
					if (ai == index || ai >= g->checkpoints.count)
						continue;
					var fastPoint = (FastPoint*)g->checkpoints.data + ai;
					awayPoints.Add(new Point(fastPoint->x, fastPoint->y));
				}
				while (true)
				{
					var nextGlobalTarget = new Point(
						config.GAME_WIDTH / 10 + random.NextDouble() * config.GAME_WIDTH * 8 / 10,
						config.GAME_HEIGHT / 10 + random.NextDouble() * config.GAME_HEIGHT * 8 / 10);
					if (awayPoints.All(p => p.Distance(nextGlobalTarget) > minDiffDist))
						return new FastPoint(nextGlobalTarget.x, nextGlobalTarget.y);
				}
			}
		}

		protected sealed override Direct GetDirect()
		{
			var sim = new FastState(state, nextCheckpoint);
			PrepareGlobalTarget(&sim);
			fixed (FastGlobalState* g = &global)
			{
				var fastDirect = ai.GetDirect(g, &sim, 0);
				return fastDirect.ToDirect(config);
			}
		}

		private void PrepareGlobalTarget(FastState* sim)
		{
			fixed (FastGlobalState* g = &global)
			{
				sim->nextCheckpoint = nextCheckpoint;
				if (sim->UpdateNextCheckpoint(g))
					nextCheckpoint = sim->nextCheckpoint;
			}
		}
	}
}