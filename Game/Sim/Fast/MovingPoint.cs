using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Protocol;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct MovingPoint
	{
		public const int size = sizeof(double) * 4;

		public double x;
		public double y;
		public double angle;
		public double speed;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Move(Config config, double radius)
		{
			if (speed == 0)
				return;

			var dx = speed * Math.Cos(angle);
			var dy = speed * Math.Sin(angle);

			x += dx;
			y += dy;
			if (x < radius) x = radius;
			if (x > config.GAME_WIDTH - radius) x = config.GAME_WIDTH - radius;
			if (y < radius) y = radius;
			if (y > config.GAME_HEIGHT - radius) y = config.GAME_HEIGHT - radius;

			speed -= config.VISCOSITY;
			if (speed < 0)
				speed = 0;
		}


		public override string ToString()
		{
			return $"{x},{y} => A:{angle}, S:{speed}";
		}
	}
}