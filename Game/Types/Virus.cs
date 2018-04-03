using System;
using Game.Protocol;

namespace Game.Types
{
	public class Virus : Circle
	{
		private double speed;
		private double angle;
		private double splitAngle;

		public Virus(int id, double x, double y, double mass, Config config) : base(id, x, y, config.VIRUS_RADIUS, mass, config)
		{
		}

		public double CanHurt(Circle circle)
		{
			if (circle.radius < radius)
				return double.PositiveInfinity;
			var qdist = QDistance(circle);
			var tr = radius * Constants.RAD_HURT_FACTOR + circle.radius;
			return qdist < tr * tr ? qdist : double.PositiveInfinity;
		}

		public double CanEat(Ejection eject)
		{
			if (mass > eject.mass * Constants.MASS_EAT_FACTOR)
			{
				var dist = Distance(eject);
				if (dist - eject.radius + (eject.radius * 2) * Constants.DIAM_EAT_FACTOR < radius)
					return radius - dist;
			}
			return double.NegativeInfinity;
		}

		public void Eat(Ejection eject)
		{
			mass += eject.mass;
			splitAngle = eject.angle;
		}

		public bool CanSplit()
		{
			return mass > config.VIRUS_SPLIT_MASS;
		}

		public Virus SplitNow(int newId)
		{
			var newAngle = splitAngle;

			var newVirus = new Virus(newId, x, y, Constants.VIRUS_MASS, config);
			newVirus.SetImpulse(Constants.VIRUS_SPLIT_SPEED, newAngle);

			mass = Constants.VIRUS_MASS;
			return newVirus;
		}

		public void SetImpulse(double speed, double angle)
		{
			this.speed = Math.Abs(speed);
			this.angle = angle;
		}

		public bool Move()
		{
			if (speed == 0.0)
			{
				return false;
			}
			double dx = speed * Math.Cos(angle);
			double dy = speed * Math.Sin(angle);

			double new_x = Math.Max(radius, Math.Min(config.GAME_WIDTH - radius, x + dx));
			bool changed = (x != new_x);
			x = new_x;

			double new_y = Math.Max(radius, Math.Min(config.GAME_HEIGHT - radius, y + dy));
			changed |= (y != new_y);
			y = new_y;

			speed = Math.Max(0.0, speed - config.VISCOSITY);
			return changed;
		}

		public TurnInput.ObjectData ToObjectData()
		{
			return new TurnInput.ObjectData
			{
				Id = IdToString(),
				X = x,
				Y = y,
				M = mass,
				T = "V"
			};
		}
	}
}