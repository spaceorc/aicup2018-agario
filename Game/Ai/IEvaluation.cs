using Game.Sim;

namespace Game.Ai
{
	public unsafe interface IEvaluation
	{
		double Evaluate(FastGlobal* global, Simulator* state, int player);
	}
}