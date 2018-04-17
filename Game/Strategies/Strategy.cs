using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Types;
using Game.Types;

namespace Game.Strategies
{
	public unsafe class Strategy : IStrategy
	{
		private readonly Config config;
		private readonly IAi ai;
		private const int CHECKPOINTS_COUNT = FastPoint.List.capacity;
		private const int CHECKPOINTS_LAST_AWAY_COUNT = 2;
		private FastGlobal global;
		private int nextCheckpoint;
		private TimeManager timeManager;
		private readonly State state;
		private readonly Random random;
		private readonly string checkpointsDebugInfo;

		public Strategy(Config config, IAi ai)
		{
			this.config = config;
			state = new State(config);
			random = new Random();
			this.ai = ai;
			for (var i = 0; i < CHECKPOINTS_COUNT; i++)
				global.checkpoints.Add(GenerateCheckPoint(i));
			checkpointsDebugInfo = GetCheckpointsDebugInfo();
		}

		public TurnOutput OnTick(TurnInput turnInput, TimeManager timeManager)
		{
			this.timeManager = timeManager;
			state.Apply(turnInput);
			var direct = GetDirect() ?? new Direct(0, 0, config) { debug = "Accept defeat" };
			return direct.ToOutput();
		}

		public static void RegisterAi(string name, Func<Config, IAi> createAi)
		{
			StrategiesRegistry.Register(name, c => new Strategy(c, createAi(c)));
		}

		public static void Register()
		{
			foreach (var aiType in GameHelpers.GetImplementors<IAi>())
			{
				var registerMethod = aiType.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
				if (registerMethod == null)
					RegisterAi(aiType.Name, c => (IAi)Activator.CreateInstance(aiType, c));
				else
					registerMethod.Invoke(null, new object[0]);
			}
		}

		private FastPoint GenerateCheckPoint(int index)
		{
			fixed (FastGlobal* g = &global)
			{
				var minDiffDist = Math.Sqrt(config.GAME_WIDTH * config.GAME_WIDTH + config.GAME_HEIGHT * config.GAME_HEIGHT) * 0.25;
				var awayPoints = new List<Point>();
				for (var i = index + CHECKPOINTS_COUNT - CHECKPOINTS_LAST_AWAY_COUNT; i <= index + CHECKPOINTS_COUNT + CHECKPOINTS_LAST_AWAY_COUNT; i++)
				{
					var ai = i % CHECKPOINTS_COUNT;
					if (ai == index || ai >= g->checkpoints.count)
						continue;
					var fastPoint = (FastPoint*)g->checkpoints.data + ai;
					awayPoints.Add(new Point(fastPoint->x, fastPoint->y));
				}
				while (true)
				{
					var nextGlobalTarget = new Point(
						config.GAME_WIDTH / 20 + random.NextDouble() * config.GAME_WIDTH * 18 / 20,
						config.GAME_HEIGHT / 20 + random.NextDouble() * config.GAME_HEIGHT * 18 / 20);
					if (awayPoints.All(p => p.Distance(nextGlobalTarget) > minDiffDist))
						return new FastPoint(nextGlobalTarget.x, nextGlobalTarget.y);
				}
			}
		}

		private Direct GetDirect()
		{
			var sim = new Simulator(state, nextCheckpoint);
			PrepareGlobalTarget(&sim);
			fixed (FastGlobal* g = &global)
			{
				var fastDirect = ai.GetDirect(g, &sim, 0, timeManager);
				var direct = fastDirect.ToDirect(config);
				direct.debug = $"{(timeManager.BeStupid ? "STUPID! " : "")}{(timeManager.BeSmart ? "SMART! " : "")}{timeManager.Elapsed}/{timeManager.millisPerTick} ms; " +
				               $"est: {fastDirect.estimation}; next: {nextCheckpoint}; cp: {checkpointsDebugInfo}";
				return direct;
			}
		}

		private string GetCheckpointsDebugInfo()
		{
			fixed (FastGlobal* g = &global)
			{
				var result = new StringBuilder();
				var checkpoint = (FastPoint*)g->checkpoints.data;
				for (var i = 0; i < g->checkpoints.count; i++, checkpoint++)
				{
					if (i != 0)
						result.Append(';');
					result.Append(new Point((int)checkpoint->x, (int)checkpoint->y));
				}
				return result.ToString();
			}
		}

		private void PrepareGlobalTarget(Simulator* sim)
		{
			fixed (FastGlobal* g = &global)
			{
				sim->nextCheckpoint = nextCheckpoint;
				if (sim->UpdateNextCheckpoint(g))
					nextCheckpoint = sim->nextCheckpoint;
			}
		}
	}
}