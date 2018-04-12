namespace Game.Sim.Fast
{
	public unsafe interface IFastAi
	{
		FastDirect GetDirect(FastGlobalState* global, FastState* state, int player);
	}
}