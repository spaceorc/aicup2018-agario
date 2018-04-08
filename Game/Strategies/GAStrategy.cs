using Game.Protocol;
using Game.Sim;
using Game.Types;

namespace Game.Strategies
{
	public class GAStrategy : SimulationStrategyBase
	{
		private readonly GA ga;

		public GAStrategy(Config config) : base(config)
		{
			ga = new GA(config, random);
		}


		protected override Direct GetDirect(Simulator sim)
		{
			var population = ga.Simulate(sim, 0);
			return population[0].desision;
		}
	}
}