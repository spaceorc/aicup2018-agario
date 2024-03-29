﻿using System.Linq;
using System.Runtime.InteropServices;
using Game.Protocol;
using Game.Sim.Types;
using Game.Types;

namespace Game.Sim
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct Simulator
	{
		public FastPoint.List foods; // 16 nearest
		public FastEjection.List ejections; // 16 nearest
		public FastVirus.List viruses; // 16 nearest

		// all fragments are ordered by mass in descending order
		public FastFragment.List fragments0; // 16 most massive
		public FastFragment.List fragments1; // 16 nearest
		public FastFragment.List fragments2; // 16 nearest
		public FastFragment.List fragments3; // 16 nearest

		public fixed int scores[4];
		public int checkpointsTaken;
		public int nextCheckpoint;
		public int tick;

		public Simulator(State state, int nextCheckpoint) : this()
		{
			var myFragments = state.players[state.myId];
			if (!myFragments.Any())
				return;
			foreach (var food in state.foods.Values.Where(x => !x.ban).OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item))).Take(FastPoint.List.capacity))
				foods.Add(new FastPoint(food.item));
			foreach (var ejection in state.ejections.Values.OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item))).Take(FastEjection.List.capacity))
				ejections.Add(new FastEjection(ejection.item));
			foreach (var virus in state.viruses.Values.OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item))).Take(FastVirus.List.capacity))
				viruses.Add(new FastVirus(virus.item));
			foreach (var frag in myFragments.Values.OrderByDescending(x => x.item.mass).ThenByDescending(x => x.item.fragmentId).Take(FastFragment.List.capacity))
				fragments0.Add(new FastFragment(frag.item));

			fixed (FastFragment.List* l = &fragments0)
			{
				var cur = l;
				foreach (var p in state.players.Where(x => x.Key != state.myId))
				{
					cur++;
					var fragments = p.Value.Values;
					foreach (var frag in fragments
						.OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item)))
						.Take(FastFragment.List.capacity)
						.OrderByDescending(x => x.item.mass)
						.ThenByDescending(x => x.item.fragmentId))
					{
						cur->Add(new FastFragment(frag.item));
					}
				}
			}

			this.nextCheckpoint = nextCheckpoint;
			tick = state.tick;

			// todo recover score - it may be needed for better evaluation
		}

		public void Tick(FastGlobal* global, FastDirect.List* directs, Config config)
		{
			ApplyDirects(directs, config);
			MoveMoveables(config);
			PlayerEjects(directs);
			PlayerSplits(directs, config);

			if (tick % Constants.SHRINK_EVERY_TICK == 0)
				ShrinkPlayers();

			EatAll(config);
			FusePlayers(config);
			BurstOnViruses(config);

			UpdatePlayersRadius(config);
			SplitViruses(config);

			if (UpdateNextCheckpoint(global))
				checkpointsTaken++;

			tick++;
		}

		private void ApplyDirects(FastDirect.List* directs, Config config)
		{
			fixed (Simulator* that = &this)
			{
				var direct = (FastDirect*)directs->data;
				var fragments = &that->fragments0;
				for (var p = 0; p < directs->count; p++, direct++, fragments++)
				{
					if (double.IsNaN(direct->target.x))
						continue;
					var frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count; i++, frag++)
						frag->ApplyDirect(direct, config);
				}
			}
		}

		private void MoveMoveables(Config config)
		{
			fixed (Simulator* that = &this)
			{
				var eject = (FastEjection*)that->ejections.data;
				for (int i = 0; i < that->ejections.count; i++, eject++)
					eject->Move(config);

				var virus = (FastVirus*)that->viruses.data;
				for (int i = 0; i < that->viruses.count; i++, virus++)
					virus->Move(config);

				var fragments = &that->fragments0;
				for (var p = 0; p < 4; p++, fragments++)
				{
					var frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count - 1; i++, frag++)
					{
						var next = frag + 1;
						for (var k = i + 1; k < fragments->count; k++, next++)
							frag->CollisionCalc(next);
					}
					frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count; i++, frag++)
						frag->Move(config);
				}
			}
		}

		private void PlayerEjects(FastDirect.List* directs)
		{
			fixed (Simulator* that = &this)
			{
				var direct = (FastDirect*)directs->data;
				var fragments = &that->fragments0;
				for (var p = 0; p < directs->count; p++, direct++, fragments++)
				{
					if (double.IsNaN(direct->target.x) || direct->split == 1 || direct->eject == 0)
						continue;
					var frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count; i++, frag++)
					{
						if (frag->CanEject())
						{
							var new_eject = frag->EjectNow(p);
							if (that->ejections.count < FastEjection.List.capacity)
								that->ejections.Add(new_eject);
						}
					}
				}
			}
		}

		private void PlayerSplits(FastDirect.List* directs, Config config)
		{
			fixed (Simulator* that = &this)
			{
				var direct = (FastDirect*)directs->data;
				var fragments = &that->fragments0;
				for (var p = 0; p < directs->count; p++, direct++, fragments++)
				{
					if (double.IsNaN(direct->target.x) || direct->split == 0)
						continue;
					var frag = (FastFragment*)fragments->data;

					var origFragmentsCount = fragments->count;

					for (var i = 0; i < origFragmentsCount; i++, frag++)
					{
						if (frag->CanSplit(fragments->count, config))
						{
							var new_frag = frag->SplitNow(config);
							if (fragments->count < FastFragment.List.capacity)
								fragments->Add(new_frag);
						}
					}

					fragments->Sort();
				}
			}
		}

		private void ShrinkPlayers()
		{
			fixed (Simulator* that = &this)
			{
				var fragments = &that->fragments0;
				for (var p = 0; p < 4; p++, fragments++)
				{
					var frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count; i++, frag++)
					{
						if (frag->CanShrink())
							frag->ShrinkNow();
					}
				}
			}
		}

		private void EatFood(Config config)
		{
			fixed (Simulator* that = &this)
			{
				var food = (FastPoint*)that->foods.data;
				byte tf = 0;
				var tfood = food;
				for (var f = 0; f < that->foods.count; f++, food++)
				{
					var fragments = &that->fragments0;
					FastFragment* eater = null;
					var eaterPlayer = -1;
					var deeper_dist = double.NegativeInfinity;
					for (var p = 0; p < 4; p++, fragments++)
					{
						var frag = (FastFragment*)fragments->data;
						for (var i = 0; i < fragments->count; i++, frag++)
						{
							var qdist = frag->CanEat(food, config);
							if (qdist > deeper_dist)
							{
								eater = frag;
								eaterPlayer = p;
								deeper_dist = qdist;
							}
						}
					}

					if (eater != null)
					{
						eater->Eat(food, config);
						that->scores[eaterPlayer] += Constants.SCORE_FOR_FOOD;
					}
					else
					{
						if (tf != f)
							*tfood = *food;
						tf++;
						tfood++;
					}
				}
				that->foods.count = tf;
			}
		}

		private void EatFragments()
		{
			fixed (Simulator* that = &this)
			{
				var ffragments = &that->fragments0;
				for (var fp = 0; fp < 4; fp++, ffragments++)
				{
					var ffrag = (FastFragment*)ffragments->data;
					byte tf = 0;
					var tffrag = ffrag;
					for (var fi = 0; fi < ffragments->count; fi++, ffrag++)
					{
						var fragments = &that->fragments0;
						FastFragment* eater = null;
						var eaterPlayer = -1;
						var deeper_dist = double.NegativeInfinity;
						for (var p = 0; p < 4; p++, fragments++)
						{
							if (p == fp)
								continue;
							var frag = (FastFragment*)fragments->data;
							for (var i = 0; i < fragments->count; i++, frag++)
							{
								var qdist = frag->CanEat(ffrag);
								if (qdist > deeper_dist)
								{
									eater = frag;
									eaterPlayer = p;
									deeper_dist = qdist;
								}
							}
						}

						if (eater != null)
						{
							var isLast = fi == ffragments->count - 1 && tf == 0;
							eater->Eat(ffrag);
							that->scores[eaterPlayer] += isLast ? Constants.SCORE_FOR_LAST : Constants.SCORE_FOR_PLAYER;
						}
						else
						{
							if (tf != fi)
								*tffrag = *ffrag;
							tf++;
							tffrag++;
						}
					}

					ffragments->count = tf;
				}
			}
		}

		private void EatEjections()
		{
			fixed (Simulator* that = &this)
			{
				var eject = (FastEjection*)that->ejections.data;
				byte te = 0;
				var teject = eject;
				for (var e = 0; e < that->ejections.count; e++, eject++)
				{
					var virus = (FastVirus*)that->viruses.data;
					FastVirus* eaterVirus = null;
					var deeper_dist = double.NegativeInfinity;
					for (int i = 0; i < that->viruses.count; i++, virus++)
					{
						var qdist = virus->CanEat(eject);
						if (qdist > deeper_dist)
						{
							eaterVirus = virus;
							deeper_dist = qdist;
						}
					}

					if (eaterVirus != null)
						eaterVirus->Eat(eject);
					else
					{
						var fragments = &that->fragments0;
						FastFragment* eaterFrag = null;
						var eaterPlayer = -1;
						deeper_dist = double.NegativeInfinity;
						for (var p = 0; p < 4; p++, fragments++)
						{
							var frag = (FastFragment*)fragments->data;
							for (var i = 0; i < fragments->count; i++, frag++)
							{
								var qdist = frag->CanEat(eject);
								if (qdist > deeper_dist)
								{
									eaterFrag = frag;
									eaterPlayer = p;
									deeper_dist = qdist;
								}
							}
						}

						if (eaterFrag != null)
						{
							eaterFrag->Eat(eject);
							if (eaterPlayer != eject->player)
								that->scores[eaterPlayer] += Constants.SCORE_FOR_FOOD;
						}
						else
						{
							if (te != e)
								*teject = *eject;
							te++;
							teject++;
						}
					}
				}
				that->ejections.count = te;
			}
		}

		private void EatAll(Config config)
		{
			EatFood(config);;
			EatEjections();
			EatFragments();
		}

		private void FusePlayers(Config config)
		{
			fixed (Simulator* that = &this)
			{
				var fragments = &that->fragments0;
				for (var p = 0; p < 4; p++, fragments++)
				{
					if (fragments->count == 0)
						continue;

					bool new_fusion_check = true; // проверим всех. Если слияние произошло - перепроверим ещё разок, чтобы все могли слиться в один тик
					while (new_fusion_check)
					{
						new_fusion_check = false;

						var frag = (FastFragment*) fragments->data;
						for (var i = 0; i < fragments->count - 1; i++, frag++)
						{
							var next = frag + 1;
							var tk = (byte) (i + 1);
							var tnext = next;
							for (var k = i + 1; k < fragments->count; k++, next++)
							{
								if (frag->CanFuse(next))
								{
									frag->Fusion(next);
									new_fusion_check = true;
								}
								else
								{
									if (tk != k)
										*tnext = *next;
									tk++;
									tnext++;
								}
							}

							fragments->count = tk;
						}

						if (new_fusion_check)
						{
							frag = (FastFragment*)fragments->data;
							for (var i = 0; i < fragments->count; i++, frag++)
								frag->UpdateByMass(config);
						}
					}

					fragments->Sort();
				}
			}
		}

		private void BurstOnViruses(Config config)
		{
			fixed (Simulator* that = &this)
			{
				byte tv = 0;
				var virus = (FastVirus*)that->viruses.data;
				var tvirus = virus;
				for (int v = 0; v < that->viruses.count; v++, virus++)
				{
					var nearest_dist = double.PositiveInfinity;
					var fragments = &that->fragments0;
					FastFragment.List* nearest_fragments = null;
					FastFragment* nearest_frag = null;
					var nearestPlayer = -1;
					for (var p = 0; p < 4; p++, fragments++)
					{
						var frag = (FastFragment*)fragments->data;
						for (var i = 0; i < fragments->count; i++, frag++)
						{
							var qdist = virus->CanHurt(frag);
							if (qdist < nearest_dist)
							{
								var yet_cnt = fragments->count;
								if (frag->CanBurst(yet_cnt, config))
								{
									nearest_dist = qdist;
									nearest_frag = frag;
									nearestPlayer = p;
									nearest_fragments = fragments;
								}
							}
						}
					}

					if (nearest_frag != null)
					{
						nearest_frag->BurstOn(virus, config);
						that->scores[nearestPlayer] += Constants.SCORE_FOR_BURST;
						nearest_frag->BurstNow(nearest_fragments, config);
					}
					else
					{
						if (tv != v)
							*tvirus = *virus;
						tv++;
						tvirus++;
					}
				}

				that->viruses.count = tv;
			}
		}

		private void UpdatePlayersRadius(Config config)
		{
			fixed (Simulator* that = &this)
			{
				var fragments = &that->fragments0;
				for (var p = 0; p < 4; p++, fragments++)
				{
					var frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count; i++, frag++)
						frag->UpdateByMass(config);
					fragments->Sort();
				}
			}
		}

		private void SplitViruses(Config config)
		{
			fixed (Simulator* that = &this)
			{
				var virus = (FastVirus*)that->viruses.data;
				var virusesCount = that->viruses.count;
				for (int v = 0; v < virusesCount; v++, virus++)
				{
					if (virus->CanSplit(config))
					{
						var new_virus = virus->SplitNow();
						if (that->viruses.count < FastVirus.List.capacity)
							viruses.Add(new_virus);
					}
				}
			}
		}

		public bool UpdateNextCheckpoint(FastGlobal* global)
		{
			fixed (Simulator* that = &this)
			{
				var fragments = &that->fragments0;
				var frag = (FastFragment*)fragments->data;
				for (var i = 0; i < fragments->count; i++, frag++)
				{
					var checkpoints = (FastPoint*)global->checkpoints.data;
					if (frag->QDistance(checkpoints + that->nextCheckpoint) < 4 * 4 * frag->radius * frag->radius)
					{
						that->nextCheckpoint = (that->nextCheckpoint + 1) % global->checkpoints.count;
						return true;
					}
				}
			}

			return false;
		}
	}
}