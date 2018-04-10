﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Protocol;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastFragment
	{
		public const int size = sizeof(double) * 6 + sizeof(int) * 2 + sizeof(bool);

		static FastFragment()
		{
			if (sizeof(FastVirus) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastFragment)}) != {size}");
		}

		public double x;
		public double y;
		public double mass;
		public double radius;
		public double speed;
		public double angle;
		public int fuse_timer;
		public bool isFast;
		public int score;

		public FastFragment(Player player) : this()
		{
			x = player.x;
			y = player.y;
			radius = player.radius;
			mass = player.mass;
			angle = player.angle;
			speed = player.speed;
			fuse_timer = player.fuse_timer;
			isFast = player.isFast;
		}

		public override string ToString()
		{
			return $"{x},{y} => M:{mass}, R:{radius}, A:{angle}, S:{speed}, TTF:{fuse_timer}{(isFast ? ", FAST" : "")}, {nameof(score)}:{score}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyDirect(FastDirect* direct, Config config)
		{
			if (isFast)
				return;

			double speed_x = speed * Math.Cos(angle);
			double speed_y = speed * Math.Sin(angle);
			double max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);

			double dy = direct->target.y - y, dx = direct->target.x - x;
			double dist = Math.Sqrt(dx * dx + dy * dy);
			double ny = (dist > 0) ? (dy / dist) : 0;
			double nx = (dist > 0) ? (dx / dist) : 0;
			double inertion = config.INERTION_FACTOR;

			speed_x += (nx * max_speed - speed_x) * inertion / mass;
			speed_y += (ny * max_speed - speed_y) * inertion / mass;

			angle = Math.Atan2(speed_y, speed_x);

			double new_speed = Math.Sqrt(speed_x * speed_x + speed_y * speed_y);
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
					for (int i = 0; i < count - 1; i++, a++)
					{
						var b = a + 1;
						for (int k = i + 1; k < count; k++, b++)
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

			double dx = speed * Math.Cos(angle);
			double dy = speed * Math.Sin(angle);

			if (rB + dx < config.GAME_WIDTH && lB + dx > 0)
			{
				x += dx;
			}
			else
			{
				// долетаем до стенки
				x = Math.Max(radius, Math.Min(config.GAME_WIDTH - radius, x + dx));
				// зануляем проекцию скорости по dx
				double speed_y = speed * Math.Sin(angle);
				speed = Math.Abs(speed_y);
				angle = (speed_y >= 0) ? Math.PI / 2 : -Math.PI / 2;
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
				double speed_x = speed * Math.Cos(angle);
				speed = Math.Abs(speed_x);
				angle = (speed_x >= 0) ? 0 : Math.PI;
			}

			if (isFast)
			{
				double max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);
				// если на этом тике не снизим скорость достаточно - летим дальше
				if (speed - config.VISCOSITY > max_speed)
				{
					speed -= config.VISCOSITY;
				}
				else
				{
					// иначе выставляем максимальную скорость и выходим из режима полёта
					speed = max_speed;
					isFast = false;
				}
			}
			if (fuse_timer > 0)
			{
				fuse_timer--;
			}
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
			if (isFast || other->isFast)
			{ // do not collide splits
				return;
			}
			double dist = Distance(other);
			if (dist >= radius + other->radius)
			{
				return;
			}

			// vector from centers
			double collisionVectorX = x - other->x;
			double collisionVectorY = y - other->y;
			// normalize to 1
			double vectorLen = Math.Sqrt(collisionVectorX * collisionVectorX + collisionVectorY * collisionVectorY);
			if (vectorLen < 1e-9)
			{ // collision object in same point??
				return;
			}
			collisionVectorX /= vectorLen;
			collisionVectorY /= vectorLen;

			double collisionForce = 1.0 - dist / (radius + other->radius);
			collisionForce *= collisionForce;
			collisionForce *= Constants.COLLISION_POWER;

			double sumMass = mass + other->mass;
			// calc influence on us
			{
				double currPart = other->mass / sumMass; // more influence on us if other bigger and vice versa

				double dx = speed * Math.Cos(angle);
				double dy = speed * Math.Sin(angle);
				dx += collisionForce * currPart * collisionVectorX;
				dy += collisionForce * currPart * collisionVectorY;
				speed = Math.Sqrt(dx * dx + dy * dy);
				angle = Math.Atan2(dy, dx);
			}

			// calc influence on other
			{
				double otherPart = mass / sumMass;

				double dx = other->speed * Math.Cos(other->angle);
				double dy = other->speed * Math.Sin(other->angle);
				dx -= collisionForce * otherPart * collisionVectorX;
				dy -= collisionForce * otherPart * collisionVectorY;
				other->speed = Math.Sqrt(dx * dx + dy * dy);
				other->angle = Math.Atan2(dy, dx);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ShrinkNow()
		{
			mass -= ((mass - Constants.MIN_SHRINK_MASS) * Constants.SHRINK_FACTOR);
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
			double new_mass = mass / 2;
			double new_radius = Mass2Radius(new_mass);

			var new_player = new FastFragment
			{
				x = x,
				y = y,
				radius = new_radius,
				mass = new_mass,
				speed = Constants.SPLIT_START_SPEED,
				angle = angle,
				isFast = true
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
		private static int RestFragmentsCount(int existingFragmentsCount, Config config)
		{
			return config.MAX_FRAGS_CNT - existingFragmentsCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FastEjection EjectNow(int player)
		{
			double ex = x + Math.Cos(angle) * (radius + 1);
			double ey = y + Math.Sin(angle) * (radius + 1);

			var new_eject = new FastEjection
			{
				point =
				{
					x = ex,
					y = ey,
					speed = Constants.EJECT_START_SPEED,
					angle = angle
				},
				player = player
			};
			
			mass -= Constants.EJECT_MASS;
			radius = Mass2Radius(mass);
			score += Constants.SCORE_FOR_EJECT;
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