using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastVirus
	{
		public const int size = sizeof(double) * 7;

		static FastVirus()
		{
			if (sizeof(FastVirus) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastVirus)}) != {size}");
		}

		public FastVirus(Virus virus) : this()
		{
			x = virus.x;
			y = virus.y;
			radius = virus.radius;
			mass = virus.mass;
			angle = virus.angle;
			splitAngle = virus.splitAngle;
			speed = virus.speed;
		}
		
		public double x;
		public double y;
		public double mass;
		public double radius;
		public double speed;
		public double angle;
		public double splitAngle;

		public override string ToString()
		{
			return $"{x},{y} => M:{mass}, R:{radius}, A:{angle}, S:{speed}, {nameof(splitAngle)}:{splitAngle}";
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct List
		{
			public const int capacity = 16;
			public byte count;
			public fixed byte data[capacity * size];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(FastVirus item)
			{
				fixed (byte* d = data)
				{
					((FastVirus*)d)[count++] = item;
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
						result.Append(((FastVirus*)d)[i]);
					}
					return result.ToString();
				}
			}
		}
	}
}