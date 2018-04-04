using System;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game
{
	// pack: 0
	public static class Settings
	{
		public const int FOOD_FORGET_TICKS = 20;
		public const int ENEMY_FORGET_TICKS = 20;
	}

	public class Strategy
	{
		private readonly Config config;
		private readonly SimState state;
		private Point globalTarget;
		private bool globalTargetObsolete;
		private Random random;

		public Strategy(Config config)
		{
			this.config = config;
			state = new SimState(config);
			random = new Random();
		}

		public TurnOutput OnTick(TurnInput turnInput)
		{
			state.Apply(turnInput);
			var minDist = double.PositiveInfinity;
			Point target = null;
			foreach (var player in state.players[state.myId])
			{
				if (!globalTargetObsolete && globalTarget != null && player.Value.item.Distance(globalTarget) < 4 * player.Value.item.radius)
					globalTargetObsolete = true;

				foreach (var food in state.foods)
				{
					var qdist = player.Value.item.QDistance(food.Value.item);
					if (qdist < minDist)
					{
						minDist = qdist;
						target = food.Value.item;
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