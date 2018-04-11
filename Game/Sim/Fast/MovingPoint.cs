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
		public const int size = sizeof(double) * 5;

		public double x;
		public double y;
		public double ndx;
		public double ndy;
		public double speed;

		public void SetAngle(double angle)
		{
			ndx = Math.Cos(angle);
			ndy = Math.Sin(angle);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Move(Config config, double radius)
		{
			if (speed == 0)
				return;

			var dx = speed * ndx;
			var dy = speed * ndy;

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
			return $"{x},{y} => A:{Math.Atan2(ndy, ndx)}, S:{speed}";
		}
	}
}