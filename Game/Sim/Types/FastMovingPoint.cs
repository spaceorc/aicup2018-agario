using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Protocol;

namespace Game.Sim.Types
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastMovingPoint
	{
		public const int size = sizeof(double) * 5;

		static FastMovingPoint()
		{
			if (sizeof(FastMovingPoint) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastMovingPoint)})({sizeof(FastMovingPoint)}) != {size}");
		}

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