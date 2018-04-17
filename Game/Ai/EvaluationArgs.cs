namespace Game.Ai
{
	public class EvaluationArgs
	{
		public double canEatMeRadiusFactor;
		public double canSuperEatMeRadiusFactor;
		public double eatableRadiusFactor;
		public double scoreCoeff;
		public double nearestFoodCoeff;
		public double checkpointsTakenCoeff;
		public double eatableCoeff;
		public double lastEatableCoeff;
		public double canEatMeCoeff;
		public double lastCanEatMeCoeff;
		public double canSuperEatMeCoeff;
		public double lastCanSuperEatMeCoeff;

		private EvaluationArgs()
		{
		}

		public static EvaluationArgs CreateFixed()
		{
			var result = new EvaluationArgs
			{
				lastCanSuperEatMeCoeff = 500000,
				canSuperEatMeCoeff = 80000,
				lastCanEatMeCoeff = 250000,
				canEatMeCoeff = 50000,
				lastEatableCoeff = 50000,
				eatableCoeff = 10000,
				checkpointsTakenCoeff = 1,
				nearestFoodCoeff = 100,
				scoreCoeff = 10000,
				eatableRadiusFactor = 4,
				canSuperEatMeRadiusFactor = 6,
				canEatMeRadiusFactor = 4
			};
			return result;
		}

		public static EvaluationArgs CreateDefault()
		{
			var result = new EvaluationArgs
			{
				canEatMeRadiusFactor = 3,
				canSuperEatMeRadiusFactor = 6,
				eatableRadiusFactor = 3,
				scoreCoeff = 10000,
				nearestFoodCoeff = 100,
				checkpointsTakenCoeff = 1,
				eatableCoeff = 10000,
				lastEatableCoeff = 50000,
				canEatMeCoeff = 20000,
				lastCanEatMeCoeff = 100000,
				canSuperEatMeCoeff = 40000,
				lastCanSuperEatMeCoeff = 200000
			};
			return result;
		}
	}
}