using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public class GAStrategy : SimulationStrategyBase
	{
		public const int GA_TURN_GROUPS = 6;
		public const int GA_TURNS = 10;
		public const int GA_POPULATION = 7;

		public const double MUTATION_PROB = 0.05;

		public const double ENEMY_SPLIT_TO_LIMIT = 0.05;
		public const double ENEMY_VICTIM_LIMIT = 0.1;
		public const double ENEMY_SPLIT_AWAY_LIMIT = 0.15;
		public const double ENEMY_AGGRESSOR_LIMIT = 0.2;
		public const double ENEMY_SPLIT_TO_TARGET_LIMIT = 0.3;
		public const double ENEMY_MOVE_TO_TARGET_LIMIT = 0.4;

		public const double NOENEMY_SPLIT_TO_TARGET_LIMIT = 0.1;
		public const double NOENEMY_MOVE_TO_TARGET_LIMIT = 0.3;

		public double[] aggressiveGenomeFragment = { 0, 0, 0 };
		public double[] victimGenomeFragment = { 0.125, 0.125, 0.125 };
		public double[] ignoringGenomeFragment = { 0.25, 0.25, 0.25 };
		public double[] noEnemiesGenomeFragment = { 0, 0, 0 };

		public static int[] selectionOperator;

		static GAStrategy()
		{
			var selectionOp = new List<int>();
			for (int i = 0; i < GA_POPULATION; i++)
				selectionOp.AddRange(Enumerable.Repeat(i, GA_POPULATION - i));
			selectionOperator = selectionOp.ToArray();
		}

		public GAStrategy(Config config) : base(config)
		{
		}

		private class Creature
		{
			public double[] genome;
			public double score;
			public Direct desision;
		}

		protected override Direct GetDirect(Simulator sim)
		{
			// genome: GA_TURN_GROUPS groups (each is the behavior for next N=GA_TURNS ticks)
			//   metaGene:
			//     no enemies: target - nearest food or global target
			//        0.0...0.1 - split to target (at tick -> 0...N-1) if can, otherwise and all other time - move to target
			//        0.1...0.3 - move to target
			//        0.3...1.0 - move according to angle and distance (from mass-center)
			//
			//     has enemies: target - nearest food or global target
			//        0.00...0.05 - split to victim (at tick -> 0...N-1) if can, otherwise and all other time - move to victim
			//        0.05...0.10 - move to victim
			//        0.10...0.15 - split away from enemies mass-center (at tick -> 0...N-1) if can, otherwise and all other time - move away from enemies mass-center
			//        0.15...0.20 - move away from enemies mass-center
			//        0.20...0.30 - split to target (at tick -> 0...N-1) if can, otherwise and all other time - move to target
			//        0.30...0.40 - move to target
			//        0.40...1.00 - move according to angle and distance (from mass-center)
			//
			//   angle (0...1) - angle of vector from mass-center (0...2*pi)
			//   distance (0...1) - length of vector from mass-center (0...MAX_DIST) MAX_DIST=config.GAME_HEIGHT

			var stopwatch = Stopwatch.StartNew();

			var populaton = GeneratePopulation(sim, 0);
			var generation = 0;
			while (stopwatch.ElapsedMilliseconds < 17)
			{
				var newCreature = EvolveNewCreature(populaton);
				newCreature.desision = Simulate(sim, 0, newCreature.genome);
				newCreature.score = Evaluate(sim, 0);
				populaton.Add(newCreature);
				populaton.Sort((g1, g2) => g1.score > g2.score ? -1 : g1.score < g2.score ? 1 : 0);
				populaton.RemoveAt(populaton.Count - 1);
				generation++;
			}
			Logger.Info($"Generation: {generation}");
			Logger.Info($"Best score: {populaton[0].score}");
			return populaton[0].desision;
		}

		private Creature EvolveNewCreature(List<Creature> populaton)
		{
			var father = selectionOperator[random.Next(selectionOperator.Length)];
			var mother = selectionOperator[random.Next(selectionOperator.Length)];
			var son = new double[populaton[father].genome.Length];
			Array.Copy(populaton[father].genome, son, son.Length);
			if (father == mother)
			{
				var mutationPoint = random.Next(son.Length);
				son[mutationPoint] = random.NextDouble();
			}
			else
			{
				var crossoverPoint = random.Next(0, son.Length + 1);
				if (crossoverPoint < son.Length)
					Array.Copy(populaton[mother].genome, crossoverPoint, son, crossoverPoint, son.Length - crossoverPoint);
				if (random.NextDouble() < MUTATION_PROB)
				{
					var mutationPoint = random.Next(son.Length);
					son[mutationPoint] = random.NextDouble();
				}
			}

			return new Creature
			{
				genome = son
			};
		}

		private List<Creature> GeneratePopulation(Simulator sim, int player)
		{
			var result = new List<Creature>();
			GenerateHeuristicCreatures(sim, player, result);
			while (result.Count < GA_POPULATION)
				result.Add(new Creature { genome = GenerateRandomCreature() });
			foreach (var creature in result)
			{
				var clone = sim.Clone();
				creature.desision = Simulate(clone, player, creature.genome);
				creature.score = Evaluate(clone, player);
			}
			result.Sort((g1, g2) => g1.score > g2.score ? -1 : g1.score < g2.score ? 1 : 0);
			return result;
		}

		private double[] GenerateRandomCreature()
		{
			var result = new double[GA_TURN_GROUPS * 3];
			for (var i = 0; i < result.Length; i++)
				result[i] = random.NextDouble();
			return result;
		}

		private void GenerateHeuristicCreatures(Simulator sim, int player, List<Creature> population)
		{
			if (HasEnemies(sim, player))
			{
				population.Add(new Creature { genome = MakeGenome(aggressiveGenomeFragment) });
				population.Add(new Creature { genome = MakeGenome(victimGenomeFragment) });
				population.Add(new Creature { genome = MakeGenome(ignoringGenomeFragment) });
			}
			else
				population.Add(new Creature { genome = MakeGenome(noEnemiesGenomeFragment) });
		}

		private double[] MakeGenome(double[] genomeFragment)
		{
			var result = new double[GA_TURN_GROUPS * 3];
			for (var g = 0; g < GA_TURN_GROUPS * 3; g += 3)
			{
				result[g + 0] = genomeFragment[0];
				result[g + 1] = genomeFragment[1];
				result[g + 2] = genomeFragment[2];
			}

			return result;
		}

		private Direct Simulate(Simulator sim, int player, double[] genome)
		{
			Direct result = null;
			for (var g = 0; g < GA_TURN_GROUPS * 3; g += 3)
			{
				if (sim.players[player].Count == 0)
					return result;

				Simulate(sim, player, genome[g], genome[g + 1], genome[g + 2], out var nextDesision);
				if (g == 0)
					result = nextDesision;
			}

			return result;
		}

		private double Evaluate(Simulator sim, int player)
		{
			if (sim.players[player].Count == 0)
				return double.MinValue;

			var score = sim.scores[0] - sim.scores.Skip(1).Max();
			var minGlobalQDist = sim.players[player].Min(p => p.QDistance(globalTarget));

			var minFoodQDist = double.PositiveInfinity;
			var avgFoodQDist = 0.0;
			foreach (var frag in sim.players[player])
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

			avgFoodQDist /= sim.players[player].Count;

			var result = 10000.0 * score + 0.5 * (config.GAME_HEIGHT - Math.Sqrt(minGlobalQDist) / config.GAME_HEIGHT);
			if (!double.IsPositiveInfinity(minFoodQDist))
				result += 100.0 * (config.GAME_HEIGHT - Math.Sqrt(minFoodQDist) / config.GAME_HEIGHT)
						  + 10.0 * (config.GAME_HEIGHT - Math.Sqrt(avgFoodQDist) / config.GAME_HEIGHT);
			return result;
		}

		private void Simulate(Simulator sim, int player, double metaGene, double angleGene, double distGene, out Direct desision)
		{
			var cache = new Cache();
			if (HasEnemies(sim, player))
			{
				if (metaGene < ENEMY_VICTIM_LIMIT)
				{
					// try to target victim
					var victim = GetVictim(sim, player, cache);
					if (victim != null)
					{
						if (metaGene < ENEMY_SPLIT_TO_LIMIT && victim.mass * 2.5 < GetBiggestAlly(sim, player, cache).mass && CanSplit(sim, player, cache))
							SimulateSplitToPlayer(sim, player, victim, (int)(metaGene * GA_TURNS / ENEMY_SPLIT_TO_LIMIT), out desision);
						else
							SimulateMoveToPlayer(sim, player, victim, out desision);

						return;
					}
				}
				else if (metaGene < ENEMY_AGGRESSOR_LIMIT)
				{
					// try to get away from aggressor
					var aggressorCenterMass = GetAggressorMassCenter(sim, player, cache);
					if (aggressorCenterMass != null)
					{
						if (metaGene < ENEMY_SPLIT_AWAY_LIMIT && CanSplit(sim, player, cache))
							SimulateSplitAwayFromPoint(sim, player, aggressorCenterMass, (int)((metaGene - ENEMY_VICTIM_LIMIT) * GA_TURNS / (ENEMY_SPLIT_AWAY_LIMIT - ENEMY_VICTIM_LIMIT)), cache, out desision);
						else
							SimulateMoveAwayFromPoint(sim, player, aggressorCenterMass, cache, out desision);

						return;
					}
				}
				else if (metaGene < ENEMY_MOVE_TO_TARGET_LIMIT)
				{
					// go to nearest food or global target
					if (metaGene < ENEMY_SPLIT_TO_TARGET_LIMIT && CanSplit(sim, player, cache))
						SimulateSplitToFoodOrTarget(sim, player, globalTarget, (int)((metaGene - ENEMY_AGGRESSOR_LIMIT) * GA_TURNS / (ENEMY_SPLIT_TO_TARGET_LIMIT - ENEMY_AGGRESSOR_LIMIT)), cache, out desision);
					else
						SimulateMoveToFoodOrTarget(sim, player, globalTarget, cache, out desision);

					return;
				}
			}
			else
			{
				if (metaGene < NOENEMY_SPLIT_TO_TARGET_LIMIT && CanSplit(sim, player, cache))
				{
					SimulateSplitToFoodOrTarget(sim, player, globalTarget, (int)(metaGene * GA_TURNS / NOENEMY_SPLIT_TO_TARGET_LIMIT), cache, out desision);
					return;
				}
				if (metaGene < NOENEMY_MOVE_TO_TARGET_LIMIT)
				{
					SimulateMoveToFoodOrTarget(sim, player, globalTarget, cache, out desision);
					return;
				}
			}

			SimulateMoveWithAngleAndDist(sim, player, angleGene, distGene, cache, out desision);
		}

		private void SimulateMoveWithAngleAndDist(Simulator sim, int player, double angleGene, double distGene, Cache cache, out Direct desision)
		{
			desision = null;
			var massCenter = GetMassCenter(sim, player, cache);
			if (massCenter == null)
				return;
			var target = new Point(massCenter);

			var angle = angleGene * 2 * Math.PI;
			var dist = distGene * config.GAME_HEIGHT;

			target.Move(angle, dist);

			var directs = new Direct[] { null, null, null, null };
			directs[player] = desision = new Direct(target, config);
			for (var i = 0; i < GA_TURNS; i++)
				sim.Tick(directs);
		}

		private void SimulateMoveToPlayer(Simulator sim, int player, Circle target, out Direct desision)
		{
			desision = null;
			var directs = new Direct[] { null, null, null, null };
			for (var i = 0; i < GA_TURNS; i++)
			{
				if (target.removed)
					return;
				directs[player] = new Direct(target, config);
				if (desision == null)
					desision = directs[player];
				sim.Tick(directs);
			}
		}

		private void SimulateSplitAwayFromPoint(Simulator sim, int player, Point awayPoint, int turn, Cache cache, out Direct desision)
		{
			desision = null;
			var massCenter = GetMassCenter(sim, player, cache);
			if (massCenter == null)
				return;
			var dx = awayPoint.x - massCenter.x;
			var dy = awayPoint.y - massCenter.y;
			var target = new Point(massCenter);
			var dist = target.Distance(awayPoint);
			target.x -= dx * 100 / dist;
			target.y -= dy * 100 / dist;
			var directs = new Direct[] { null, null, null, null };

			for (var i = 0; i < GA_TURNS; i++)
			{
				directs[player] = new Direct(target, config)
				{
					split = i == turn
				};
				if (desision == null)
					desision = directs[player];
				sim.Tick(directs);
			}
		}

		private void SimulateMoveAwayFromPoint(Simulator sim, int player, Point awayPoint, Cache cache, out Direct desision)
		{
			desision = null;
			var massCenter = GetMassCenter(sim, player, cache);
			if (massCenter == null)
				return;
			var dx = awayPoint.x - massCenter.x;
			var dy = awayPoint.y - massCenter.y;
			var target = new Point(massCenter);
			var dist = target.Distance(awayPoint);
			target.x -= dx * 100 / dist;
			target.y -= dy * 100 / dist;
			var directs = new Direct[] { null, null, null, null };
			directs[player] = desision = new Direct(target, config);

			for (var i = 0; i < GA_TURNS; i++)
				sim.Tick(directs);
		}

		private void SimulateSplitToPlayer(Simulator sim, int player, Circle target, int turn, out Direct desision)
		{
			var directs = new Direct[] { null, null, null, null };
			desision = null;
			for (var i = 0; i < GA_TURNS; i++)
			{
				if (target.removed)
					return;
				directs[player] = new Direct(target, config);
				if (i == turn)
					directs[player].split = true;
				if (desision == null)
					desision = directs[player];
				sim.Tick(directs);
			}
		}

		private void SimulateMoveToFoodOrTarget(Simulator sim, int player, Point target, Cache cache, out Direct desision)
		{
			var directs = new Direct[] { null, null, null, null };
			desision = null;
			for (var i = 0; i < GA_TURNS; i++)
			{
				var food = GetNearestFood(sim, player, cache);
				directs[player] = new Direct(food ?? target, config);
				if (desision == null)
					desision = directs[player];
				sim.Tick(directs);
			}
		}

		private void SimulateSplitToFoodOrTarget(Simulator sim, int player, Point target, int turn, Cache cache, out Direct desision)
		{
			var directs = new Direct[] { null, null, null, null };
			desision = null;
			for (var i = 0; i < GA_TURNS; i++)
			{
				var food = GetNearestFood(sim, player, cache);
				directs[player] = new Direct(food ?? target, config);
				if (i == turn)
					directs[player].split = true;
				if (desision == null)
					desision = directs[player];
				sim.Tick(directs);
			}
		}

		private Food GetNearestFood(Simulator sim, int player, Cache cache)
		{
			if (cache.nearestFood != null)
			{
				if (cache.nearestFoodIndex < sim.foods.Count && sim.foods[cache.nearestFoodIndex] == cache.nearestFood)
					return cache.nearestFood;
			}

			if (sim.foods.Count == 0)
				return null;
			cache.nearestFood = null;
			var min = double.PositiveInfinity;
			for (var p = 0; p < sim.players[player].Count; p++)
			{
				var frag = sim.players[player][p];
				for (var f = 0; f < sim.foods.Count; f++)
				{
					var food = sim.foods[f];
					var qdist = frag.QDistance(food);
					if (qdist < min)
					{
						cache.nearestFood = food;
						cache.nearestFoodIndex = f;
						min = qdist;
					}
				}
			}
			return cache.nearestFood;
		}

		private class Cache
		{
			public Food nearestFood;
			public int nearestFoodIndex;
			public bool? canSplit;
			public Point massCenter;
			public Player victim;
			public Point aggressorMassCenter;
			public Player biggestAlly;
			public Player smallestAlly;
			public bool victimAndAggressorAreDone;
		}

		private bool HasEnemies(Simulator sim, int player)
		{
			for (var i = 0; i < sim.players.Length; i++)
			{
				if (i != player && sim.players[i].Count > 0)
					return true;
			}
			return false;
		}

		private bool CanSplit(Simulator sim, int player, Cache cache)
		{
			if (cache.canSplit.HasValue)
				return cache.canSplit.Value;
			if (sim.players[player].Count >= config.MAX_FRAGS_CNT)
			{
				cache.canSplit = false;
				return false;
			}

			var fragments = sim.players[player];
			for (var i = 0; i < fragments.Count; i++)
			{
				if (fragments[i].mass > Constants.MIN_SPLIT_MASS)
				{
					cache.canSplit = true;
					return true;
				}
			}

			cache.canSplit = false;
			return false;
		}

		private Point GetMassCenter(Simulator sim, int player, Cache cache)
		{
			if (cache.massCenter != null)
				return cache.massCenter;
			var fragments = sim.players[player];
			var x = 0.0;
			var y = 0.0;
			var m = 0.0;
			for (var i = 0; i < fragments.Count; i++)
			{
				var frag = fragments[i];
				x += frag.x * frag.mass;
				y += frag.y * frag.mass;
				m += frag.mass;
			}

			cache.massCenter = new Point(x / m, y / m);
			return cache.massCenter;
		}

		private Player GetVictim(Simulator sim, int player, Cache cache)
		{
			PrepareVictimAndAggressor(sim, player, cache);
			return cache.victim;
		}

		private Point GetAggressorMassCenter(Simulator sim, int player, Cache cache)
		{
			PrepareVictimAndAggressor(sim, player, cache);
			return cache.aggressorMassCenter;
		}

		private Player GetBiggestAlly(Simulator sim, int player, Cache cache)
		{
			PrepareVictimAndAggressor(sim, player, cache);
			return cache.biggestAlly;
		}

		private void PrepareVictimAndAggressor(Simulator sim, int player, Cache cache)
		{
			if (cache.victimAndAggressorAreDone)
				return;
			cache.victimAndAggressorAreDone = true;
			var fragments = sim.players[player];
			for (var p = 0; p < fragments.Count; p++)
			{
				var frag = fragments[p];
				if (cache.biggestAlly == null || cache.biggestAlly.mass < frag.mass)
					cache.biggestAlly = frag;
				if (cache.smallestAlly == null || cache.smallestAlly.mass > frag.mass)
					cache.smallestAlly = frag;
			}

			if (cache.biggestAlly != null)
			{
				var x = 0.0;
				var y = 0.0;
				var m = 0.0;
				for (var i = 0; i < sim.players.Length; i++)
				{
					if (i != player)
					{
						fragments = sim.players[i];
						for (var p = 0; p < fragments.Count; p++)
						{
							var frag = fragments[p];
							if (cache.victim == null && cache.biggestAlly.mass > frag.mass * Constants.MASS_EAT_FACTOR
								|| cache.victim != null && cache.victim.mass > frag.mass)
								cache.victim = frag;
							if (frag.mass > cache.smallestAlly.mass * Constants.MASS_EAT_FACTOR)
							{
								x += frag.x * frag.mass;
								y += frag.y * frag.mass;
								m += frag.mass;
							}
						}
					}
				}

				if (m != 0)
					cache.aggressorMassCenter = new Point(x / m, y / m);
			}
		}
	}
}