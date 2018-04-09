using System;
using System.Linq;
using System.Runtime.InteropServices;
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
			foreach (var frag in myFragments.Values.OrderByDescending(x => x.item.mass).ThenBy(x => x.item.fragmentId).Take(FastFragment.List.capacity))
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
						.ThenBy(x => x.item.fragmentId))
					{
						cur->Add(new FastFragment(frag.item));
					}
				}
			}

			this.nextCheckpoint = nextCheckpoint;
			tick = simState.tick;

			// todo recover score - it may be needed for better evaluation
		}

		public void Tick(FastGlobalState* globalState, FastDirect.List* directs)
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

			UpdateNextGlobalTargets();

			tick++;
		}
	}
}