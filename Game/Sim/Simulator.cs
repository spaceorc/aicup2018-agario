using System;
using System.Collections.Generic;
using System.Linq;
using Game.Types;

namespace Game.Sim
{
	public class Simulator
	{
		public int tick;
		public int id_counter = 1000000;
		public List<Food> foods = new List<Food>();
		public List<Virus> viruses = new List<Virus>();
		public List<Ejection> ejections = new List<Ejection>();
		public List<Player>[] players =
		{
			new List<Player>(),
			new List<Player>(),
			new List<Player>(),
			new List<Player>()
		};
		public int[] scores = new int[4];
		
		public Simulator()
		{
		}

		public Simulator(SimState simState)
		{
			tick = simState.tick;
			viruses = simState.viruses.Select(x => x.Value.item.Clone()).ToList();
			foods = simState.foods.Select(x => x.Value.item.Clone()).ToList();
			ejections = simState.ejections.Select(x => x.Value.item.Clone()).ToList();
			players[0].AddRange(simState.players[simState.myId].Select(x => x.Value.item.Clone()));
			var p = 1;
			foreach (var kvp in simState.players.Where(x => x.Key != simState.myId))
			{
				players[p].AddRange(kvp.Value.Select(x => x.Value.item.Clone()));
				p++;
			}
		}

		public Simulator Clone()
		{
			return new Simulator
			{
				tick = tick,
				id_counter = id_counter,
				ejections = ejections.Select(x => x.Clone()).ToList(),
				foods = foods.Select(x => x.Clone()).ToList(),
				viruses = viruses.Select(x => x.Clone()).ToList(),
				scores = scores.ToArray(),
				players = players.Select(fragments => fragments.Select(f => f.Clone()).ToList()).ToArray(),
			};
		}

		public void Tick(params Direct[] directs)
		{
			ApplyDirects(directs);
			MoveMoveables();
			PlayerEjects(directs);
			PlayerSplits(directs);

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

			tick++;
		}

		private void UpdatePlayersRadius()
		{
			foreach (var fragments in players)
				foreach (var frag in fragments)
					frag.UpdateByMass();
		}

		private void SplitViruses()
		{
			var virusesCount = viruses.Count;
			for (var i = 0; i < virusesCount; i++)
			{
				var virus = viruses[i];
				if (virus.CanSplit())
				{
					var new_virus = virus.SplitNow(id_counter);
					viruses.Add(new_virus);
					id_counter++;
				}
			}
		}

		private void UpdateScores()
		{
			for (var i = 0; i < players.Length; i++)
			{
				var fragments = players[i];
				foreach (var frag in fragments)
				{
					var score = frag.score;
					if (score > 0)
					{
						frag.score = 0;
						scores[i] += score;
					}
				}
			}
		}
		private void EatAll()
		{
			Func<Circle, Player> nearest_player = circle =>
			{
				Player nearest_predator = null;
				var deeper_dist = double.NegativeInfinity;
				foreach (var fragments in players)
				{
					foreach (var predator in fragments)
					{
						var qdist = predator.CanEat(circle);
						if (qdist > deeper_dist)
						{
							deeper_dist = qdist;
							nearest_predator = predator;
						}
					}
				}
				return nearest_predator;
			};

			Func<Ejection, Virus> nearest_virus = eject =>
			{
				Virus nearest_predator = null;
				var deeper_dist = double.NegativeInfinity;
				foreach (var predator in viruses)
				{
					var qdist = predator.CanEat(eject);
					if (qdist > deeper_dist)
					{
						deeper_dist = qdist;
						nearest_predator = predator;
					}
				}
				return nearest_predator;
			};

			for (var i = 0; i < foods.Count;)
			{
				var food = foods[i];
				var eater = nearest_player(food);
				if (eater != null)
				{
					eater.Eat(food);
					foods.RemoveAt(i);
				}
				else
					i++;
			}

			for (var i = 0; i < ejections.Count;)
			{
				var ejection = ejections[i];
				var virus = nearest_virus(ejection);
				if (virus != null)
				{
					virus.Eat(ejection);
					ejections.RemoveAt(i);
				}
				else
				{
					var player = nearest_player(ejection);
					if (player != null)
					{
						player.Eat(ejection);
						ejections.RemoveAt(i);
					}
					else
						i++;
				}
			}

			for (var pi = 0; pi < players.Length; pi++)
			{
				var fragments = players[pi];
				for (var i = 0; i < fragments.Count;)
				{
					var frag = fragments[i];
					var eater = nearest_player(frag);
					if (eater != null)
					{
						var isLast = fragments.Count == 1;
						eater.Eat(frag, isLast);
						fragments.RemoveAt(i);
					}
					else
						i++;
				}
			}
		}

		private void FusePlayers()
		{
			foreach (var fragments in players)
			{
				if (fragments.Count == 0)
					continue;

				for (var i = 0; i < fragments.Count - 1; i++)
				{
					var frag = fragments[i];
					for (var j = i + 1; j < fragments.Count; )
					{
						var frag2 = fragments[j];
						if (frag.CanFuse(frag2))
						{
							frag.Fusion(frag2);
							fragments.RemoveAt(j);
						}
						else
							j++;
					}
				}

				if (fragments.Count == 1)
					fragments[0].ClearFragments();
			}
		}

		private void BurstOnViruses()
		{
			Func<Virus, Tuple<int, Player>> nearest_to = virus =>
			{
				var nearest_dist = double.PositiveInfinity;
				Player nearest_player = null;
				int nearest_player_i = -1;

				for (var i = 0; i < players.Length; i++)
				{
					var fragments = players[i];
					foreach (var frag in fragments)
					{
						var qdist = virus.CanHurt(frag);
						if (qdist < nearest_dist)
						{
							var yet_cnt = fragments.Count;
							if (frag.CanBurst(yet_cnt))
							{
								nearest_dist = qdist;
								nearest_player = frag;
								nearest_player_i = i;
							}
						}
					}
				}

				return nearest_player == null ? null : Tuple.Create(nearest_player_i, nearest_player);
			};

			foreach (var virus in viruses.ToList())
			{
				var playerT = nearest_to(virus);
				if (playerT != null)
				{
					var yet_cnt = players[playerT.Item1].Count;
					int max_fId = players[playerT.Item1].Max(p => p.fragmentId);

					playerT.Item2.BurstOn(virus);
					var fragments = playerT.Item2.BurstNow(max_fId, yet_cnt);
					players[playerT.Item1].AddRange(fragments);
					
					viruses.Remove(virus);
				}
			}
		}

		private void ShrinkPlayers()
		{
			foreach (var fragments in players)
			{
				foreach (var frag in fragments)
				{
					if (frag.CanShrink())
						frag.ShrinkNow();
				}
			}
		}

		private void MoveMoveables()
		{
			foreach (var eject in ejections)
				eject.Move();
			foreach (var virus in viruses)
				virus.Move();
			foreach (var fragments in players)
			{
				for (var i = 0; i != fragments.Count; ++i)
				{
					var curr = fragments[i];
					for (var j = i + 1; j < fragments.Count; ++j)
						curr.CollisionCalc(fragments[j]);
				}
			}
			foreach (var fragments in players)
				foreach (var frag in fragments)
					frag.Move();
		}

		private void PlayerEjects(Direct[] directs)
		{
			for (var i = 0; i < directs.Length; i++)
			{
				var direct = directs[i];
				if (direct == null || direct.split || !direct.eject)
					continue;
				var fragments = players[i];
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

		private void PlayerSplits(Direct[] directs)
		{
			for (var i = 0; i < directs.Length; i++)
			{
				var direct = directs[i];
				if (direct == null || !direct.split)
					continue;
				var fragments = players[i];
				var yet_cnt = fragments.Count;

				var origFragmentsCount = fragments.Count;
				for (var fi = 0; fi < origFragmentsCount; fi++)
				{
					var frag = fragments[fi];
					if (frag.CanSplit(yet_cnt))
					{
						var max_fId = fragments.Max(f => f.fragmentId);
						var new_frag = frag.SplitNow(max_fId);
						fragments.Add(new_frag);
						yet_cnt++;
					}
				}
			}
		}

		private void ApplyDirects(Direct[] directs)
		{
			for (var i = 0; i < directs.Length; i++)
			{
				if (directs[i] == null)
					continue;
				var fragments = players[i];
				foreach (var frag in fragments)
					frag.ApplyDirect(directs[i]);
			}
		}
	}
}