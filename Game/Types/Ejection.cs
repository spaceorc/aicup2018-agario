using System;
using Game.Protocol;

namespace Game.Types
{
	public class Ejection : Circle
	{
		public double speed;
		public double angle;
		public int player;

		public Ejection(int id, double x, double y, int player, Config config) : base(id, x, y, Constants.EJECT_RADIUS, Constants.EJECT_MASS, config)
		{
			speed = 0;
			angle = 0;
			this.player = player;
		}

		public void SetImpulse(double newSpeed, double newAngle)
		{
			speed = Math.Abs(newSpeed);
			angle = newAngle;
		}

		public bool Move()
		{
			if (speed == 0.0)
				return false;

			var dx = speed * Math.Cos(angle);
			var dy = speed * Math.Sin(angle);

			var new_x = Math.Max(radius, Math.Min(config.GAME_WIDTH - radius, x + dx));
			var changed = (x != new_x);
			x = new_x;

			var new_y = Math.Max(radius, Math.Min(config.GAME_HEIGHT - radius, y + dy));
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
				T = "E",
				pId = player.ToString()
			};
		}
	}
}