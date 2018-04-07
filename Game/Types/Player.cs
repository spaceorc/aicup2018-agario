using System;
using System.Collections.Generic;
using Game.Protocol;

namespace Game.Types
{
	public class Player : Circle
	{
		public int fuse_timer;
		public bool isFast;
		public double speed;
		public double angle;
		public int score;
		public int fragmentId;
		public double visionRadius;

		public Player(int id, double x, double y, double radius, double mass, int fId, Config config)
			: base(id, x, y, radius, mass, config)
		{
			fragmentId = fId;
			if (fId > 0)
				fuse_timer = config.TICKS_TIL_FUSION;
		}

		public override string IdToString()
		{
			return fragmentId == 0 ? id.ToString() : $"{id}.{fragmentId}";
		}

		public void SetImpulse(double speed, double angle)
		{
			this.speed = Math.Abs(speed);
			this.angle = angle;
			isFast = true;
		}

		public void ApplyViscosity(double usualSpeed)
		{
			// если на этом тике не снизим скорость достаточно - летим дальше
			if (speed - config.VISCOSITY > usualSpeed)
			{
				speed -= config.VISCOSITY;
			}
			else
			{
				// иначе выставляем максимальную скорость и выходим из режима полёта
				speed = usualSpeed;
				isFast = false;
			}
		}

		public bool UpdateVision(int fragCnt)
		{
			double newVision;
			if (fragCnt == 1)
			{
				newVision = radius * Constants.VIS_FACTOR;
			}
			else
			{
				newVision = radius * Constants.VIS_FACTOR_FR * Math.Sqrt(fragCnt);
			}
			if (visionRadius != newVision)
			{
				visionRadius = newVision;
				return true;
			}
			return false;
		}

		public bool CanSee(Circle circle)
		{
			var xVisionCenter = x + Math.Cos(angle) * Constants.VIS_SHIFT;
			var yVisionCenter = y + Math.Sin(angle) * Constants.VIS_SHIFT;
			var qdist = circle.QDistance(new Point(xVisionCenter, yVisionCenter));

			var tr = visionRadius + circle.radius;
			return qdist < tr * tr;
		}

		public double CanEat(Circle food)
		{
			if (food is Player player && player.id == id)
				return double.NegativeInfinity;

			if (mass > food.mass * Constants.MASS_EAT_FACTOR)
			{
				double dist = Distance(food);
				if (dist - food.radius + (food.radius * 2) * Constants.DIAM_EAT_FACTOR < radius)
					return radius - dist;
			}

			return double.NegativeInfinity;
		}

		public void Eat(Circle food, bool isLast = false)
		{
			food.removed = true;
			mass += food.mass;
			if (food is Ejection ejection && ejection.player == id)
				return;
			if (food is Food)
				score += Constants.SCORE_FOR_FOOD;
			else if (food is Player)
				score += !isLast ? Constants.SCORE_FOR_PLAYER : Constants.SCORE_FOR_LAST;
		}

		public bool CanBurst(int yetCnt)
		{
			if (mass < Constants.MIN_BURST_MASS * 2)
			{
				return false;
			}
			int fragsCnt = (int)(mass / Constants.MIN_BURST_MASS);
			if (fragsCnt > 1 && yetCnt + 1 <= config.MAX_FRAGS_CNT)
			{
				return true;
			}
			return false;
		}

		public void BurstOn(Virus virus)
		{
			double dist = Distance(virus);
			double dy = y - virus.y, dx = x - virus.x;
			double new_angle = 0.0;

			if (dist > 0)
			{
				new_angle = Math.Asin(dy / dist);
				if (dx < 0)
				{
					new_angle = Math.PI - new_angle;
				}
			}
			angle = new_angle;
			double max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);
			if (speed < max_speed)
			{
				speed = max_speed;
			}
			mass += Constants.BURST_BONUS;
			score += Constants.SCORE_FOR_BURST;
		}

		public List<Player> BurstNow(int max_fId, int yet_cnt)
		{
			List<Player> fragments = new List<Player>();
			int new_frags_cnt = (int)(mass / Constants.MIN_BURST_MASS) - 1;
			int max_cnt = config.MAX_FRAGS_CNT - yet_cnt;
			if (new_frags_cnt > max_cnt)
			{
				new_frags_cnt = max_cnt;
			}

			double new_mass = mass / (new_frags_cnt + 1);
			double new_radius = Constants.RADIUS_FACTOR * Math.Sqrt(new_mass);

			for (int I = 0; I < new_frags_cnt; I++)
			{
				int new_fId = max_fId + I + 1;
				var new_fragment = new Player(id, x, y, new_radius, new_mass, new_fId, config);
				fragments.Add(new_fragment);

				double burst_angle = angle - Constants.BURST_ANGLE_SPECTRUM / 2 + I * Constants.BURST_ANGLE_SPECTRUM / new_frags_cnt;
				new_fragment.SetImpulse(Constants.BURST_START_SPEED, burst_angle);
			}
			SetImpulse(Constants.BURST_START_SPEED, angle + Constants.BURST_ANGLE_SPECTRUM / 2);
			
			fragmentId = max_fId + new_frags_cnt + 1;
			mass = new_mass;
			radius = new_radius;
			fuse_timer = config.TICKS_TIL_FUSION;
			return fragments;
		}

		public bool CanSplit(int yet_cnt)
		{
			if (yet_cnt + 1 <= config.MAX_FRAGS_CNT)
			{
				if (mass > Constants.MIN_SPLIT_MASS)
					return true;
			}
			return false;
		}

		public Player SplitNow(int max_fId)
		{
			double new_mass = mass / 2;
			double new_radius = Constants.RADIUS_FACTOR * Math.Sqrt(new_mass);

			var new_player = new Player(id, x, y, new_radius, new_mass, max_fId + 1, config);
			new_player.SetImpulse(Constants.SPLIT_START_SPEED, angle);

			fragmentId = max_fId + 2;
			fuse_timer = config.TICKS_TIL_FUSION;
			mass = new_mass;
			radius = new_radius;

			return new_player;
		}

		public bool CanFuse(Player frag)
		{
			double dist = Distance(frag);
			double nR = radius + frag.radius;

			return fuse_timer == 0 && frag.fuse_timer == 0 && dist <= nR;
		}

		public void CollisionCalc(Player other)
		{
			if (isFast || other.isFast)
			{ // do not collide splits
				return;
			}
			double dist = Distance(other);
			if (dist >= radius + other.radius)
			{
				return;
			}

			// vector from centers
			double collisionVectorX = this.x - other.x;
			double collisionVectorY = this.y - other.y;
			// normalize to 1
			double vectorLen = Math.Sqrt(collisionVectorX * collisionVectorX + collisionVectorY * collisionVectorY);
			if (vectorLen < 1e-9)
			{ // collision object in same point??
				return;
			}
			collisionVectorX /= vectorLen;
			collisionVectorY /= vectorLen;

			double collisionForce = 1.0 - dist / (radius + other.radius);
			collisionForce *= collisionForce;
			collisionForce *= Constants.COLLISION_POWER;

			double sumMass = mass + other.mass;
			// calc influence on us
			{
				double currPart = other.mass / sumMass; // more influence on us if other bigger and vice versa

				double dx = speed * Math.Cos(angle);
				double dy = speed * Math.Sin(angle);
				dx += collisionForce * currPart * collisionVectorX;
				dy += collisionForce * currPart * collisionVectorY;
				this.speed = Math.Sqrt(dx * dx + dy * dy);
				this.angle = Math.Atan2(dy, dx);
			}

			// calc influence on other
			{
				double otherPart = mass / sumMass;

				double dx = other.speed * Math.Cos(other.angle);
				double dy = other.speed * Math.Sin(other.angle);
				dx -= collisionForce * otherPart * collisionVectorX;
				dy -= collisionForce * otherPart * collisionVectorY;
				other.speed = Math.Sqrt(dx * dx + dy * dy);
				other.angle = Math.Atan2(dy, dx);
			}
		}

		public void Fusion(Player frag)
		{
			frag.removed = true;
			double fragDX = frag.speed * Math.Cos(frag.angle);
			double fragDY = frag.speed * Math.Sin(frag.angle);
			double dX = speed * Math.Cos(angle);
			double dY = speed * Math.Sin(angle);
			double sumMass = mass + frag.mass;

			double fragInfluence = frag.mass / sumMass;
			double currInfluence = mass / sumMass;

			// center with both parts influence
			this.x = this.x * currInfluence + frag.x * fragInfluence;
			this.y = this.y * currInfluence + frag.y * fragInfluence;

			// new move vector with both parts influence
			dX = dX * currInfluence + fragDX * fragInfluence;
			dY = dY * currInfluence + fragDY * fragInfluence;

			// new angle and speed, based on vectors
			angle = Math.Atan2(dY, dX);
			speed = Math.Sqrt(dX * dX + dY * dY);

			mass += frag.mass;
		}

		public bool CanEject()
		{
			return mass > Constants.MIN_EJECT_MASS;
		}

		public Ejection EjectNow(int eject_id)
		{
			double ex = x + Math.Cos(angle) * (radius + 1);
			double ey = y + Math.Sin(angle) * (radius + 1);

			Ejection new_eject = new Ejection(eject_id, ex, ey, this.id, config);
			new_eject.SetImpulse(Constants.EJECT_START_SPEED, angle);

			mass -= Constants.EJECT_MASS;
			radius = Constants.RADIUS_FACTOR * Math.Sqrt(mass);
			score += Constants.SCORE_FOR_EJECT;
			return new_eject;
		}

		public bool UpdateByMass()
		{
			bool changed = false;
			double new_radius = Constants.RADIUS_FACTOR * Math.Sqrt(mass);
			if (radius != new_radius)
			{
				radius = new_radius;
				changed = true;
			}

			double new_speed = config.SPEED_FACTOR / Math.Sqrt(mass);
			if (speed > new_speed && !isFast)
			{
				speed = new_speed;
			}

			if (x - radius < 0)
			{
				x += (radius - x);
				changed = true;
			}
			if (y - radius < 0)
			{
				y += (radius - y);
				changed = true;
			}
			if (x + radius > config.GAME_WIDTH)
			{
				x -= (radius + x - config.GAME_WIDTH);
				changed = true;
			}
			if (y + radius > config.GAME_HEIGHT)
			{
				y -= (radius + y - config.GAME_HEIGHT);
				changed = true;
			}

			return changed;
		}

		public void ApplyDirect(Direct direct)
		{
			direct.Limit();
			if (isFast) return;

			double speed_x = speed * Math.Cos(angle);
			double speed_y = speed * Math.Sin(angle);
			double max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);

			double dy = direct.y - y, dx = direct.x - x;
			double dist = Math.Sqrt(dx * dx + dy * dy);
			double ny = (dist > 0) ? (dy / dist) : 0;
			double nx = (dist > 0) ? (dx / dist) : 0;
			double inertion = config.INERTION_FACTOR;

			speed_x += (nx * max_speed - speed_x) * inertion / mass;
			speed_y += (ny * max_speed - speed_y) * inertion / mass;

			angle = Math.Atan2(speed_y, speed_x);

			double new_speed = Math.Sqrt(speed_x * speed_x + speed_y * speed_y);
			if (new_speed > max_speed)
			{
				new_speed = max_speed;
			}
			speed = new_speed;
		}

		public bool Move()
		{
			double rB = x + radius, lB = x - radius;
			double dB = y + radius, uB = y - radius;

			double dx = speed * Math.Cos(angle);
			double dy = speed * Math.Sin(angle);

			bool changed = false;
			if (rB + dx < config.GAME_WIDTH && lB + dx > 0)
			{
				x += dx;
				changed = true;
			}
			else
			{
				// долетаем до стенки
				double new_x = Math.Max(radius, Math.Min(config.GAME_WIDTH - radius, x + dx));
				changed |= (x != new_x);
				x = new_x;
				// зануляем проекцию скорости по dx
				double speed_y = speed * Math.Sin(angle);
				speed = Math.Abs(speed_y);
				angle = (speed_y >= 0) ? Math.PI / 2 : -Math.PI / 2;
			}
			if (dB + dy < config.GAME_HEIGHT && uB + dy > 0)
			{
				y += dy;
				changed = true;
			}
			else
			{
				// долетаем до стенки
				double new_y = Math.Max(radius, Math.Min(config.GAME_HEIGHT - radius, y + dy));
				changed |= (y != new_y);
				y = new_y;
				// зануляем проекцию скорости по dy
				double speed_x = speed * Math.Cos(angle);
				speed = Math.Abs(speed_x);
				angle = (speed_x >= 0) ? 0 : Math.PI;
			}

			if (isFast)
			{
				double max_speed = config.SPEED_FACTOR / Math.Sqrt(mass);
				ApplyViscosity(max_speed);
			}
			if (fuse_timer > 0)
			{
				fuse_timer--;
			}
			return changed;
		}

		public bool ClearFragments()
		{
			if (fragmentId == 0)
				return false;
			fragmentId = 0;
			return true;
		}

		public bool CanShrink()
		{
			return mass > Constants.MIN_SHRINK_MASS;
		}

		public void ShrinkNow()
		{
			mass -= ((mass - Constants.MIN_SHRINK_MASS) * Constants.SHRINK_FACTOR);
			radius = Constants.RADIUS_FACTOR * Math.Sqrt(mass);
		}

		public override TurnInput.ObjectData ToObjectData()
		{
			return new TurnInput.ObjectData
			{
				Id = IdToString(),
				X = x,
				Y = y,
				M = mass,
				R = radius,
				T = "P"
			};
		}

		public TurnInput.MineData ToMineData()
		{
			return new TurnInput.MineData
			{
				Id = IdToString(),
				X = x,
				Y = y,
				M = mass,
				R = radius,
				SX = speed * Math.Cos(angle),
				SY = speed * Math.Sin(angle),
				TTF = fuse_timer
			};
		}
	}
}