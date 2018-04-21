using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Protocol;
using Game.Types;

namespace Game.Sim.Types
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastFragment
	{
		public const int size = sizeof(double) * 7 + sizeof(int) * 2;

		static FastFragment()
		{
			if (sizeof(FastFragment) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastFragment)})({sizeof(FastFragment)}) != {size}");
		}

		public double x;
		public double y;
		public double mass;
		public double radius;
		public double speed;
		public double ndx;
		public double ndy;
		public int fuse_timer;
		public int isFast;
		private const double RADIUS_EAT_FACTOR = 2 * Constants.DIAM_EAT_FACTOR - 1;

		public FastFragment(Player player) : this()
		{
			x = player.x;
			y = player.y;
			radius = player.radius;
			mass = player.mass;
			ndx = Math.Cos(player.angle);
			ndy = Math.Sin(player.angle);
			speed = player.speed;
			fuse_timer = player.fuse_timer;
			isFast = player.isFast ? 1 : 0;
		}

		public override string ToString()
		{
			return $"{x},{y} => M:{mass}, R:{radius}, A:{Math.Atan2(ndy, ndx)}, S:{speed}, TTF:{fuse_timer}{(isFast == 1 ? ", FAST" : "")}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyDirect(FastDirect* direct, Config config)
		{
			if (isFast == 1)
				return;

			var speed_x = speed * ndx;
			var speed_y = speed * ndy;
			var max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);

			double dy = direct->target.y - y, dx = direct->target.x - x;
			var dist = Math.Sqrt(dx * dx + dy * dy);
			var ny = dist > 0 ? dy / dist : 0;
			var nx = dist > 0 ? dx / dist : 0;
			var inertion = config.INERTION_FACTOR;

			speed_x += (nx * max_speed - speed_x) * inertion / mass;
			speed_y += (ny * max_speed - speed_y) * inertion / mass;


			var new_speed = Math.Sqrt(speed_x * speed_x + speed_y * speed_y);
			ndx = speed_x / new_speed;
			ndy = speed_y / new_speed;

			if (new_speed > max_speed)
				new_speed = max_speed;

			speed = new_speed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct List
		{
			public const int capacity = 16;
			public byte count;
			public fixed byte data[capacity * size];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(FastFragment item)
			{
				fixed (byte* d = data)
				{
					((FastFragment*)d)[count++] = item;
				}
			}

			public override string ToString()
			{
				fixed (byte* d = data)
				{
					var result = new StringBuilder();
					foreach (var i in Enumerable.Range(0, count))
					{
						if (result.Length > 0)
							result.Append('|');
						result.Append(((FastFragment*)d)[i]);
					}
					return result.ToString();
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Sort()
			{
				fixed (byte* d = data)
				{
					var a = (FastFragment*) d;
					for (var i = 0; i < count - 1; i++, a++)
					{
						var b = a + 1;
						for (var k = i + 1; k < count; k++, b++)
						{
							var cmp = Compare(a, b);
							if (cmp > 0)
							{
								var tmp = *a;
								*a = *b;
								*b = tmp;
							}
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Compare(FastFragment* a, FastFragment* b)
		{
			if (a->mass > b->mass)
				return -1;
			if (a->mass < b->mass)
				return 1;
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Move(Config config)
		{
			double rB = x + radius, lB = x - radius;
			double dB = y + radius, uB = y - radius;

			var dx = speed * ndx;
			var dy = speed * ndy;

			if (rB + dx < config.GAME_WIDTH && lB + dx > 0)
			{
				x += dx;
			}
			else
			{
				// долетаем до стенки
				x = Math.Max(radius, Math.Min(config.GAME_WIDTH - radius, x + dx));
				// зануляем проекцию скорости по dx
				var speed_y = speed * ndy;
				speed = Math.Abs(speed_y);
				ndx = 0;
				ndy = speed_y >= 0 ? 1 : -1;
			}
			if (dB + dy < config.GAME_HEIGHT && uB + dy > 0)
			{
				y += dy;
			}
			else
			{
				// долетаем до стенки
				y = Math.Max(radius, Math.Min(config.GAME_HEIGHT - radius, y + dy));
				// зануляем проекцию скорости по dy
				var speed_x = speed * ndx;
				speed = Math.Abs(speed_x);
				ndy = 0;
				ndx = speed_x >= 0 ? 1 : -1;
			}

			if (isFast == 1)
			{
				var max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);
				// если на этом тике не снизим скорость достаточно - летим дальше
				if (speed - config.VISCOSITY > max_speed)
				{
					speed -= config.VISCOSITY;
				}
				else
				{
					// иначе выставляем максимальную скорость и выходим из режима полёта
					speed = max_speed;
					isFast = 0;
				}
			}
			if (fuse_timer > 0)
			{
				fuse_timer--;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastEjection* other)
		{
			var dx = x - other->point.x;
			var dy = y - other->point.y;
			return dx * dx + dy * dy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastEjection* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastPoint* other)
		{
			var dx = x - other->x;
			var dy = y - other->y;
			return dx * dx + dy * dy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastPoint* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastVirus* other)
		{
			var dx = x - other->point.x;
			var dy = y - other->point.y;
			return dx * dx + dy * dy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastVirus* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastFragment* other)
		{
			var dx = x - other->x;
			var dy = y - other->y;
			return dx * dx + dy * dy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastFragment* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CollisionCalc(FastFragment* other)
		{
			if (isFast == 1 || other->isFast == 1)
			{ // do not collide splits
				return;
			}

			var nr = radius + other->radius;
			// vector from centers
			var collisionVectorX = x - other->x;
			var collisionVectorY = y - other->y;
			if (Math.Abs(collisionVectorX) >= nr || Math.Abs(collisionVectorY) >= nr)
				return;

			var qdist = collisionVectorX * collisionVectorX + collisionVectorY * collisionVectorY;
			if (qdist >= nr * nr)
				return;

			// normalize to 1
			const double eps = 1e-9*1e-9;
			if (qdist < eps)
			{ // collision object in same point??
				return;
			}

			var dist = Math.Sqrt(qdist);

			collisionVectorX /= dist;
			collisionVectorY /= dist;

			var collisionForce = 1.0 - dist / nr;
			collisionForce *= collisionForce;
			collisionForce *= Constants.COLLISION_POWER;

			var sumMass = mass + other->mass;
			// calc influence on us
			{
				var currPart = other->mass / sumMass; // more influence on us if other bigger and vice versa

				var dx = speed * ndx;
				var dy = speed * ndy;
				dx += collisionForce * currPart * collisionVectorX;
				dy += collisionForce * currPart * collisionVectorY;
				speed = Math.Sqrt(dx * dx + dy * dy);
				ndx = dx / speed;
				ndy = dy / speed;
			}

			// calc influence on other
			{
				var otherPart = mass / sumMass;

				var dx = other->speed * other->ndx;
				var dy = other->speed * other->ndy;
				dx -= collisionForce * otherPart * collisionVectorX;
				dy -= collisionForce * otherPart * collisionVectorY;
				other->speed = Math.Sqrt(dx * dx + dy * dy);
				other->ndx = dx / other->speed;
				other->ndy = dy / other->speed;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Eatable(FastFragment* frag)
		{
			return mass > frag->mass * Constants.MASS_EAT_FACTOR;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool EatableBySplit(FastFragment* frag)
		{
			return mass / 2 > frag->mass * Constants.MASS_EAT_FACTOR;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double CanEat(FastFragment* frag)
		{
			if (Eatable(frag))
			{
				var qdist = QDistance(frag);
				var nr = radius - frag->radius * RADIUS_EAT_FACTOR;
				if (qdist < nr * nr)
					return radius - Math.Sqrt(qdist);
			}

			return double.NegativeInfinity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double CanEat(FastPoint* food, Config config)
		{
			if (mass > config.FOOD_MASS * Constants.MASS_EAT_FACTOR)
			{
				var qdist = QDistance(food);
				var nr = radius - Constants.FOOD_RADIUS * RADIUS_EAT_FACTOR;
				if (qdist < nr * nr)
					return radius - Math.Sqrt(qdist);
			}

			return double.NegativeInfinity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double CanEat(FastEjection* eject)
		{
			if (mass > Constants.EJECT_MASS * Constants.MASS_EAT_FACTOR)
			{
				var qdist = QDistance(eject);
				var nr = radius - Constants.EJECT_RADIUS * RADIUS_EAT_FACTOR;
				if (qdist < nr * nr)
					return radius - Math.Sqrt(qdist);
			}

			return double.NegativeInfinity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Eat(FastPoint* food, Config config)
		{
			mass += config.FOOD_MASS;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Eat(FastEjection* eject)
		{
			mass += Constants.EJECT_MASS;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Eat(FastFragment* frag)
		{
			mass += frag->mass;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ShrinkNow()
		{
			mass -= (mass - Constants.MIN_SHRINK_MASS) * Constants.SHRINK_FACTOR;
			radius = Mass2Radius(mass);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanShrink()
		{
			return mass > Constants.MIN_SHRINK_MASS;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FastFragment SplitNow(Config config)
		{
			var new_mass = mass / 2;
			var new_radius = Mass2Radius(new_mass);

			var new_player = new FastFragment
			{
				x = x,
				y = y,
				radius = new_radius,
				mass = new_mass,
				speed = Constants.SPLIT_START_SPEED,
				ndx = ndx,
				ndy = ndy,
				isFast = 1,
				fuse_timer = config.TICKS_TIL_FUSION
			};
			
			fuse_timer = config.TICKS_TIL_FUSION;
			mass = new_mass;
			radius = new_radius;

			return new_player;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanSplit(int yet_cnt, Config config)
		{
			if (RestFragmentsCount(yet_cnt, config) > 0)
			{
				if (mass > Constants.MIN_SPLIT_MASS)
					return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanFuse(FastFragment* frag)
		{
			if (fuse_timer != 0 || frag->fuse_timer != 0)
				return false;
			var qdist = QDistance(frag);
			var nR = radius + frag->radius;
			return qdist <= nR * nR;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Fusion(FastFragment* frag)
		{
			var fragDX = frag->speed * frag->ndx;
			var fragDY = frag->speed * frag->ndy;
			var dX = speed * ndx;
			var dY = speed * ndy;
			var sumMass = mass + frag->mass;

			var fragInfluence = frag->mass / sumMass;
			var currInfluence = mass / sumMass;

			// center with both parts influence
			x = x * currInfluence + frag->x * fragInfluence;
			y = y * currInfluence + frag->y * fragInfluence;

			// new move vector with both parts influence
			dX = dX * currInfluence + fragDX * fragInfluence;
			dY = dY * currInfluence + fragDY * fragInfluence;

			// new angle and speed, based on vectors
			speed = Math.Sqrt(dX * dX + dY * dY);
			ndx = dX / speed;
			ndy = dY / speed;

			mass += frag->mass;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int RestFragmentsCount(int existingFragmentsCount, Config config)
		{
			return config.MAX_FRAGS_CNT - existingFragmentsCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanBurst(byte yet_cnt, Config config)
		{
			if (mass < Constants.MIN_BURST_MASS * 2)
				return false;

			var fragsCnt = (int)(mass / Constants.MIN_BURST_MASS);
			if (fragsCnt > 1 && RestFragmentsCount(yet_cnt, config) > 0)
				return true;

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdateByMass(Config config)
		{
			var mass_sq = Math.Sqrt(mass);

			radius = Constants.PLAYER_RADIUS_FACTOR * mass_sq;

			var new_speed = config.SPEED_FACTOR / mass_sq;
			if (speed > new_speed && isFast == 0)
				speed = new_speed;

			if (x - radius < 0) x += radius - x;
			if (y - radius < 0) y += radius - y;
			if (x + radius > config.GAME_WIDTH) x -= radius + x - config.GAME_WIDTH;
			if (y + radius > config.GAME_HEIGHT) y -= radius + y - config.GAME_HEIGHT;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void BurstNow(List* fragments, Config config)
		{
			var new_frags_cnt = (int)(mass / Constants.MIN_BURST_MASS) - 1;

			new_frags_cnt = Math.Min(new_frags_cnt, RestFragmentsCount(fragments->count, config));

			var new_mass = mass / (new_frags_cnt + 1);
			var new_radius = Mass2Radius(new_mass);

			var angle = Math.Atan2(ndy, ndx);
			for (var I = 0; I < new_frags_cnt; I++)
			{
				var burst_angle = angle - Constants.BURST_ANGLE_SPECTRUM / 2 + I * Constants.BURST_ANGLE_SPECTRUM / new_frags_cnt;
				var new_fragment = new FastFragment
				{
					x = x,
					y = y,
					radius = new_radius,
					mass = new_mass,
					isFast = 1,
					speed = Constants.BURST_START_SPEED,
					ndx = Math.Cos(burst_angle),
					ndy = Math.Sin(burst_angle),
					fuse_timer = config.TICKS_TIL_FUSION
				};
				if (fragments->count < List.capacity)
					fragments->Add(new_fragment);
			}

			isFast = 1;
			speed = Constants.BURST_START_SPEED;
			angle = angle + Constants.BURST_ANGLE_SPECTRUM / 2;
			ndx = Math.Cos(angle);
			ndy = Math.Sin(angle);
			mass = new_mass;
			radius = new_radius;
			fuse_timer = config.TICKS_TIL_FUSION;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void BurstOn(FastVirus* virus, Config config)
		{
			var dist = Distance(virus);
			double dy = y - virus->point.y, dx = x - virus->point.x;
			
			if (dist > 0)
			{
				ndx = dx / dist;
				ndy = dy / dist;
			}
			else
			{
				ndx = 1;
				ndy = 0;
			}
			
			var max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);
			if (speed < max_speed)
				speed = max_speed;

			mass += Constants.BURST_BONUS;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FastEjection EjectNow(int player)
		{
			var ex = x + ndx * (radius + 1);
			var ey = y + ndy * (radius + 1);

			var new_eject = new FastEjection
			{
				point =
				{
					x = ex,
					y = ey,
					speed = Constants.EJECT_START_SPEED,
					ndx = ndx,
					ndy = ndy
				},
				player = player
			};
			
			mass -= Constants.EJECT_MASS;
			radius = Mass2Radius(mass);
			return new_eject;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanEject()
		{
			return mass > Constants.MIN_EJECT_MASS;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double Mass2Radius(double mass)
		{
			return Constants.PLAYER_RADIUS_FACTOR * Math.Sqrt(mass);
		}
	}
}