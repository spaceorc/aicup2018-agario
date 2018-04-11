using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Game;
using Game.Helpers;
using Game.Mech;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Fast;
using Game.Strategies;
using Game.Types;
using Newtonsoft.Json;
using Point = Game.Types.Point;

namespace Experiments
{
	internal class Program
	{
		public static void Main212()
		{
			Logger.enableFile = false;

			var config = JsonConvert.DeserializeObject<Config>(
				@"{""GAME_HEIGHT"":660,""GAME_WIDTH"":660,""GAME_TICKS"":75000,""FOOD_MASS"":1.0,""MAX_FRAGS_CNT"":10,""TICKS_TIL_FUSION"":250,""VIRUS_RADIUS"":22.0,""VIRUS_SPLIT_MASS"":80.0,""VISCOSITY"":0.25,""INERTION_FACTOR"":10.0,""SPEED_FACTOR"":25.0}");

			var turnInput = JsonConvert.DeserializeObject<TurnInput>(
				@"{""Mine"":[{""Id"":""1"",""X"":282.764659119549,""Y"":255.00047968359704,""SX"":2.3029305366512363,""SY"":1.1739094325097006,""R"":12.649110640673518,""M"":40.0,""TTF"":3014700}],""Objects"":[{""Id"":null,""pId"":null,""T"":""F"",""X"":299.0,""Y"":225.0,""R"":0.0,""M"":0.0}]}");

			var log = new DirectoryInfo(FileHelper.PatchDirectoryName("logs")).GetFiles("*.txt").OrderBy(x => x.Name).Select(x => x.FullName).Last();

			var simState = new SimState(config);
			simState.Apply(turnInput);

			var lines = File.ReadAllLines(log);
			var globalTargets = new List<Point>();
			foreach (var gline in lines.Where(ll => ll.IndexOf("GLOBAL: ") >= 0))
			{
				var gs = gline.Substring(gline.IndexOf("GLOBAL: ") + "GLOBAL: ".Length).Split(' ');
				globalTargets.Add(new Point(double.Parse(gs[0]), double.Parse(gs[1])));
			}

			var simulator = new Simulator(simState, globalTargets.ToArray(), new[] {0, 0, 0, 0});
			var genome = @"0,723181351890406 0,744578209586711 0,772885398367832 0,774327497358586 0,563500011136522 0,565475642013119 0,414556856460197 0,303055067687787 0,383227259099124 0,427279238788075 0,848288005147263 0,374969424854484 0,354406009593236 0,351577740791988 0,194683879238872 0,990434869653748 0,50676403963322 0,245935701879643"
				.Split(' ').Select(double.Parse).ToArray();

			var ga = new GA(config, new Random());
			ga.Simulate(simulator, 0, genome);

			Console.Out.WriteLine(ga.Evaluate(simulator, 0));

		}

		public static void Main123123()
		{
			Logger.minLevel = Logger.Level.Debug;

			var config = JsonConvert.DeserializeObject<Config>(
				@"{""GAME_HEIGHT"":660,""GAME_WIDTH"":660,""GAME_TICKS"":75000,""FOOD_MASS"":1.0,""MAX_FRAGS_CNT"":10,""TICKS_TIL_FUSION"":250,""VIRUS_RADIUS"":22.0,""VIRUS_SPLIT_MASS"":80.0,""VISCOSITY"":0.25,""INERTION_FACTOR"":10.0,""SPEED_FACTOR"":25.0}");

			var turnInput = JsonConvert.DeserializeObject<TurnInput>(
				@"{""Mine"":[{""Id"":""1"",""X"":282.764659119549,""Y"":255.00047968359704,""SX"":2.3029305366512363,""SY"":1.1739094325097006,""R"":12.649110640673518,""M"":40.0,""TTF"":3014700}],""Objects"":[{""Id"":null,""pId"":null,""T"":""F"",""X"":299.0,""Y"":225.0,""R"":0.0,""M"":0.0}]}");

			//var gaStrategy = new GAStrategy(config);
			//var turnOutput1 = gaStrategy.OnTick(turnInput);
			//Console.Out.WriteLine(JsonConvert.SerializeObject(turnOutput1));

			var log = new DirectoryInfo(FileHelper.PatchDirectoryName("logs")).GetFiles("*.txt").OrderBy(x => x.Name).Select(x => x.FullName).Last();

			var bitmap = new Bitmap(config.GAME_WIDTH, config.GAME_HEIGHT);
			using (var graphics = Graphics.FromImage(bitmap))
			{
				foreach (var mine in turnInput.Mine)
				{
					Draw(mine.X, mine.Y, mine.R, Color.Green);
				}

				var colors = new Dictionary<string, Color>
				{
					{"F", Color.Magenta},
					{"E", Color.Red},
				};
				foreach (var obj in turnInput.Objects)
				{
					Draw((int) obj.X, (int) obj.Y, obj.R, colors[obj.T]);
				}

				var lineColors = new[] {Color.Blue, Color.Red, Color.Wheat, Color.CornflowerBlue, Color.DarkOliveGreen, Color.DarkOrchid, Color.GreenYellow };

				var lines = File.ReadAllLines(log);

				var routes = new List<(double score, double[] genome, List<Point> route)>();
				Point cur;
				
				foreach (var line in lines)
				{
					if (line.IndexOf("BEFORE: ") >= 0)
					{
						cur = new Point(turnInput.Mine[0].X, turnInput.Mine[0].Y);
						routes.Add((0, null, new List<Point> { cur }));
					}
					else if (line.IndexOf("GROUP: ") >= 0)
					{
						var nexts = line.Substring(line.IndexOf("GROUP: ") + "GROUP: ".Length).Split(' ');
						var x = double.Parse(nexts[0]);
						var y = double.Parse(nexts[1]);
						var next = new Point(x, y);
						routes.Last().route.Add(next);
						cur = next;
					}else if (line.IndexOf("AFTER: ") >= 0)
					{
						var ss = line.Substring(line.IndexOf("AFTER: ") + "AFTER: ".Length).Split(' ');
						var score = double.Parse(ss[0]);
						var genome = ss.Skip(1).Select(double.Parse).ToArray();
						routes.Add((score, genome, routes.Last().route));
						routes.RemoveAt(routes.Count - 2);
					}
				}

				var l = 0;
				//foreach (var r in routes.OrderByDescending(x => x.score).Take(7))
				foreach (var r in routes.Take(7))
				{
					var route = r.route;
					l = (l + 1) % lineColors.Length;
					for (int i = 0; i < route.Count - 1; i++)
					{
						using (var pen = new Pen(lineColors[l]))
							graphics.DrawLine(pen, (int)route[i].x, (int)route[i].y, (int)route[i+1].x, (int)route[i+1].y);
					}
				}

				foreach (var gline in lines.Where(ll => ll.IndexOf("GLOBAL: ") >= 0))
				{
					var gs = gline.Substring(gline.IndexOf("GLOBAL: ") + "GLOBAL: ".Length).Split(' ');
					Draw(double.Parse(gs[0]), double.Parse(gs[1]), 5, Color.DarkSalmon);
					Draw(double.Parse(gs[0]), double.Parse(gs[1]), 4, Color.DarkSalmon);
					Draw(double.Parse(gs[0]), double.Parse(gs[1]), 3, Color.DarkSalmon);
					Draw(double.Parse(gs[0]), double.Parse(gs[1]), 2, Color.DarkSalmon);
				}


				void Draw(double x, double y, double radius, Color color)
				{
					radius = Math.Max(radius, 3);
					using (var pen = new Pen(color))
						graphics.DrawEllipse(pen, (int) (x - radius), (int) (y - radius), (int) radius*2, (int) radius*2);
				}
			}

			bitmap.Save(log + "-first.jpg");


			/*gaStrategy = new GAStrategy(config);
			var turnOutput2 = gaStrategy.OnTick(turnInput);*/

			
			//Console.Out.WriteLine(JsonConvert.SerializeObject(turnOutput2));
		}

		public static void Main12312312()
		{
			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":1.1699651787931546,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":18.311331992550009,""MAX_FRAGS_CNT"":4,""SPEED_FACTOR"":61.964198190568723,""TICKS_TIL_FUSION"":428,""VIRUS_RADIUS"":27.671011120486231,""VIRUS_SPLIT_MASS"":82.838148942917172,""VISCOSITY"":0.1961202661671938}");
			var mechanic = new Mechanic(config, new List<PlayerStrategy>
			{
				new PlayerStrategy(config, c => new SimpleStrategy(c)),
				new PlayerStrategy(config, c => new FastAiStrategy(c, new SimpleFastAi(c))),
				new PlayerStrategy(config, c => new NearestFoodStrategy(c, true)),
				new PlayerStrategy(config, c => new NearestFoodStrategy(c, false)),
			});
			mechanic.Play();
			foreach (var kvp in mechanic.playerScores)
			{
				Console.Out.WriteLine($"{mechanic.strategies[kvp.Key]}: {kvp.Value}");
			}
		}

		public static void Main()
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