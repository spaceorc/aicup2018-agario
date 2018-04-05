using System;
using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace Game.Mech
{
	public class Mechanic
	{
		public int tick;
		public int id_counter;

		private readonly Config config;

		public readonly List<Player> players = new List<Player>();
		public readonly List<Food> foods = new List<Food>();
		public readonly List<Ejection> ejections = new List<Ejection>();
		public readonly List<Virus> viruses = new List<Virus>();

		public readonly Dictionary<int, PlayerStrategy> strategies = new Dictionary<int, PlayerStrategy>();
		public readonly Dictionary<int, Direct> strategyDirects = new Dictionary<int, Direct>();
		public readonly Dictionary<int, int> playerScores = new Dictionary<int, int>();
		private readonly Random random;

		public Mechanic(Config config, List<PlayerStrategy> strategies, int? seed = null)
		{
			this.config = config;
			random = new Random(seed ?? Guid.NewGuid().GetHashCode());
			id_counter = 1;
			AddPlayer(Constants.START_PLAYER_SETS, strategies);
			AddFood(Constants.START_FOOD_SETS);
			AddVirus(Constants.START_VIRUS_SETS);
		}

		public void Play()
		{
			while (true)
			{
				TickEvent();
				if (tick >= config.GAME_TICKS || Known())
					break;
			}
		}

		public int TickEvent()
		{
			ApplyStrategies();
			tick++;
			MoveMoveables();
			PlayerEjects();
			PlayerSplits();

			if (tick % Constants.SHRINK_EVERY_TICK == 0)
			{
				ShrinkPlayers();
			}
			EatAll();
			FusePlayers();
			BurstOnViruses();

			UpdatePlayersRadius();
			UpdateScores();
			SplitViruses();

			if (tick % Constants.ADD_FOOD_DELAY == 0 && foods.Count < Constants.MAX_GAME_FOOD)
			{
				AddFood(Constants.ADD_FOOD_SETS);
			}
			if (tick % Constants.ADD_VIRUS_DELAY == 0 && viruses.Count < Constants.MAX_GAME_VIRUS)
			{
				AddVirus(Constants.ADD_VIRUS_SETS);
			}
			strategyDirects.Clear();
			return tick;
		}

		public bool Known()
		{
			var livingIds = players.Select(x => x.id).Distinct().ToList();
			if (livingIds.Count == 0) {
				return true;
			}

			if (livingIds.Count == 1) {
				int living_score = playerScores[livingIds[0]];
				foreach (int pId in playerScores.Keys) {
					if (pId != livingIds[0] && playerScores[pId] >= living_score) {
						return false;
					}
				}
				return true;
			}
			return false;
		}

		private void PlayerSplits()
		{
			foreach (var it in strategyDirects)
			{
				var sId = it.Key;
				var direct = it.Value;
				if (!direct.split)
					continue;
				var fragments = players.Where(p => p.id == sId).ToList();
				int yet_cnt = fragments.Count;

				foreach (var frag in fragments)
				{
					if (frag.CanSplit(yet_cnt))
					{
						int max_fId = fragments.Max(f => f.fragmentId);
						var new_frag = frag.SplitNow(max_fId);
						players.Add(new_frag);
						yet_cnt++;
					}
				}
			}
		}

		private void PlayerEjects()
		{
			foreach (var it in strategyDirects)
			{
				var sId = it.Key;
				var direct = it.Value;
				if (direct.split || !direct.eject)
					continue;
				var fragments = players.Where(p => p.id == sId).ToList();

				foreach (var frag in fragments)
				{
					if (frag.CanEject())
					{
						var new_eject = frag.EjectNow(id_counter);
						ejections.Add(new_eject);
						id_counter++;
					}
				}
			}
		}

		private void EatAll()
		{
			Func<Circle, Player> nearest_player = circle => {
				Player nearest_predator = null;
				double deeper_dist = Double.NegativeInfinity;
				foreach (var predator in players)
				{
					double qdist = predator.CanEat(circle);
					if (qdist > deeper_dist)
					{
						deeper_dist = qdist;
						nearest_predator = predator;
					}
				}
				return nearest_predator;
			};

			Func<Ejection, Virus> nearest_virus = eject => {
				Virus nearest_predator = null;
				double deeper_dist = Double.NegativeInfinity;
				foreach (var predator in viruses)
				{
					double qdist = predator.CanEat(eject);
					if (qdist > deeper_dist)
					{
						deeper_dist = qdist;
						nearest_predator = predator;
					}
				}
				return nearest_predator;
			};

			foreach (var food in foods.ToList())
			{
				var eater = nearest_player(food);
				if (eater != null)
				{
					eater.Eat(food);
					foods.Remove(food);
				}
			}

			foreach (var ejection in ejections.ToList())
			{
				var virus = nearest_virus(ejection);
				if (virus != null)
				{
					virus.Eat(ejection);
					ejections.Remove(ejection);
				}
				else
				{
					var player = nearest_player(ejection);
					if (player != null)
					{
						player.Eat(ejection);
						ejections.Remove(ejection);
					}
				}
				
			}

			foreach (var player in players.ToList())
			{
				var eater = nearest_player(player);
				if (eater != null)
				{
					bool isLast = players.Count(pp => pp.id == player.id) == 1;
					eater.Eat(player, isLast);
					players.Remove(player);
				}
			}
		}

		private void UpdatePlayersRadius()
		{
			foreach (var player in players)
				player.UpdateByMass();
		}

		private void UpdateScores()
		{
			foreach (var player in players)
			{
				int score = player.score;
				if (score > 0)
				{
					int pId = player.id;
					player.score = 0;
					playerScores[pId] += score;
				}
			}
		}

		private void SplitViruses()
		{
			foreach (var virus in viruses.ToList())
			{
				if (virus.CanSplit())
				{
					var new_virus = virus.SplitNow(id_counter);
					viruses.Add(new_virus);
					id_counter++;
				}
			}
		}

		private void ShrinkPlayers()
		{
			foreach (var player in players)
			{
				if (player.CanShrink())
					player.ShrinkNow();
			}
		}

		private void BurstOnViruses()
		{ 
			Func<Virus, Player> nearest_to = virus => {
				double nearest_dist = Double.PositiveInfinity;
				Player nearest_player = null;

				foreach (Player player in players)
				{
					double qdist = virus.CanHurt(player);
					if (qdist < nearest_dist)
					{
						int yet_cnt = players.Count(p => p.id == player.id);
						if (player.CanBurst(yet_cnt))
						{
							nearest_dist = qdist;
							nearest_player = player;
						}
					}
				}
				return nearest_player;
			};

			foreach (var virus in viruses.ToList())
			{
				var player = nearest_to(virus);
				if (player != null)
				{
					int yet_cnt = players.Count(p => p.id == player.id);
					int max_fId = players.Where(p => p.id == player.id).Max(p => p.fragmentId);
				
					player.BurstOn(virus);
					var fragments = player.BurstNow(max_fId, yet_cnt);
					players.AddRange(fragments);

					viruses.Remove(virus);
				}
			}
		}

		private void FusePlayers()
		{
			var fused_players = new HashSet<Player>();
			foreach (Player player in players)
			{
				if (fused_players.Contains(player))
					continue;

				var fragments = players.Where(p => p.id == player.id).ToList();
				if (fragments.Count == 1)
					player.ClearFragments();

				foreach (var frag in fragments)
				{
					if (player != frag && !fused_players.Contains(frag))
					{
						if (player.CanFuse(frag))
						{
							player.Fusion(frag);
							fused_players.Add(frag);
						}
					}
				}
			}

			foreach (var p in fused_players)
				players.Remove(p);
		}

		private void MoveMoveables()
		{
			foreach (var eject in ejections)
				eject.Move();
			foreach (var virus in viruses)
				virus.Move();
			foreach (var strategyKvp in strategies)
			{
				var sId = strategyKvp.Key;
				var fragments = players.Where(p => p.id == sId).ToList();
				for (int i = 0; i != fragments.Count; ++i)
				{
					var curr = fragments[i];
					for (int j = i + 1; j < fragments.Count; ++j)
						curr.CollisionCalc(fragments[j]);
				}
			}
			foreach (var player in players)
				player.Move();
		}


		private void ApplyStrategies()
		{
			foreach (var strategy in strategies)
			{
				int sId = strategy.Key;
				var fragments = players.Where(p => p.id == sId).ToList();
				if (!fragments.Any())
					continue;

				var visibles = GetVisibles(fragments);

				Direct direct = strategy.Value.TickEvent(fragments, visibles);

				ApplyDirectFor(sId, direct);
			}
		}

		private void ApplyDirectFor(int sId, Direct direct)
		{
			var fragments = players.Where(p => p.id == sId).ToList();
			foreach (Player frag in fragments)
				frag.ApplyDirect(direct);
			strategyDirects[sId] = direct;
		}


		private List<Circle> GetVisibles(List<Player> for_them) {
			// fog of war
			foreach (var player in players) {
				int frag_cnt = players.Count(p => p.id == player.id);
				player.UpdateVision(frag_cnt);
			}

			var visibles = new List<Circle>();
			foreach (var fragment in for_them)
			{
				foreach (var food in foods)
				{
					if (fragment.CanSee(food))
						visibles.Add(food);
				}
				foreach (var eject in ejections)
				{
					if (fragment.CanSee(eject))
						visibles.Add(eject);
				}
				foreach (var player in players)
				{
					if (for_them.IndexOf(player) == -1)
					{
						if (fragment.CanSee(player))
							visibles.Add(player);
					}
				}
			}

			foreach (var virus in viruses)
				visibles.Add(virus);

			return visibles;
		}


		private bool IsSpaceEmpty(double _x, double _y, double _radius)
		{
			foreach (var player in players)
			{
				if (player.Intersects(_x, _y, _radius))
					return false;
			}
			foreach (var virus in viruses)
			{
				if (virus.Intersects(_x, _y, _radius))
					return false;
			}
			return true;
		}

		private void AddCircular(int sets_cnt, double one_radius, Action<double, double> addOne)
		{
			double center_x = config.GAME_WIDTH / 2, center_y = config.GAME_HEIGHT / 2;
			for (int I = 0; I < sets_cnt; I++)
			{
				double _x = random.Next() % Math.Ceiling(center_x - 4 * one_radius) + 2 * one_radius;
				double _y = random.Next() % Math.Ceiling(center_y - 4 * one_radius) + 2 * one_radius;

				addOne(_x, _y);
				addOne(center_x + (center_x - _x), _y);
				addOne(center_x + (center_x - _x), center_y + (center_y - _y));
				addOne(_x, center_y + (center_y - _y));
			}
		}

		private void AddFood(int sets_cnt)
		{
			AddCircular(sets_cnt, Constants.FOOD_RADIUS, (_x, _y) => {
				Food new_food = new Food(id_counter, _x, _y, config);
				foods.Add(new_food);
				id_counter++;
			});
		}

		private void AddVirus(int sets_cnt)
		{
			double rad = config.VIRUS_RADIUS;
			AddCircular(sets_cnt, rad, (_x, _y) => {
				if (!IsSpaceEmpty(_x, _y, rad))
				{
					return;
				}
				Virus new_virus = new Virus(id_counter, _x, _y, rad, config);
				viruses.Add(new_virus);
				id_counter++;
			});
		}

		private void AddPlayer(int sets_cnt, List<PlayerStrategy> strategies)
		{
			int index = 0;
			AddCircular(sets_cnt, Constants.PLAYER_RADIUS, (_x, _y) =>
			{
				if (!IsSpaceEmpty(_x, _y, Constants.PLAYER_RADIUS))
					return;

				var new_player = new Player(id_counter, _x, _y, Constants.PLAYER_RADIUS, Constants.PLAYER_MASS, 0, config);
				players.Add(new_player);
				new_player.UpdateByMass();

				var new_strategy = strategies[index++];
				this.strategies[id_counter] = new_strategy;

				playerScores[id_counter] = 0;
				id_counter++;
			});
		}

	}
}