using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Helpers;
using Game.Protocol;

namespace Game.Strategies
{
	public static class StrategiesRegistry
	{
		private static readonly Dictionary<string, Func<Config, IStrategy>> strategies = new Dictionary<string, Func<Config, IStrategy>>(StringComparer.OrdinalIgnoreCase);

		public static void Register(string name, Func<Config, IStrategy> factory)
		{
			strategies.Add(name, factory);
		}

		public static string[] Names => strategies.Keys.ToArray();

		public static IStrategy Create(string name, Config data)
		{
			if (!strategies.TryGetValue(name, out var factory))
				throw new InvalidOperationException($"Couldn't find strategy: {name}");
			return factory(data);
		}

		static StrategiesRegistry()
		{
			foreach (var strategyType in GameHelpers.GetImplementors<IStrategy>())
			{
				var registerMethod = strategyType.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
				if (registerMethod == null)
					Register(strategyType.Name, data => (IStrategy)Activator.CreateInstance(strategyType, data));
				else
					registerMethod.Invoke(null, new object[0]);
			}
		}
	}
}