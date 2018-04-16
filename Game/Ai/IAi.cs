using Game.Sim.Types;

namespace Game.Sim
{
	public unsafe interface IAi
	{
		FastDirect GetDirect(FastGlobal* global, Simulator* state, int player);
	}
}