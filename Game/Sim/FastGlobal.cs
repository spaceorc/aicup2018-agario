using System.Runtime.InteropServices;
using Game.Sim.Types;

namespace Game.Sim
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastGlobal
	{
		public FastPoint.List checkpoints;
	}
}