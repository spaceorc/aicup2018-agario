using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Game.Types;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastPoint
	{
		public const int size = sizeof(double) * 2;

		static FastPoint()
		{
			if (sizeof(FastPoint) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastPoint)})({sizeof(FastPoint)}) != {size}");
		}

		public double x;
		public double y;

		public FastPoint(double x, double y) : this()
		{
			this.x = x;
			this.y = y;
		}

		public FastPoint(Point point) : this(point.x, point.y)
		{
		}

		public override string ToString() => $"{x},{y}";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastPoint* other)
		{
			var dx = x - other->x;
			var dy = y - other->y;
			return dx * dx + dy * dy;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastPoint* other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double QDistance(FastPoint other)
		{
			return QDistance(&other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(FastPoint other)
		{
			return Math.Sqrt(QDistance(other));
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct List
		{
			public const int capacity = 16;
			public byte count;
			public fixed byte data[capacity * size];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(FastPoint item)
			{
				fixed (byte* d = data)
				{
					((FastPoint*)d)[count++] = item;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(double x, double y)
			{
				Add(new FastPoint(x, y));
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
						result.Append(((FastPoint*) d)[i]);
					}
					return result.ToString();
				}
			}
		}
	}
}