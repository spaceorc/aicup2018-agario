using System;
using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace Game.Sim
{
	public class SimState
	{
		public class Actual<T>
		{
			public T item;
			public int actual;
		}

		private readonly Config config;
		public int myId;
		public int actual;
		public readonly Dictionary<int, Actual<Food>> foods = new Dictionary<int, Actual<Food>>();
		public readonly Dictionary<int, Actual<Virus>> viruses = new Dictionary<int, Actual<Virus>>();
		public readonly Dictionary<int, Actual<Ejection>> ejections = new Dictionary<int, Actual<Ejection>>();
		public readonly Dictionary<int, Dictionary<int, Actual<Player>>> players = new Dictionary<int, Dictionary<int, Actual<Player>>>();

		public SimState(Config config)
		{
			this.config = config;
		}

		public void Apply(TurnInput turn)
		{
			actual++;
			foreach (var mine in turn.Mine)
				AddOrUpdateMine(mine);
			foreach (var obj in turn.Objects)
				AddOrUpdateObj(obj);
			RemoveNonActual();
		}

		private void RemoveNonActual()
		{
			// todo don't forget just fogged food
			foreach (var kvp in foods.ToList())
			{
				if (kvp.Value.actual != actual)
					foods.Remove(kvp);
			}
			foreach (var kvp in viruses.ToList())
			{
				if (kvp.Value.actual != actual)
					viruses.Remove(kvp);
			}
			// todo don't forget just fogged ejection
			foreach (var kvp in ejections.ToList())
			{
				if (kvp.Value.actual != actual)
					ejections.Remove(kvp);
			}
			// todo don't forget just fogged enemy
			foreach (var kvp in players)
			{
				foreach (var kvpp in kvp.Value.ToList())
				{
					if (kvpp.Value.actual != actual)
						kvp.Value.Remove(kvpp);
				}
			}
		}

		private void AddOrUpdateMine(TurnInput.MineData mine)
		{
			var ids = mine.Id.Split('.');
			var id = int.Parse(ids[0]);
			var fragmentId = ids.Length == 1 ? 0 : int.Parse(ids[1]);
			myId = id;
			var fragments = players.GetOrAdd(id);
			var act = fragments.GetOrAdd(fragmentId);
			act.actual = actual;
			act.item = new Player(id, mine.X, mine.Y, mine.R, mine.M, fragmentId, config)
			{
				angle = Math.Atan2(mine.SY, mine.SX),
				speed = Math.Sqrt(mine.SX * mine.SX + mine.SY * mine.SY),
				fuse_timer = mine.TTF
			};
		}

		private void AddOrUpdateObj(TurnInput.ObjectData obj)
		{
			switch (obj.T)
			{
				case "F":
					AddOrUpdateFood(obj);
					break;
				case "E":
					AddOrUpdateEjection(obj);
					break;
				case "V":
					AddOrUpdateVirus(obj);
					break;
				case "P":
					AddOrUpdatePlayer(obj);
					break;
			}
		}

		private void AddOrUpdateFood(TurnInput.ObjectData obj)
		{
			var id = (int) obj.Y * config.GAME_WIDTH + (int) obj.X;
			var act = foods.GetOrAdd(id);
			act.actual = actual;
			act.item = new Food(id, obj.X, obj.Y, config);
		}

		private void AddOrUpdateEjection(TurnInput.ObjectData obj)
		{
			var id = (int) obj.Y * config.GAME_WIDTH + (int) obj.X;
			var pid = int.Parse(obj.pId);
			var act = ejections.GetOrAdd(id);
			act.actual = actual;
			act.item = new Ejection(id, obj.X, obj.Y, pid, config); // todo for mine ejections: correct angle and speed
		}

		private void AddOrUpdateVirus(TurnInput.ObjectData obj)
		{
			var id = int.Parse(obj.Id);
			var act = viruses.GetOrAdd(id);
			act.actual = actual;
			act.item = new Virus(id, obj.X, obj.Y, obj.M, config); // todo for viruses: correct angle and speed
		}

		private void AddOrUpdatePlayer(TurnInput.ObjectData obj)
		{
			var ids = obj.Id.Split('.');
			var id = int.Parse(ids[0]);
			var fragmentId = ids.Length == 1 ? 0 : int.Parse(ids[1]);
			var fragments = players.GetOrAdd(id);
			var act = fragments.GetOrAdd(fragmentId);
			act.actual = actual;
			act.item = new Player(id, obj.X, obj.Y, obj.R, obj.M, fragmentId, config)
			{
				fuse_timer = 0 // todo for enemies: angle, speed, isFast, fuse_timer
			};
		}
	}
}