using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Protocol;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastEjection
	{
		public const int size = sizeof(double) * 4 + sizeof(int);

		static FastEjection()
		{
			if (sizeof(FastEjection) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastEjection)}) != {size}");
		}

		public FastEjection(Ejection ejection) : this()
		{
			point.x = ejection.x;
			point.y = ejection.y;
			point.SetAngle(ejection.angle);
			point.speed = ejection.speed;
			player = ejection.player;
		}

		public MovingPoint point;
		public int player;

		public override string ToString()
		{
			return $"{point}, {nameof(player)}:{player}";
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct List
		{
			public const int capacity = 16;
			public byte count;
			public fixed byte data[capacity * size];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(FastEjection item)
			{
				fixed (byte* d = data)
				{
					((FastEjection*)d)[count++] = item;
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
						result.Append(((FastEjection*)d)[i]);
					}
					return result.ToString();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Move(Config config)
		{
			point.Move(config, Constants.EJECT_RADIUS);
		}
	}
}