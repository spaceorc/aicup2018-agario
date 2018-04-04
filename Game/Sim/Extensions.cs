using System;
using System.Collections.Generic;
using Game.Types;

namespace Game.Sim
{
	public static class Extensions
	{
		public static T GetOrAdd<TKey, T>(this Dictionary<TKey, T> dct, TKey key) where T : new()
		{
			return dct.GetOrAdd(key, _ => new T());
		}

		public static T GetOrAdd<TKey, T>(this Dictionary<TKey, T> dct, TKey key, Func<TKey, T> factory)
		{
			if (dct.TryGetValue(key, out var value))
				return value;
			dct.Add(key, value = factory(key));
			return value;
		}

		public static void Remove<TKey, T>(this Dictionary<TKey, T> dct, KeyValuePair<TKey, T> kvp)
		{
			((ICollection<KeyValuePair<TKey, T>>) dct).Remove(kvp);
		}

		public static T Clone<T>(this T circle) where T : Circle
		{
			return (T)circle.MemberwiseClone();
		}
	}
}