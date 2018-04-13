using System;
using Game.Protocol;
using Newtonsoft.Json;

namespace Game.Sim.Fast
{
	public class FastEvaluationConstants
	{
		public double canEatMeRadiusFactor = 3;
		public double canSuperEatMeRadiusFactor = 6;
		public double eatableRadiusFactor = 3;
		public double scoreCoeff = 10000;
		public double nearestFoodCoeff = 100;
		public double checkpointsTakenCoeff = 1;
		public double eatableCoeff = 10000;
		public double lastEatableCoeff = 50000;
		public double canEatMeCoeff = 20000;
		public double lastCanEatMeCoeff = 100000;
		public double canSuperEatMeCoeff = 40000;
		public double lastCanSuperEatMeCoeff = 200000;
	}

	public unsafe class FastEvaluation : IFastEvaluation
	{
		private readonly Config config;
		private readonly FastEvaluationConstants evaluationConstants;
		private readonly double diameter;

		public FastEvaluation(Config config, FastEvaluationConstants evaluationConstants)
		{
			this.config = config;
			this.evaluationConstants = evaluationConstants;
			diameter = Math.Sqrt(config.GAME_HEIGHT * config.GAME_HEIGHT + config.GAME_WIDTH * config.GAME_WIDTH);
		}

		public double Evaluate(FastGlobalState* global, FastState* state, int player)
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
			var eatingAllyRadius = double.PositiveInfinity;
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
							if (qdist < (evaluationConstants.eatableRadiusFactor * frag->radius) * (evaluationConstants.eatableRadiusFactor * frag->radius) && qdist < eatableQDist)
							{
								eatableQDist = qdist;
								eatableIsLast = efragments->count == 1;
								eatingAllyRadius = frag->radius;
							}
						}
						else if (efrag->EatableBySplit(frag))
						{
							var qdist = frag->QDistance(efrag);
							if (qdist < (evaluationConstants.canSuperEatMeRadiusFactor * efrag->radius) * (evaluationConstants.canSuperEatMeRadiusFactor * efrag->radius) && qdist < canEatMeQDist)
							{
								canEatMeQDist = qdist;
								canEatMeRadius = efrag->radius;
								isSuperEatMe = true;
							}
						}
						else if (efrag->Eatable(frag))
						{
							var qdist = frag->QDistance(efrag);
							if (qdist < (evaluationConstants.canEatMeRadiusFactor * efrag->radius) * (evaluationConstants.canEatMeRadiusFactor * efrag->radius) && qdist < canEatMeQDist)
							{
								canEatMeQDist = qdist;
								canEatMeRadius = efrag->radius;
								isSuperEatMe = false;
							}
						}
					}
				}
			}

			var scoreValue = state->scores[player] - maxEnemyScore;
			var nearestFoodValue = double.IsPositiveInfinity(foodQDist) ? 0 : (diameter - Math.Sqrt(foodQDist)) / diameter;
			var eatableValue = double.IsPositiveInfinity(eatableQDist) ? 0 : (eatingAllyRadius * evaluationConstants.eatableRadiusFactor - Math.Sqrt(eatableQDist)) / (eatingAllyRadius * evaluationConstants.eatableRadiusFactor);
			var canEatMeValue = isSuperEatMe || double.IsPositiveInfinity(canEatMeQDist) ? 0 : (canEatMeRadius * evaluationConstants.canEatMeRadiusFactor - Math.Sqrt(canEatMeQDist)) / (canEatMeRadius * evaluationConstants.canEatMeRadiusFactor);
			var canSuperEatMeValue = !isSuperEatMe || double.IsPositiveInfinity(canEatMeQDist) ? 0 : (canEatMeRadius * evaluationConstants.canSuperEatMeRadiusFactor - Math.Sqrt(canEatMeQDist)) / (canEatMeRadius * evaluationConstants.canSuperEatMeRadiusFactor);

			var checkpointsTakenValue = player != 0 ? 0 : state->checkpointsTaken + (diameter - Math.Sqrt(checkpointQDist)) / diameter;


			var result = scoreValue * evaluationConstants.scoreCoeff
			        + nearestFoodValue * evaluationConstants.nearestFoodCoeff
			        + checkpointsTakenValue * evaluationConstants.checkpointsTakenCoeff
			        + eatableValue * (eatableIsLast ? evaluationConstants.lastEatableCoeff : evaluationConstants.eatableCoeff)
			        - canEatMeValue * (fragments->count == 1 ? evaluationConstants.lastCanEatMeCoeff : evaluationConstants.canEatMeCoeff)
			        - canSuperEatMeValue * (fragments->count == 1 ? evaluationConstants.lastCanSuperEatMeCoeff : evaluationConstants.canSuperEatMeCoeff);
			Logger.Debug($"  {JsonConvert.SerializeObject(new {result, scoreValue, nearestFoodValue, checkpointsTakenValue, eatableValue, canEatMeValue, canSuperEatMeValue})}");
			return result;
		}
	}
}