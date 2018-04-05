﻿using System;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
	public class NearestFoodStrategy : StateStrategyBase
	{
		private readonly bool fixSpeed;
		private readonly bool split;
		private Point globalTarget;
		private bool globalTargetObsolete;

		public NearestFoodStrategy(Config config, bool split = false, bool fixSpeed = false) : base(config)
		{
			this.fixSpeed = fixSpeed;
			this.split = split;
		}

		public static void Register()
		{
			StrategiesRegistry.Register("NearestFood", c => new NearestFoodStrategy(c));
			StrategiesRegistry.Register("NearestFood_FixSpeed", c => new NearestFoodStrategy(c, fixSpeed: true));
			StrategiesRegistry.Register("NearestFood_Split", c => new NearestFoodStrategy(c, split: true));
			StrategiesRegistry.Register("NearestFood_Split_FixSpeed", c => new NearestFoodStrategy(c, split: true, fixSpeed: true));
		}

		protected override Direct GetDirect()
		{
			var minDist = double.PositiveInfinity;
			Point target = null;
			foreach (var frag in state.players[state.myId])
			{
				if (!globalTargetObsolete && globalTarget != null && frag.Value.item.Distance(globalTarget) < 4 * frag.Value.item.radius)
					globalTargetObsolete = true;

				foreach (var food in state.foods)
				{
					var qdist = frag.Value.item.QDistance(food.Value.item);
					if (qdist < minDist)
					{
						minDist = qdist;
						target = food.Value.item;
						if (fixSpeed)
						{
							target = new Point(target);
							target.Move(frag.Value.item.angle + Math.PI, frag.Value.item.speed);
						}
					}
				}
			}

			if (target == null)
			{
				if (globalTarget == null)
				{
					globalTarget = new Point(
						config.GAME_WIDTH / 10 + random.NextDouble() * config.GAME_WIDTH * 8 / 10,
						config.GAME_HEIGHT / 10 + random.NextDouble() * config.GAME_HEIGHT * 8 / 10);
				}
				else if (globalTargetObsolete)
				{
					globalTargetObsolete = false;
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
				target = globalTarget;
			}

			return new Direct(target, config, split && !state.players.Where(p => p.Key != state.myId).SelectMany(p => p.Value).Any() && random.Next(100) < 5)
			{
				debug = globalTarget?.ToString() ?? $"Food : {target}"
			};
		}
	}
}