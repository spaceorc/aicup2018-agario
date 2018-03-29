using Newtonsoft.Json.Linq;

namespace Game
{
	public class Strategy
	{
		public Strategy(JObject config)
		{
		}

		public JObject OnTick(JObject parsed)
		{
			var mine = (JArray)parsed.GetValue("Mine");
			var result = new JObject();
			if (mine.Count > 0)
			{
				var objects = (JArray)parsed.GetValue("Objects");
				var food = FindFood(objects);
				if (food != null)
				{
					result["X"] = food.GetValue("X");
					result["Y"] = food.GetValue("Y");
				}
				else
				{
					result["X"] = 0;
					result["Y"] = 0;
					result["Debug"] = "No food";
				}
			}
			else
			{
				result["X"] = 0;
				result["Y"] = 0;
				result["Debug"] = "Died";
			}
			return result;
		}

		private static JObject FindFood(JArray objects)
		{
			foreach (JObject obj in objects)
			{
				var type = (string)obj.GetValue("T");
				if (type.Equals("F"))
				{
					return obj;
				}
			}
			return null;
		}
	}
}