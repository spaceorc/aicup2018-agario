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

		public void SplitFragments(List<Player> fragments)
		{
			int fragments_count = fragments.Count;

			// Сортировка фрагментов по массе. При совпадении массы - по индексу.
			// Фрагменты с большим значением критерия после сортировки окажутся ближе к началу.
			fragments.Sort((lhs, rhs) => -(lhs.mass, lhs.fragmentId).CompareTo((rhs.mass, rhs.fragmentId)));

			foreach (Player frag in fragments)
			{
				if (frag.CanSplit(fragments_count))
				{
					int max_fId = players.Where(f => f.id == frag.id).Max(f => f.fragmentId);
					Player new_frag = frag.SplitNow(max_fId);
					players.Add(new_frag);
					fragments_count++;
				}
			}
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
				SplitFragments(fragments);
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
					playerScores[eater.id] += Constants.SCORE_FOR_FOOD;
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
						if (ejection.id != player.id)
							playerScores[player.id] += Constants.SCORE_FOR_FOOD;
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
					eater.Eat(player);
					playerScores[eater.id] += isLast ? Constants.SCORE_FOR_LAST : Constants.SCORE_FOR_PLAYER;
					players.Remove(player);
				}
			}
		}

		private void UpdatePlayersRadius()
		{
			foreach (var player in players)
				player.UpdateByMass();
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
					playerScores[player.id] += Constants.SCORE_FOR_BURST;
					var fragments = player.BurstNow(max_fId, yet_cnt);
					players.AddRange(fragments);

					viruses.Remove(virus);
				}
			}
		}

		private void FusePlayers()
		{
			var playerIds = new HashSet<int>();
			foreach (var player in players)
				playerIds.Add(player.id);

			var fusedPlayers = new List<Player>();
			foreach (var id in playerIds)
			{
				var fragments = players.Where(p => p.id == id).ToList();
				
				// приведём в предсказуемый порядок
				fragments.Sort((lhs, rhs) => (-lhs.mass, lhs.fragmentId).CompareTo((-rhs.mass, rhs.fragmentId)));

				bool new_fusion_check = true; // проверим всех. Если слияние произошло - перепроверим ещё разок, чтобы все могли слиться в один тик
				while (new_fusion_check)
				{
					new_fusion_check = false;
					for (var it = 0; it < fragments.Count - 1; it++)
					{
						var player = fragments[it];
						for (var it2 = it + 1; it2 < fragments.Count;)
						{
							var frag = fragments[it2];
							if (player.CanFuse(frag))
							{
								player.Fusion(frag);
								fusedPlayers.Add(frag);
								new_fusion_check = true;
								fragments.RemoveAt(it2);
							}
							else
								++it2;
						}
					}

					if (new_fusion_check)
					{
						foreach (var fragment in fragments)
							fragment.UpdateByMass();
					}
				}

				if (fragments.Count == 1)
					fragments[0].ClearFragments();
			}

			foreach (var p in fusedPlayers)
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


		private List<Circle> GetVisibles(List<Player> for_them)
		{
			// fog of war
			foreach (var player in players)
			{
				int frag_cnt = players.Count(p => p.id == player.id);
				player.UpdateVision(frag_cnt);
			}

			Func<Circle, bool> canSee = c =>
			{
				foreach (Player fragment in for_them)
				{
					if (fragment.CanSee(c))
						return true;
				}

				return false;
			};

			var visibles = new List<Circle>();
			foreach (var food in foods)
			{
				if (canSee(food))
					visibles.Add(food);
			}

			foreach (var eject in ejections)
			{
				if (canSee(eject))
					visibles.Add(eject);
			}

			var pId = for_them.Count == 0 ? -1 : for_them[0].id;
			foreach (var player in players)
			{
				if (player.id != pId && canSee(player))
					visibles.Add(player);
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