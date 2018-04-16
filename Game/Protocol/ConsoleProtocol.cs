using System;
using Newtonsoft.Json;

namespace Game.Protocol
{
	public static class ConsoleProtocol
	{
		public static Config ReadConfig()
		{
			var line = FixedConsole.ReadLine();
			return JsonConvert.DeserializeObject<Config>(line);
		}

		public static TurnInput ReadTurnInput()
		{
			var line = FixedConsole.ReadLine();
			return JsonConvert.DeserializeObject<TurnInput>(line);
		}

		public static void WriteTurnInput(TurnOutput output)
		{
			if (output.Debug != null && output.Debug.Length > 1000)
				output.Debug = output.Debug.Substring(0, 1000);
			Console.WriteLine(JsonConvert.SerializeObject(output, new JsonSerializerSettings{Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore}));
		}
	}
}