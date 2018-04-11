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
	public unsafe struct FastVirus
	{
		public const int size = MovingPoint.size + sizeof(double) * 3;

		static FastVirus()
		{
			if (sizeof(FastVirus) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastVirus)}) != {size}");
		}

		public FastVirus(Virus virus) : this()
		{
			point.x = virus.x;
			point.y = virus.y;
			point.angle = virus.angle;
			point.speed = virus.speed;
			radius = virus.radius;
			mass = virus.mass;
			splitAngle = virus.splitAngle;
		}

		public MovingPoint point;
		public double mass;
		public double radius;
		public double splitAngle;

		public override string ToString()
		{
			return $"{point}, M:{mass}, R:{radius}, {nameof(splitAngle)}:{splitAngle}";
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Move(Config config)
		{
			point.Move(config, radius);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastEjection* other)
		{
			var dx = point.x - other->point.x;
			var dy = point.y - other->point.y;
			return dx * dx + dy * dy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastEjection* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double CanEat(FastEjection* eject)
		{
			if (mass > Constants.EJECT_MASS * Constants.MASS_EAT_FACTOR)
			{
				var dist = Distance(eject);
				if (dist - Constants.EJECT_RADIUS + (Constants.EJECT_RADIUS * 2) * Constants.DIAM_EAT_FACTOR < radius)
					return radius - dist;
			}
			return double.NegativeInfinity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Eat(FastEjection* eject)
		{
			mass += Constants.EJECT_MASS;
			splitAngle = eject->point.angle;
		}
	}
}