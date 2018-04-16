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
	public unsafe struct FastDirect
	{
		public const int size = FastPoint.size + sizeof(double) + 2 * sizeof(int);

		static FastDirect()
		{
			if (sizeof(FastDirect) != size)
				throw new InvalidOperationException($"sizeof({nameof(FastDirect)})({sizeof(FastDirect)}) != {size}");
		}

		public FastDirect(double x, double y, bool split = false, bool eject = false, double estimation = 0)
		{
			target.x = x;
			target.y = y;
			this.split = split ? 1 : 0;
			this.eject = eject ? 1 : 0;
			this.estimation = estimation;
		}

		public FastPoint target;
		public double estimation;
		public int split;
		public int eject;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Limit(Config config)
		{
			if (target.x > config.GAME_WIDTH)
				target.x = config.GAME_WIDTH;
			else if (target.x < 0)
				target.x = 0;

			if (target.y > config.GAME_HEIGHT)
				target.y = config.GAME_HEIGHT;
			else if (target.y < 0)
				target.y = 0;
		}

		public override string ToString()
		{
			return $"{target}{(split == 1 ? ", SPLIT" : "")}{(eject == 1 ? ", EJECT" : "")} => {estimation}";
		}

		public Direct ToDirect(Config config)
		{
			return new Direct(target.x, target.y, config, split == 1, eject == 1);
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