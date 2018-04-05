using Game.Protocol;

namespace Game.Strategies
{
	public class SimpleStrategy : IStrategy
	{
		public SimpleStrategy(Config config)
		{
		}

		public TurnOutput OnTick(TurnInput turnInput)
		{
			var mine = turnInput.Mine;
			var result = new TurnOutput();
			if (mine.Length > 0)
			{
				var objects = turnInput.Objects;
				var food = FindFood(objects);
				if (food != null)
				{
					result.X = food.X;
					result.Y = food.Y;
				}
				else
				{
					result.X = 0;
					result.Y = 0;
					result.Debug = "No food";
				}
			}
			else
			{
				result.X = 0;
				result.Y = 0;
				result.Debug = "Died";
			}
			return result;
		}

		private static TurnInput.ObjectData FindFood(TurnInput.ObjectData[] objects)
		{
			foreach (var obj in objects)
			{
				var type = obj.T;
				if (type.Equals("F"))
					return obj;
			}
			return null;
		}
	}
}