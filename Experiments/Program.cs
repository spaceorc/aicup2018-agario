using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.Helpers;
using Game.Mech;
using Game.Protocol;
using Game.Strategies;
using Newtonsoft.Json;

namespace Experiments
{
	internal class Program
	{
		public static void Main()
		{
			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":1.1699651787931546,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":18.311331992550009,""MAX_FRAGS_CNT"":4,""SPEED_FACTOR"":61.964198190568723,""TICKS_TIL_FUSION"":428,""VIRUS_RADIUS"":27.671011120486231,""VIRUS_SPLIT_MASS"":82.838148942917172,""VISCOSITY"":0.1961202661671938}");
			var mechanic = new Mechanic(config, new List<PlayerStrategy>
			{
				new PlayerStrategy(config, c => new SimpleStrategy(c)),
				new PlayerStrategy(config, c => new GAStrategy(c)),
				new PlayerStrategy(config, c => new NearestFoodStrategy(c, true)),
				new PlayerStrategy(config, c => new MonteCarloStrategy(config)),
			});
			mechanic.Play();
			foreach (var kvp in mechanic.playerScores)
			{
				Console.Out.WriteLine($"{mechanic.strategies[kvp.Key]}: {kvp.Value}");
			}
		}

		public static void Main2()
		{
			const int parallelizm = 4;
			var strategies = StrategiesRegistry.Names;
			var nameMaxLength = strategies.Max(s => s.Length);
			int exit = 0;
			Console.CancelKeyPress += (_, args) =>
			{
				exit = 1;
				args.Cancel = true;
			};

			var wins = new int[strategies.Length];
			var gameScores = new StatValue[strategies.Length];
			for (int i = 0; i < gameScores.Length; i++)
				gameScores[i] = new StatValue();

			var total = new int[strategies.Length];
			var tasks = new Task[parallelizm];
			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = Task.Run(
					() =>
					{
						try
						{
							var random = new Random(Guid.NewGuid().GetHashCode());
							while (Volatile.Read(ref exit) != 1)
							{
								var players = strategies.Select((args, player) => new { args, player }).ToArray();
								random.Shuffle(players);
								players = players.Take(4).ToArray();
								var scores = Play(players.Select(p => p.args).ToArray());
								var winners = scores.Select((score, pi) => new { score, players[pi].player }).OrderByDescending(x => x.score).ToArray();
								for (var k = 0; k < winners.Length; k++)
								{
									lock (gameScores[winners[k].player])
										gameScores[winners[k].player].Add(winners.Length - k);
								}
								var winner = winners.First().player;
								Interlocked.Increment(ref wins[winner]);
								foreach (var player in players)
									Interlocked.Increment(ref total[player.player]);
							}
						}
						catch (Exception exception)
						{
							Console.WriteLine(exception);
							Environment.Exit(1);
						}
					});
			}

			var sort = -1;
			while (Volatile.Read(ref exit) != 1)
			{
				Thread.Sleep(1000);
				var stats = strategies
					.Select((bot, i) =>
					{
						var w = wins[i];
						var t = total[i];
						var gs = gameScores[i].Clone();
						var percent = t == 0 ? 0 : w * 100 / t;
						var ps = new { percent, wins = w, total = t, score = gs };

						return new { bot, ps, score = ps.score.Mean };
					})
					.OrderByDescending(s => s.ps.percent)
					.ToArray();
				Console.SetCursorPosition(0, 0);
				var statSep = $"+{Pad("-", nameMaxLength, '-')}+{Pad("-", 6, '-')}+{Pad("-", 6, '-')}+{Pad("-", 6, '-')}+{Pad("-", 10, '-')}+";
				WriteLine(statSep);
				WriteLine($"|{Pad("Bot", nameMaxLength)}|!{Pad("%wins", 6)}!|{Pad("wins", 6)}|{Pad("total", 6)}|{Pad("score", 10)}|");
				WriteLine(statSep);
				foreach (var stat in stats.Take(30))
					WriteLine($"|{Pad(stat.bot, nameMaxLength)}|{(stat.ps.percent == stats.Max(s => s.ps.percent) ? "!" : "")}{Pad(stat.ps.percent.ToString(), 6)}{(stat.ps.percent == stats.Max(s => s.ps.percent) ? "!" : "")}|{Pad(stat.ps.wins.ToString(), 6)}|{Pad(stat.ps.total.ToString(), 6)}|{Pad($"{stat.ps.score.Mean:F1} +-{stat.ps.score.Dispersion:F1}", 10)}|");
				WriteLine(statSep);
			}
			Task.WaitAll(tasks);
		}

		private static void WriteLine(string s)
		{
			var strings = s.Split('!');
			var colored = false;
			foreach (var s1 in strings)
			{
				if (colored)
					Console.ForegroundColor = ConsoleColor.Green;
				else
					Console.ResetColor();
				Console.Write(s1);
				colored = !colored;
			}
			Console.ResetColor();
			Console.WriteLine();
		}

		private static string Pad(string s, int l, char c = ' ')
		{
			return s.PadRight(l, c).Substring(0, l);
		}

		private static int[] Play(params string[] strategies)
		{
			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":1.1699651787931546,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":18.311331992550009,""MAX_FRAGS_CNT"":4,""SPEED_FACTOR"":61.964198190568723,""TICKS_TIL_FUSION"":428,""VIRUS_RADIUS"":27.671011120486231,""VIRUS_SPLIT_MASS"":82.838148942917172,""VISCOSITY"":0.1961202661671938}");
			var mechanic = new Mechanic(config, strategies.Select(s => new PlayerStrategy(config, c => StrategiesRegistry.Create(s, c))).ToList());
			mechanic.Play();
			return mechanic.playerScores.Select(x => x.Value).ToArray();
		}
	}
}