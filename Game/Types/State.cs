using System;
using System.Collections.Generic;
using System.Linq;
using Game.Helpers;
using Game.Protocol;

namespace Game.Types
{
	public class State
	{
		private const int FOOD_STUCK_TICKS = 10;

		private readonly Config config;
		private readonly bool supportFoodStuckEscape;
		public readonly Dictionary<int, Actual<Ejection>> ejections = new Dictionary<int, Actual<Ejection>>();
		public readonly Dictionary<int, Actual<Food>> foods = new Dictionary<int, Actual<Food>>();

		public readonly Dictionary<int, Dictionary<int, Actual<Player>>> players =
			new Dictionary<int, Dictionary<int, Actual<Player>>>();

		public readonly Dictionary<int, Actual<Virus>> viruses = new Dictionary<int, Actual<Virus>>();

		private bool enableFoodBan;
		public int myId;
		private double stuckDist;
		private int stuckFoodHash;
		private int stuckTicks;
		public int tick;

		public State(Config config, bool supportFoodStuckEscape)
		{
			this.config = config;
			this.supportFoodStuckEscape = supportFoodStuckEscape;
		}

		public void Apply(TurnInput turn)
		{
			tick++;
			foreach (var mine in turn.Mine)
				AddOrUpdateMine(mine);
			foreach (var obj in turn.Objects)
				AddOrUpdateObj(obj);
			RemoveNonActual();
		}

		private void RemoveNonActual()
		{
			// Remove obsolete ally fragments
			var fragments = players[myId];
			foreach (var kvp in fragments.ToList())
				if (kvp.Value.tick != tick)
					fragments.Remove(kvp);
			// Update ally fragments vision
			foreach (var fragment in fragments.Values)
				fragment.item.UpdateVision(fragments.Count);

			// Remove obsolete food
			foreach (var kvp in foods.ToList())
				if (kvp.Value.tick != tick)
				{
					if (!fragments.Any(f => f.Value.item.CanSee(kvp.Value.item)))
						if (kvp.Value.tick >= tick - Settings.FOOD_FORGET_TICKS)
							continue;

					foods.Remove(kvp);
				}

			// Remove obsolete viruses
			foreach (var kvp in viruses.ToList())
				if (kvp.Value.tick != tick)
					viruses.Remove(kvp);

			// todo don't forget just fogged ejection
			foreach (var kvp in ejections.ToList())
				if (kvp.Value.tick != tick)
					ejections.Remove(kvp);

			// Remove obsolete enemies
			foreach (var kvp in players.Where(x => x.Key != myId))
			foreach (var kvpp in kvp.Value.ToList())
				if (kvpp.Value.tick != tick)
				{
					if (!fragments.Any(f => f.Value.item.CanSee(kvpp.Value.item)))
						if (kvpp.Value.tick >= tick - Settings.ENEMY_FORGET_TICKS)
						{
							if (kvpp.Value.item.fuse_timer > 0)
								kvpp.Value.item.fuse_timer--;
							continue;
						}

					kvp.Value.Remove(kvpp);
				}

			// fix food stuck
			if (supportFoodStuckEscape)
			{
				if (fragments.Count > 0 && foods.Count >= 2)
				{
					var twoNearestFoods = foods
						.Select(x => new {x.Key, qdist = fragments.Min(f => f.Value.item.QDistance(x.Value.item)), food = x.Value})
						.OrderBy(x => x.qdist)
						.Take(2)
						.OrderBy(x => x.Key)
						.ToList();

					var foodHash = twoNearestFoods[0].Key + twoNearestFoods[1].Key;
					if (foodHash != stuckFoodHash)
					{
						stuckTicks = 0;
						stuckDist = 0;
						enableFoodBan = false;
					}
					else if (!enableFoodBan)
					{
						var dist = Math.Sqrt(twoNearestFoods[0].qdist) + Math.Sqrt(twoNearestFoods[1].qdist);
						stuckTicks++;
						if (stuckTicks == 1)
						{
							stuckDist = dist;
						}
						else if (Math.Abs(dist - stuckDist) > stuckDist * 0.2)
						{
							stuckTicks = 0;
							stuckDist = 0;
						}
						else if (stuckTicks >= FOOD_STUCK_TICKS)
						{
							stuckTicks = 0;
							stuckDist = 0;
							enableFoodBan = true;
							twoNearestFoods[0].food.ban = true;
							Logger.Info($"Banned food: {twoNearestFoods[0].food.item}");
						}
					}

					stuckFoodHash = foodHash;
				}
				else
				{
					stuckFoodHash = 0;
					stuckDist = 0;
					enableFoodBan = false;
				}

				if (!enableFoodBan)
					foreach (var food in foods)
						food.Value.ban = false;
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
			act.tick = tick;

			var max_speed = config.SPEED_FACTOR / Math.Sqrt(mine.M);
			var speed = Math.Sqrt(mine.SX * mine.SX + mine.SY * mine.SY);
			var isFast = speed > max_speed;

			act.item = new Player(id, mine.X, mine.Y, mine.R, mine.M, fragmentId, config)
			{
				isFast = isFast,
				angle = Math.Atan2(mine.SY, mine.SX),
				speed = speed,
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
			act.tick = tick;
			act.item = new Food(id, obj.X, obj.Y, config);
		}

		private void AddOrUpdateEjection(TurnInput.ObjectData obj)
		{
			var id = int.Parse(obj.Id);
			var pid = int.Parse(obj.pId);
			var act = ejections.GetOrAdd(id);
			act.tick = tick;
			act.item = new Ejection(id, obj.X, obj.Y, pid, config); // todo for mine ejections: correct angle and speed
		}

		private void AddOrUpdateVirus(TurnInput.ObjectData obj)
		{
			var id = int.Parse(obj.Id);
			var act = viruses.GetOrAdd(id);
			act.tick = tick;
			act.item = new Virus(id, obj.X, obj.Y, obj.M, config); // todo for viruses: correct angle and speed
		}

		private void AddOrUpdatePlayer(TurnInput.ObjectData obj)
		{
			var ids = obj.Id.Split('.');
			var id = int.Parse(ids[0]);
			var fragmentId = ids.Length == 1 ? 0 : int.Parse(ids[1]);
			var fragments = players.GetOrAdd(id);
			var act = fragments.GetOrAdd(fragmentId);
			act.tick = tick;
			var prev = act.item;
			act.item = new Player(id, obj.X, obj.Y, obj.R, obj.M, fragmentId, config)
			{
				fuse_timer = config.TICKS_TIL_FUSION
			};
			if (prev != null)
			{
				var speedx = obj.X - prev.x;
				var speedy = obj.Y - prev.y;
				act.item.angle = Math.Atan2(speedy, speedx);
				act.item.speed = Math.Sqrt(speedx * speedx + speedy * speedy);
				var max_speed = config.SPEED_FACTOR / Math.Sqrt(act.item.mass);
				act.item.isFast = act.item.speed > max_speed;
				if (act.item.isFast)
					act.item.ApplyViscosity(max_speed);
				act.item.fuse_timer = prev.fuse_timer > 0 ? prev.fuse_timer - 1 : 0;
			}
		}

		public class Actual<T>
		{
			public bool ban;
			public T item;
			public int tick;
		}
	}
}