namespace Game.Sim.Fast
{
	public unsafe interface IFastEvaluation
	{
		double Evaluate(FastGlobalState* global, FastState* state, int player);
	}
}