using Game.Protocol;
using Game.Sim;
using Game.Sim.Types;
using Game.Strategies;

namespace Game.Ai
{
	public unsafe class FixedSimulationAi : IAi
	{
		private readonly Config config;
		private readonly int depth;
		private readonly IEvaluation evaluation;
		private readonly IAi simpleAi;
		private readonly bool useSplit;

		public FixedSimulationAi(Config config, IEvaluation evaluation, int depth, bool useSplit, IAi simpleAi)
		{
			this.config = config;
			this.evaluation = evaluation;
			this.depth = depth;
			this.useSplit = useSplit;
			this.simpleAi = simpleAi;
		}

		public FastDirect GetDirect(FastGlobal* global, Simulator* state, int player, TimeManager timeManager)
		{
			if (timeManager.BeStupid)
				return simpleAi.GetDirect(global, state, player, timeManager);
			var bestScore = double.NegativeInfinity;
			var bestDirect = new FastDirect();

			var targets = stackalloc FastPoint[16 * 10];
			var targetsCount = 0;
			GetStaticTargetsForAll(global, player, targets, ref targetsCount);
			for (var t = 0; t < targetsCount; ++t)
			{
				var target = targets[t];
				for (var split = 0; split <= (useSplit ? 1 : 0); ++split)
				{
					var clone = *state;
					var direct = SimAllToStaticTarget(global, &clone, player, timeManager, target, split);
					var score = evaluation.Evaluate(global, &clone, player);
					if (score > bestScore)
					{
						bestScore = score;
						bestDirect = direct;
					}
				}
			}

			var virus = (FastVirus*) state->viruses.data;
			for (var f = 0; f < state->viruses.count; f++, virus++)
			{
				var clone = *state;
				var direct = SimAllToStaticTarget(global, &clone, player, timeManager, *(FastPoint*) &virus->point, 0);
				var score = evaluation.Evaluate(global, &clone, player);
				if (score > bestScore)
				{
					bestScore = score;
					bestDirect = direct;
				}
			}

			targetsCount = 0;
			GetStaticTargetsForFrags(state, targets, ref targetsCount);
			var fragments = &state->fragments0 + player;
			var frag = (FastFragment*) fragments->data;
			for (var f = 0; f < fragments->count; f++, frag++)
			{
				for (var t = 0; t < targetsCount; ++t)
				{
					var target = targets[t];
					for (var split = 0; split <= (useSplit ? 1 : 0); ++split)
					{
						var clone = *state;
						var direct = SimFragToStaticTarget(global, &clone, player, timeManager, frag, target, split);
						var score = evaluation.Evaluate(global, &clone, player);
						if (score > bestScore)
						{
							bestScore = score;
							bestDirect = direct;
						}
					}
				}
				var efragments = &state->fragments0;
				for (var p = 0; p < 4; p++, efragments++)
				{
					if (p == player)
						continue;
					var efrag = (FastFragment*) efragments->data;
					for (var ef = 0; ef < efragments->count; ef++, efrag++)
					{
						for (var split = 0; split <= (useSplit ? 1 : 0); ++split)
						{
							var clone = *state;
							var direct = SimFragToEnemy(global, &clone, player, p, timeManager, frag, efrag, split);
							var score = evaluation.Evaluate(global, &clone, player);
							if (score > bestScore)
							{
								bestScore = score;
								bestDirect = direct;
							}
						}	
					}
				}
			}
			
			
			bestDirect.Limit(config);
			bestDirect.estimation = bestScore;
			Logger.Info($"Best score: {bestScore}");
			return bestDirect;
		}

		public static void Register()
		{
			//Strategy.RegisterAi("fsim_5_split",c => new FixedSimulationAi(c, new Evaluation(c, EvaluationArgs.CreateDefault()), 5, true, new SimpleAi(c)));
			//Strategy.RegisterAi("fsim_7_split",c => new FixedSimulationAi(c, new Evaluation(c, EvaluationArgs.CreateDefault()), 7, true, new SimpleAi(c)));
			Strategy.RegisterAi("fsim_5_split_fixed",c => new FixedSimulationAi(c, new FixedEvaluation(c, EvaluationArgs.CreateFixed()), 5, true, new SimpleAi(c)));
			Strategy.RegisterAi("fsim_7_split_fixed",c => new FixedSimulationAi(c, new FixedEvaluation(c, EvaluationArgs.CreateFixed()), 7, true, new SimpleAi(c)));
		}

		private FastDirect SimFragToStaticTarget(FastGlobal* global, Simulator* sim, int player, TimeManager timeManager,
			FastFragment* frag, FastPoint target, int split)
		{
			var direct = new FastDirect();
			for (var i = 0; i < depth; i++)
			{
				FastDirect.List directs;
				directs.count = 0;
				for (var p = 0; p < 4; p++)
					if (p == player)
					{
						var factor = config.INERTION_FACTOR / frag->mass - 1;
						var nx = target.x + frag->speed * frag->ndx * factor;
						var ny = target.y + frag->speed * frag->ndy * factor;
						var nextDirect = new FastDirect(nx, ny, i == 0 && split == 1);
						if (i == 0)
							direct = nextDirect;
						directs.Add(nextDirect);
					}
					else
					{
						directs.Add(simpleAi.GetDirect(global, sim, p, timeManager));
					}

				sim->Tick(global, &directs, config);
			}

			return direct;
		}

		private FastDirect SimFragToEnemy(FastGlobal* global, Simulator* sim, int player, int targetPlayer, TimeManager timeManager,
			FastFragment* frag, FastFragment* target, int split)
		{
			var direct = new FastDirect();
			var targetFragmentId = target->fragmentId;
			var targets = &sim->fragments0 + targetPlayer;
			var targetsCount = targets->count;
			for (var i = 0; i < depth; i++)
			{
				FastDirect.List directs;
				directs.count = 0;
				for (var p = 0; p < 4; p++)
					if (p == player)
					{
						if (targets->count != targetsCount || target->fragmentId != targetFragmentId)
						{
							target = null;
							var efrag = (FastFragment*)targets->data;
							for (var ef = 0; ef < targets->count; ef++, efrag++)
							{
								if (efrag->fragmentId == targetFragmentId)
								{
									target = efrag;
									break;
								}
							}

							if (target == null)
								return new FastDirect();
						}

						var factor = config.INERTION_FACTOR / frag->mass - 1;
						var nx = target->x + target->ndx * target->speed + frag->speed * frag->ndx * factor;
						var ny = target->y + target->ndy * target->speed + frag->speed * frag->ndy * factor;
						var nextDirect = new FastDirect(nx, ny, i == 0 && split == 1);
						if (i == 0)
							direct = nextDirect;
						directs.Add(nextDirect);
					}
					else
					{
						directs.Add(simpleAi.GetDirect(global, sim, p, timeManager));
					}

				sim->Tick(global, &directs, config);
			}
			return direct;
		}

		private FastDirect SimAllToStaticTarget(FastGlobal* global, Simulator* sim, int player, TimeManager timeManager,
			FastPoint target, int split)
		{
			var direct = new FastDirect();
			for (var i = 0; i < depth; i++)
			{
				FastDirect.List directs;
				directs.count = 0;
				for (var p = 0; p < 4; p++)
					if (p == player)
					{
						var nextDirect = new FastDirect(target.x, target.y, i == 0 && split == 1);
						if (i == 0)
							direct = nextDirect;
						directs.Add(nextDirect);
					}
					else
					{
						directs.Add(simpleAi.GetDirect(global, sim, p, timeManager));
					}

				sim->Tick(global, &directs, config);
			}

			return direct;
		}

		private void GetStaticTargetsForAll(FastGlobal* global, int player, FastPoint* targets,
			ref int targetsCount)
		{
			if (player == 0)
			{
				var checkpoint = (FastPoint*) global->checkpoints.data;
				for (var f = 0; f < global->checkpoints.count; f++, checkpoint++)
				{
					*targets = *checkpoint;
					targets++;
					targetsCount++;
				}
			}

			*targets = new FastPoint(0, 0);
			targets++;
			targetsCount++;

			*targets = new FastPoint(config.GAME_WIDTH, 0);
			targets++;
			targetsCount++;

			*targets = new FastPoint(config.GAME_WIDTH, config.GAME_HEIGHT);
			targets++;
			targetsCount++;

			*targets = new FastPoint(0, config.GAME_HEIGHT);
			targets++;
			targetsCount++;

			*targets = new FastPoint(config.GAME_WIDTH / 2.0, 0);
			targets++;
			targetsCount++;

			*targets = new FastPoint(config.GAME_WIDTH, config.GAME_HEIGHT / 2.0);
			targets++;
			targetsCount++;

			*targets = new FastPoint(config.GAME_WIDTH / 2.0, config.GAME_HEIGHT);
			targets++;
			targetsCount++;

			*targets = new FastPoint(0, config.GAME_HEIGHT / 2.0);
			targets++;
			targetsCount++;
		}

		private static void GetStaticTargetsForFrags(Simulator* state, FastPoint* targets, ref int targetsCount)
		{
			var food = (FastPoint*) state->foods.data;
			for (var f = 0; f < state->foods.count; f++, food++)
			{
				*targets = *food;
				targets++;
				targetsCount++;
			}

			var eject = (FastEjection*) state->ejections.data;
			for (var f = 0; f < state->ejections.count; f++, eject++)
			{
				*targets = *(FastPoint*) &eject->point;
				targets++;
				targetsCount++;
			}
		}
	}
}