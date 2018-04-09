using System.Runtime.InteropServices;

namespace Game.Sim.Fast
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct FastGlobalState
	{
		public FastPoint.List checkpoints;
	}
}