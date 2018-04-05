using System;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public class NearestFoodStrategy : IStrategy
	{
		private readonly Config config;
		private readonly bool fixSpeed;
		private readonly SimState state;
		private Point globalTarget;
		private bool globalTargetObsolete;
		private readonly Random random;

		public NearestFoodStrategy(Config config, bool fixSpeed)
		{
			this.config = config;
			this.fixSpeed = fixSpeed;
			state = new SimState(config);
			random = new Random();
		}

		public static void Register()
		{
			StrategiesRegistry.Register("NearestFood1", c => new NearestFoodStrategy(c, false));
			StrategiesRegistry.Register("NearestFood2", c => new NearestFoodStrategy(c, false));
			StrategiesRegistry.Register("NearestFood_Fixed", c => new NearestFoodStrategy(c, true));
		}

		public TurnOutput OnTick(TurnInput turnInput)
		{
			state.Apply(turnInput);
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
			
			return new TurnOutput
			{
				X = target.x,
				Y = target.y,
				Debug = globalTarget?.ToString() ?? $"Food : {target}"
			};
		}
	}
}