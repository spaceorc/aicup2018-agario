namespace Game.Ai
{
	public class EvaluationArgs
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
}