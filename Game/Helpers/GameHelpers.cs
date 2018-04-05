using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Helpers
{
	public static class GameHelpers
	{
		public static void Shuffle<T>(this Random random, IList<T> arr, int count = -1, int iterations = -1)
		{
			if (count < 0 || count > arr.Count)
				count = arr.Count;
			if (iterations < 0 || iterations > count - 1)
				iterations = count - 1;
			for (var i = 0; i < iterations; i++)
			{
				var t = random.Next(i, count);
				var tmp = arr[i];
				arr[i] = arr[t];
				arr[t] = tmp;
			}
		}

		public static List<Type> GetImplementors<T>()
		{
			return typeof (T).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof (T).IsAssignableFrom(t)).ToList();
		}
	}
}