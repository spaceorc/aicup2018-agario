using System;
using System.Diagnostics;
using System.Linq;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public class MonteCarloStrategy : SimulationStrategyBase
	{
		public MonteCarloStrategy(Config config) : base(config)
		{
		}

		protected override Direct GetDirect(Simulator sim)
		{
			var bestEvaluation = double.NegativeInfinity;
			Direct bestDirect = null;
			var evaluations = 0;
			var stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < 17)
			{
				var clone = sim.Clone();
				var total = 0;
				Direct direct = null;
				while (total < 50)
				{
					var fragments = clone.players[0];
					if (fragments.Count == 0)
						break;
					var frag = fragments[random.Next(fragments.Count)];
					var angle = random.NextDouble() * Math.PI * 2;
					var dist = Math.Exp(-random.NextDouble()) * frag.radius * 10;
					var split = random.Next(100) < 5;
					var ticks = 1 + random.Next(10);

					var target = new Point(frag);
					target.Move(angle, dist);
					var nextDirect = new Direct(target, config, split: split);
					if (direct == null)
						direct = nextDirect;

					for (int i = 0; i < ticks; i++)
						clone.Tick(nextDirect);

					total += ticks;
				}
				var evaluation = Evaluate(clone);
				evaluations++;
				if (evaluation > bestEvaluation)
				{
					bestEvaluation = evaluation;
					bestDirect = direct;
				}
			}
			Logger.Info($"Evaluations: {evaluations}");
			return bestDirect;
		}

		public static void Register()
		{
		}

		private double Evaluate(Simulator sim)
		{
			if (sim.players[0].Count == 0)
				return double.MinValue;

			var score = sim.scores[0] - sim.scores.Skip(1).Max();
			var minGlobalQDist = sim.players[0].Min(p => p.QDistance(globalTargets[globalTargetIndex]));

			var minFoodQDist = double.PositiveInfinity;
			var avgFoodQDist = 0.0;
			foreach (var frag in sim.players[0])
			{
				var min = double.PositiveInfinity;
				foreach (var food in sim.foods)
				{
					if (food.x < frag.radius && food.y < frag.radius)
					{
						if (food.QDistance(new Point(frag.radius, frag.radius)) > frag.radius * frag.radius)
							continue;
					}
					if (food.x < frag.radius && food.y > config.GAME_HEIGHT - frag.radius)
					{
						if (food.QDistance(new Point(frag.radius, config.GAME_HEIGHT - frag.radius)) > frag.radius * frag.radius)
							continue;
					}
					if (food.x > config.GAME_WIDTH - frag.radius && food.y < frag.radius)
					{
						if (food.QDistance(new Point(config.GAME_WIDTH - frag.radius, frag.radius)) > frag.radius * frag.radius)
							continue;
					}
					if (food.x > config.GAME_WIDTH - frag.radius && food.y > config.GAME_HEIGHT - frag.radius)
					{
						if (food.QDistance(new Point(config.GAME_WIDTH - frag.radius, config.GAME_HEIGHT - frag.radius)) > frag.radius * frag.radius)
							continue;
					}
					var qdist = frag.QDistance(food);
					if (qdist < min)
						min = qdist;
				}

				if (!double.IsPositiveInfinity(min))
				{
					if (minFoodQDist > min)
						minFoodQDist = min;
					avgFoodQDist += min;
				}
			}

			avgFoodQDist /= sim.players[0].Count;

			var result = 10000.0 * score + 0.5 * (config.GAME_HEIGHT - Math.Sqrt(minGlobalQDist)) / config.GAME_HEIGHT;
			if (!double.IsPositiveInfinity(minFoodQDist))
				result += 100.0 * (config.GAME_HEIGHT - Math.Sqrt(minFoodQDist)) / config.GAME_HEIGHT 
				          + 10.0 * (config.GAME_HEIGHT - Math.Sqrt(avgFoodQDist)) / config.GAME_HEIGHT;
			return result;
		}
	}
}