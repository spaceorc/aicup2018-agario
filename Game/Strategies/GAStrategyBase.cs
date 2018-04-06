using System.Linq;
using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public class GAStrategyBase : SimulationStrategyBase
	{
		public const int GA_TURN_GROUPS = 6;
		public const int GA_TURNS = 10;

		public const double ENEMY_SPLIT_TO_LIMIT = 0.05;
		public const double ENEMY_MOVE_TO_LIMIT = 0.1;
		public const double ENEMY_SPLIT_AWAY_LIMIT = 0.15;
		public const double ENEMY_MOVE_AWAY_LIMIT = 0.2;
		public const double ENEMY_SPLIT_TO_TARGET_LIMIT = 0.3;
		public const double ENEMY_MOVE_TO_TARGET_LIMIT = 0.4;

		public const double NOENEMY_SPLIT_TO_TARGET_LIMIT = 0.1;
		public const double NOENEMY_MOVE_TO_TARGET_LIMIT = 0.3;

		public GAStrategyBase(Config config) : base(config)
		{
		}

		protected override Direct GetDirect(Simulator sim)
		{
			// genome: GA_TURN_GROUPS groups (each is the behavior for next N=GA_TURNS ticks)
			//   metagene:
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


		}

		private void Simulate(Simulator sim, int player, double[] genome)
		{
			for (int g = 0; g < GA_TURN_GROUPS * 3; g+=3)
			{
				Simulate(sim, player, genome[g], genome[g + 1], genome[g + 2]);
			}
		}

		private void Simulate(Simulator sim, int player, double metagene, double angle, double dist)
		{
			bool? canSplitCache = null;
			if (HasEnemies(sim, player))
			{
				if (metagene < ENEMY_SPLIT_TO_LIMIT && CanSplit(sim, player, ref canSplitCache))
				{
					// split to victim 
					// todo what to do if there is no victim?
					return;
				}
				if (metagene < ENEMY_MOVE_TO_LIMIT)
				{
					// move to victim
					return;
				}
				if (metagene < ENEMY_SPLIT_AWAY_LIMIT && CanSplit(sim, player, ref canSplitCache))
				{
					// split away from enemies mass-center
					// todo what to do if there is no aggressor?
					return;
				}
				if (metagene < ENEMY_MOVE_AWAY_LIMIT)
				{
					// move away from enemies mass-center
					return;
				}
				if (metagene < ENEMY_SPLIT_TO_TARGET_LIMIT && CanSplit(sim, player, ref canSplitCache))
				{
					// split to target (food or global)
					return;
				}
				if (metagene < ENEMY_MOVE_TO_TARGET_LIMIT)
				{
					// move to target (food or global)
					return;
				}
			}
			else
			{
				if (metagene < NOENEMY_SPLIT_TO_TARGET_LIMIT && CanSplit(sim, player, ref canSplitCache))
				{
					// split to target (food or global)
					return;
				}
				if (metagene < NOENEMY_MOVE_TO_TARGET_LIMIT)
				{
					// move to target (food or global)
					return;
				}
			}
			// use angle and dist
		}

		private bool HasEnemies(Simulator sim, int player)
		{
			for (int i = 0; i < sim.players.Length; i++)
			{
				if (i != player && sim.players[i].Count > 0)
					return true;
			}
			return false;
		}

		private bool CanSplit(Simulator sim, int player, ref bool? cache)
		{
			if (cache.HasValue)
				return cache.Value;
			if (sim.players[player].Count >= config.MAX_FRAGS_CNT)
			{
				cache = false;
				return false;
			}

			var fragments = sim.players[player];
			for (int i = 0; i < fragments.Count; i++)
			{
				if (fragments[i].mass > Constants.MIN_SPLIT_MASS)
				{
					cache = true;
					return true;
				}
			}

			cache = false;
			return false;
		}
	}
}