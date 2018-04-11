using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastDirect
	{
		public const int size = FastPoint.size + 2 * sizeof(int);

		static FastDirect()
		{
			if (sizeof(FastDirect) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastDirect)})({sizeof(FastDirect)}) != {size}");
		}

		public FastDirect(double x, double y, bool split = false, bool eject = false)
		{
			target.x = x;
			target.y = y;
			this.split = split ? 1 : 0;
			this.eject = eject ? 1 : 0;
		}

		public FastPoint target;
		public int split;
		public int eject;

		public override string ToString()
		{
			return $"{target}{(split == 1 ? ", SPLIT" : "")}{(eject == 1 ? ", EJECT" : "")}";
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct List
		{
			public const int capacity = 4;
			public byte count;
			public fixed byte data[capacity * size];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(FastDirect item)
			{
				fixed (byte* d = data)
				{
					((FastDirect*)d)[count++] = item;
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
						result.Append(((FastDirect*)d)[i]);
					}
					return result.ToString();
				}
			}
		}
	}
}