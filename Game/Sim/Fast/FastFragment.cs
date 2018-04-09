using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastFragment
	{
		public const int size = sizeof(double) * 6;

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
		}
	}
}