using System.Collections.Generic;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Types;
using Game.Strategies;

namespace Game.Ai
{
	public unsafe class SimulationAi : IAi
	{
		private readonly Config config;
		private readonly IEvaluation evaluation;
		private readonly int depth;
		private readonly bool useSplit;
		private readonly IAi simpleAi;

		public SimulationAi(Config config, IEvaluation evaluation, int depth, bool useSplit, IAi simpleAi)
		{
			this.config = config;
			this.evaluation = evaluation;
			this.depth = depth;
			this.useSplit = useSplit;
			this.simpleAi = simpleAi;
		}

		public static void Register()
		{
			Strategy.RegisterAi("sim_5_split", c => new SimulationAi(c, new Evaluation(c, EvaluationArgs.CreateDefault()), 5, true, new SimpleAi(c)));
			Strategy.RegisterAi("sim_7_split", c => new SimulationAi(c, new Evaluation(c, EvaluationArgs.CreateDefault()), 7, true, new SimpleAi(c)));
			Strategy.RegisterAi("sim_5_split_fixed", c => new SimulationAi(c, new FixedEvaluation(c, EvaluationArgs.CreateFixed()), 5, true, new SimpleAi(c)));
			Strategy.RegisterAi("sim_7_split_fixed", c => new SimulationAi(c, new FixedEvaluation(c, EvaluationArgs.CreateFixed()), 7, true, new SimpleAi(c)));
		}

		public FastDirect GetDirect(FastGlobal* global, Simulator* state, int player, TimeManager timeManager)
		{
			if (timeManager.BeStupid)
				return simpleAi.GetDirect(global, state, player, timeManager);
			var targets = GetPossibleTargets(global, state, player);
			var fragments = &state->fragments0 + player;
			var frag = (FastFragment*) fragments->data;
			var directs = new FastDirect.List();
			var bestScore = double.NegativeInfinity;
			var bestDirect = new FastDirect();
			for (var f = 0; f < fragments->count; f++, frag++)
			{
				foreach (var target in targets)
				{
					for(int split = 0; split <= (useSplit ? 1 : 0); ++split)
					{
						var clone = *state;
						var direct = new FastDirect();
						for (var i = 0; i < depth; i++)
						{
							directs.count = 0;
							for (var p = 0; p < 4; p++)
							{
								if (p == player)
								{
									var factor = config.INERTION_FACTOR / frag->mass - 1;
									var nx = target.x + frag->speed * frag->ndx * factor;
									var ny = target.y + frag->speed * frag->ndy * factor;
									var nextDirect = new FastDirect(nx, ny, split == 1);
									if (i == 0)
										direct = nextDirect;
									directs.Add(nextDirect);
								}
								else
								{
									directs.Add(simpleAi.GetDirect(global, &clone, p, timeManager));
								}
							}
							clone.Tick(global, &directs, config);
						}

						if (Logger.IsEnabled(Logger.Level.Debug))
							Logger.Debug($"{target.x};{target.y} split={split}");
						var score = evaluation.Evaluate(global, &clone, player);
						if (score > bestScore)
						{
							bestScore = score;
							bestDirect = direct;
						}
					}
				}
			}

			bestDirect.Limit(config);
			bestDirect.estimation = bestScore;
			Logger.Info($"Best score: {bestScore}");
			return bestDirect;
		}

		private static List<FastPoint> GetPossibleTargets(FastGlobal* global, Simulator* state, int player)
		{
			var result = new List<FastPoint>();

			if (player == 0)
			{
				var checkpoint = (FastPoint*)global->checkpoints.data;
				for (var f = 0; f < global->checkpoints.count; f++, checkpoint++)
					result.Add(*checkpoint);
			}

			var food = (FastPoint*) state->foods.data;
			for (var f = 0; f < state->foods.count; f++, food++)
				result.Add(*food);

			var virus = (FastVirus*) state->viruses.data;
			for (var f = 0; f < state->viruses.count; f++, virus++)
				result.Add(*(FastPoint*) &virus->point);

			var eject = (FastEjection*) state->ejections.data;
			for (var f = 0; f < state->ejections.count; f++, eject++)
				result.Add(*(FastPoint*) &eject->point);

			var fragments = &state->fragments0;
			for (var p = 0; p < 4; p++, fragments++)
			{
				if (p == player)
					continue;
				var frag = (FastFragment*)fragments->data;
				for (var f = 0; f < fragments->count; f++, frag++)
				{
					var point = *(FastPoint*) frag;
					result.Add(point);
					point.x += frag->ndx;
					point.y += frag->ndy;
					result.Add(point);
				}
			}

			return result;
		}
	}
}