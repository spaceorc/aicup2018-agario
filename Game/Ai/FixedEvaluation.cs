using System;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Types;
using Game.Types;
using Newtonsoft.Json;

namespace Game.Ai
{
	public unsafe class FixedEvaluation : IEvaluation
	{
		private readonly Config config;
		private readonly EvaluationArgs evaluationArgs;
		private readonly double diameter;

		public FixedEvaluation(Config config, EvaluationArgs evaluationArgs)
		{
			this.config = config;
			this.evaluationArgs = evaluationArgs;
			diameter = Math.Sqrt(config.GAME_HEIGHT * config.GAME_HEIGHT + config.GAME_WIDTH * config.GAME_WIDTH);
		}

		public double Evaluate(FastGlobal* global, Simulator* state, int player)
		{
			var fragments = &state->fragments0 + player;
			if (fragments->count == 0)
				return double.MinValue;

			var maxEnemyScore = double.NegativeInfinity;
			for (int p = 0; p < 4; p++)
			{
				if (p != player && state->scores[p] > maxEnemyScore)
					maxEnemyScore = state->scores[p];
			}

			var checkpointQDist = double.PositiveInfinity;
			var foodQDist = double.PositiveInfinity;
			var canEatMeQDist = double.PositiveInfinity;
			var canEatMeRadius = double.PositiveInfinity;
			var isSuperEatMe = false;
			var eatableQDist = double.PositiveInfinity;
			var allyRadius = double.PositiveInfinity;
			var eatableIsLast = false;
			var checkpoints = (FastPoint*)global->checkpoints.data;
			var nextCheckpoint = checkpoints + state->nextCheckpoint;
			var frag = (FastFragment*)fragments->data;
			for (var i = 0; i < fragments->count; i++, frag++)
			{
				if (player == 0)
				{
					var qdist = frag->QDistance(nextCheckpoint);
					if (qdist < checkpointQDist)
						checkpointQDist = qdist;
				}
				var food = (FastPoint*)state->foods.data;
				for (var f = 0; f < state->foods.count; f++, food++)
				{
					if (food->x < frag->radius && food->y < frag->radius)
					{
						if (food->QDistance(new FastPoint(frag->radius, frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					if (food->x < frag->radius && food->y > config.GAME_HEIGHT - frag->radius)
					{
						if (food->QDistance(new FastPoint(frag->radius, config.GAME_HEIGHT - frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					if (food->x > config.GAME_WIDTH - frag->radius && food->y < frag->radius)
					{
						if (food->QDistance(new FastPoint(config.GAME_WIDTH - frag->radius, frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					if (food->x > config.GAME_WIDTH - frag->radius && food->y > config.GAME_HEIGHT - frag->radius)
					{
						if (food->QDistance(new FastPoint(config.GAME_WIDTH - frag->radius, config.GAME_HEIGHT - frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					var qdist = frag->QDistance(food);
					if (qdist < foodQDist)
						foodQDist = qdist;
				}

				var efragments = &state->fragments0;
				for (var e = 0; e < 4; e++, efragments++)
				{
					if (e == player)
						continue;
					var efrag = (FastFragment*)efragments->data;
					for (var ef = 0; ef < efragments->count; ef++, efrag++)
					{
						if (frag->Eatable(efrag))
						{
							var qdist = frag->QDistance(efrag);
							var maxDist = evaluationArgs.eatableRadiusFactor * frag->radius;
							if (qdist < maxDist * maxDist && qdist < eatableQDist)
							{
								eatableQDist = qdist;
								eatableIsLast = efragments->count == 1;
								allyRadius = frag->radius;
							}
						}
						else if (efrag->EatableBySplit(frag))
						{
							var qdist = frag->QDistance(efrag);
							var maxDist = evaluationArgs.canSuperEatMeRadiusFactor * efrag->radius;
							if (qdist < maxDist * maxDist && qdist < canEatMeQDist)
							{
								canEatMeQDist = qdist;
								canEatMeRadius = efrag->radius;
								allyRadius = frag->radius;
								isSuperEatMe = true;
							}
						}
						else if (efrag->Eatable(frag))
						{
							var qdist = frag->QDistance(efrag);
							var maxDist = evaluationArgs.canEatMeRadiusFactor * efrag->radius;
							if (qdist < maxDist * maxDist && qdist < canEatMeQDist)
							{
								canEatMeQDist = qdist;
								canEatMeRadius = efrag->radius;
								allyRadius = frag->radius;
								isSuperEatMe = false;
							}
						}
					}
				}
			}

			var scoreValue = state->scores[player] - maxEnemyScore;
			var nearestFoodValue = double.IsPositiveInfinity(foodQDist) ? 0 : (diameter - Math.Sqrt(foodQDist)) / diameter;
			var eatableValue = double.IsPositiveInfinity(eatableQDist) ? 0 : (allyRadius * evaluationArgs.eatableRadiusFactor - Math.Sqrt(eatableQDist)) / (allyRadius * evaluationArgs.eatableRadiusFactor);
			double canEatMeValue = 0;
			double canSuperEatMeValue = 0;
			if (!double.IsPositiveInfinity(canEatMeQDist))
			{
				var dist = Math.Sqrt(canEatMeQDist);
				var minDist = canEatMeRadius + allyRadius - 2 * allyRadius * Constants.DIAM_EAT_FACTOR;
				var maxDist = isSuperEatMe
					? canEatMeRadius * evaluationArgs.canSuperEatMeRadiusFactor
					: canEatMeRadius * evaluationArgs.canEatMeRadiusFactor;
				if (isSuperEatMe)
					canSuperEatMeValue = (maxDist - dist) / (maxDist - minDist);
				else
					canEatMeValue = (maxDist - dist) / (maxDist - minDist);
			}

			var checkpointsTakenValue = player != 0 ? 0 : state->checkpointsTaken + (diameter - Math.Sqrt(checkpointQDist)) / diameter;
			var result = scoreValue * evaluationArgs.scoreCoeff
			             + nearestFoodValue * evaluationArgs.nearestFoodCoeff
			             + checkpointsTakenValue * evaluationArgs.checkpointsTakenCoeff
			             + eatableValue * (eatableIsLast ? evaluationArgs.lastEatableCoeff : evaluationArgs.eatableCoeff)
			             - canEatMeValue * (fragments->count == 1 ? evaluationArgs.lastCanEatMeCoeff : evaluationArgs.canEatMeCoeff)
			             - canSuperEatMeValue * (fragments->count == 1 ? evaluationArgs.lastCanSuperEatMeCoeff : evaluationArgs.canSuperEatMeCoeff);
			if (Logger.IsEnabled(Logger.Level.Debug))
				Logger.Debug($"  {JsonConvert.SerializeObject(new { result, scoreValue, nearestFoodValue, checkpointsTakenValue, eatableValue, canEatMeValue, canSuperEatMeValue })}");
			return result;
		}
	}
}