using Game.Protocol;

namespace Game.Types
{
	public class Food : Circle
	{
		public Food(int id, double x, double y, Config config) : base(id, x, y, Constants.FOOD_RADIUS, config.FOOD_MASS, config)
		{
		}

		public TurnInput.ObjectData ToObjectData()
		{
			return new TurnInput.ObjectData
			{
				X = x,
				Y = y,
				T = "F"
			};
		}
	}
}