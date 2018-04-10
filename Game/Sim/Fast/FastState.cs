using System;
using System.Linq;
using System.Runtime.InteropServices;
using Game.Protocol;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastState
	{
		public FastPoint.List foods; // 16 nearest
		public FastEjection.List ejections; // 16 nearest
		public FastVirus.List viruses; // 16 nearest

		// all fragments are ordered by mass in descending order
		public FastFragment.List fragments0; // 16 most massive
		public FastFragment.List fragments1; // 16 nearest
		public FastFragment.List fragments2; // 16 nearest
		public FastFragment.List fragments3; // 16 nearest

		public fixed int score[4];
		public int checkpointsTaken;
		public int nextCheckpoint;
		public int tick;

		public FastState(SimState simState, int nextCheckpoint) : this()
		{
			var myFragments = simState.players[simState.myId];
			if (!myFragments.Any())
				return;
			foreach (var food in simState.foods.Values.OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item))).Take(FastPoint.List.capacity))
				foods.Add(new FastPoint(food.item));
			foreach (var ejection in simState.ejections.Values.OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item))).Take(FastEjection.List.capacity))
				ejections.Add(new FastEjection(ejection.item));
			foreach (var virus in simState.viruses.Values.OrderBy(x => myFragments.Min(f => f.Value.item.QDistance(x.item))).Take(FastVirus.List.capacity))
				viruses.Add(new FastVirus(virus.item));
			foreach (var frag in myFragments.Values.OrderByDescending(x => x.item.mass).ThenByDescending(x => x.item.fragmentId).Take(FastFragment.List.capacity))
				fragments0.Add(new FastFragment(frag.item));

			fixed (FastFragment.List* l = &fragments0)
			{
				var cur = l;
				foreach (var p in simState.players.Where(x => x.Key != simState.myId))
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
			tick = simState.tick;

			// todo recover score - it may be needed for better evaluation
		}

		public void Tick(FastGlobalState* globalState, FastDirect.List* directs, Config config)
		{
			ApplyDirects(directs, config);
			MoveMoveables(config);
			PlayerEjects(directs);
			PlayerSplits(directs, config);

			if (tick % Constants.SHRINK_EVERY_TICK == 0)
				ShrinkPlayers();

			EatAll();
			FusePlayers();
			BurstOnViruses();

			UpdatePlayersRadius();
			UpdateScores();
			SplitViruses();

			UpdateNextGlobalTargets();

			tick++;
		}

		private void ApplyDirects(FastDirect.List* directs, Config config)
		{
			fixed (FastState* that = &this)
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
			fixed (FastState* that = &this)
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
			fixed (FastState* that = &this)
			{
				var direct = (FastDirect*)directs->data;
				var fragments = &that->fragments0;
				for (var p = 0; p < directs->count; p++, direct++, fragments++)
				{
					if (double.IsNaN(direct->target.x) || direct->split || !direct->eject)
						continue;
					var frag = (FastFragment*)fragments->data;
					for (var i = 0; i < fragments->count; i++, frag++)
					{
						if (that->ejections.count < FastEjection.List.capacity)
						{
							if (frag->CanEject())
							{
								var new_eject = frag->EjectNow(p);
								that->ejections.Add(new_eject);
							}
						}
					}
				}
			}
		}

		private void PlayerSplits(FastDirect.List* directs, Config config)
		{
			fixed (FastState* that = &this)
			{
				var direct = (FastDirect*)directs->data;
				var fragments = &that->fragments0;
				for (var p = 0; p < directs->count; p++, direct++, fragments++)
				{
					if (double.IsNaN(direct->target.x) || !direct->split)
						continue;
					var frag = (FastFragment*)fragments->data;

					var origFragmentsCount = fragments->count;

					for (var i = 0; i < origFragmentsCount; i++, frag++)
					{
						if (fragments->count < FastFragment.List.capacity)
						{
							if (frag->CanSplit(fragments->count, config))
							{
								var new_frag = frag->SplitNow(config);
								fragments->Add(new_frag);
							}
						}
					}

					fragments->Sort();
				}
			}
		}

		private void ShrinkPlayers()
		{
			fixed (FastState* that = &this)
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
	}
}