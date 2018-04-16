using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Protocol;
using Game.Types;

namespace Game.Sim.Types
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastVirus
	{
		public const int size = FastMovingPoint.size + sizeof(double) * 4;

		static FastVirus()
		{
			if (sizeof(FastVirus) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastVirus)})({sizeof(FastVirus)}) != {size}");
		}

		public FastVirus(Virus virus) : this()
		{
			point.x = virus.x;
			point.y = virus.y;
			point.SetAngle(virus.angle);
			point.speed = virus.speed;
			radius = virus.radius;
			mass = virus.mass;
			splitNdx = Math.Cos(virus.splitAngle);
			splitNdy = Math.Sin(virus.splitAngle);
		}

		public FastMovingPoint point;
		public double mass;
		public double radius;
		public double splitNdx;
		public double splitNdy;

		public override string ToString()
		{
			return $"{point}, M:{mass}, R:{radius}, splitAngle:{Math.Atan2(splitNdy, splitNdx)}";
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
		public double Distance(FastFragment* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastFragment* other)
		{
			var dx = point.x - other->x;
			var dy = point.y - other->y;
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
			splitNdx = eject->point.ndx;
			splitNdy = eject->point.ndy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double CanHurt(FastFragment* frag)
		{
			if (frag->radius < radius)
				return double.PositiveInfinity;
			var qdist = QDistance(frag);
			var tr = radius * Constants.RAD_HURT_FACTOR + frag->radius;
			return qdist < tr * tr ? qdist : double.PositiveInfinity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanSplit(Config config)
		{
			return mass > config.VIRUS_SPLIT_MASS;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FastVirus SplitNow()
		{
			var newVirus = new FastVirus
			{
				point = {x = point.x, y = point.y, ndx = splitNdx, ndy = splitNdy, speed = Constants.VIRUS_SPLIT_SPEED},
				mass = Constants.VIRUS_MASS
			};
			mass = Constants.VIRUS_MASS;
			return newVirus;
		}
	}
}