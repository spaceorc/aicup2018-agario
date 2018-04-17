using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Game.Ai;
using Game.Helpers;
using Game.Mech;
using Game.Protocol;
using Game.Sim;
using Game.Strategies;
using Game.Types;
using Newtonsoft.Json;
using Point = Game.Types.Point;

namespace Experiments
{
	internal unsafe class Program
	{
		public static void Main124312()
		{
			Logger.enableFile = false;
			Logger.enableConsole = true;
			Logger.minLevel = Logger.Level.Debug;

			var config = JsonConvert.DeserializeObject<Config>(
				@"{""GAME_HEIGHT"":660,""GAME_WIDTH"":660,""GAME_TICKS"":75000,""FOOD_MASS"":1.0,""MAX_FRAGS_CNT"":10,""TICKS_TIL_FUSION"":250,""VIRUS_RADIUS"":22.0,""VIRUS_SPLIT_MASS"":80.0,""VISCOSITY"":0.25,""INERTION_FACTOR"":10.0,""SPEED_FACTOR"":25.0}");

			//{
			//	var turnInput = JsonConvert.DeserializeObject<TurnInput>(
			//		@"{""Mine"":[{""Id"":""4.2"",""M"":67.633442164008983,""R"":16.447910768727922,""SX"":-0.17497948845230288,""SY"":0.37057310278552424,""TTF"":129,""X"":873.84057938669969,""Y"":284.79763238540119},{""Id"":""4.1"",""M"":77.504022747462827,""R"":17.607273809134998,""SX"":-0.15114037419667012,""SY"":0.30010456527392138,""TTF"":129,""X"":904.78855873536077,""Y"":262.6669254192426}],""Objects"":[{""T"":""F"",""X"":937,""Y"":240},{""T"":""F"",""X"":848,""Y"":306},{""Id"":""21"",""M"":40,""T"":""V"",""X"":345.12157105167103,""Y"":393.12157105167103},{""Id"":""22"",""M"":40,""T"":""V"",""X"":644.87842894832897,""Y"":393.12157105167103},{""Id"":""23"",""M"":40,""T"":""V"",""X"":644.87842894832897,""Y"":596.87842894832897},{""Id"":""24"",""M"":40,""T"":""V"",""X"":345.12157105167103,""Y"":596.87842894832897}]}");

			//	var simState = new State(config);
			//	simState.Apply(turnInput);

			//	var ai = new SimulationAi(config, new Evaluation(config, new EvaluationArgs()), 10, false,
			//		new SimpleAi(config));

			//	var global = new FastGlobal();
			//	global.checkpoints.Add(268.20020085559935, 489.52990276729895);

			//	var state = new Simulator(simState, 0);

			//	var direct = ai.GetDirect(&global, &state, 0);

			//	Console.Out.WriteLine(direct);
			//}

			//{
			//	var turnInput = JsonConvert.DeserializeObject<TurnInput>(
			//		@"{""Mine"":[{""Id"":""4.2"",""M"":67.633442164008983,""R"":16.447910768727922,""SX"":0.58093514692431203,""SY"":-0.20863814994066202,""TTF"":128,""X"":874.42151453362396,""Y"":284.58899423546052},{""Id"":""4.1"",""M"":77.504022747462827,""R"":17.607273809134998,""SX"":0.46413424705286627,""SY"":-0.16413419361259846,""TTF"":128,""X"":905.25269298241358,""Y"":262.50279122563001}],""Objects"":[{""T"":""F"",""X"":937,""Y"":240},{""T"":""F"",""X"":848,""Y"":306},{""Id"":""21"",""M"":40,""T"":""V"",""X"":345.12157105167103,""Y"":393.12157105167103},{""Id"":""22"",""M"":40,""T"":""V"",""X"":644.87842894832897,""Y"":393.12157105167103},{""Id"":""23"",""M"":40,""T"":""V"",""X"":644.87842894832897,""Y"":596.87842894832897},{""Id"":""24"",""M"":40,""T"":""V"",""X"":345.12157105167103,""Y"":596.87842894832897}]}");

			//	var simState = new State(config);
			//	simState.Apply(turnInput);

			//	var ai = new SimulationAi(config, new Evaluation(config, new EvaluationArgs()), 10, false,
			//		new SimpleAi(config));

			//	var global = new FastGlobal();
			//	global.checkpoints.Add(268.20020085559935, 489.52990276729895);

			//	var state = new Simulator(simState, 0);

			//	var direct = ai.GetDirect(&global, &state, 0);

			//	Console.Out.WriteLine(direct);
			//}

			{
				var prevTurnInput = JsonConvert.DeserializeObject<TurnInput>(
					@"{""Mine"":[{""Id"":""1"",""X"":576.82139574124665,""Y"":547.776073934072,""SX"":0.48451889099582646,""SY"":-0.45385295600803383,""R"":53.141363072648822,""M"":706.001117304771,""TTF"":0}],""Objects"":[{""T"":""F"",""X"":531.0,""Y"":415.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":486.0,""Y"":454.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":425.0,""Y"":491.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":504.0,""Y"":528.0,""R"":0.0,""M"":0.0},{""Id"":""4.3"",""T"":""P"",""X"":548.31861736527048,""Y"":508.63565274308996,""R"":15.99928123075397,""M"":63.994249975189071},{""Id"":""4.1"",""T"":""P"",""X"":644.00071876924608,""Y"":368.71200238171156,""R"":15.99928123075397,""M"":63.994249975189071},{""Id"":""4.2"",""T"":""P"",""X"":640.02182976337554,""Y"":490.14268052524665,""R"":15.99928123075397,""M"":63.994249975189071},{""Id"":""21"",""T"":""V"",""X"":146.0,""Y"":205.0,""R"":0.0,""M"":40.0},{""Id"":""24"",""T"":""V"",""X"":146.0,""Y"":455.0,""R"":0.0,""M"":40.0},{""Id"":""266"",""T"":""V"",""X"":614.0,""Y"":123.0,""R"":0.0,""M"":40.0},{""Id"":""509"",""T"":""V"",""X"":89.0,""Y"":247.0,""R"":0.0,""M"":40.0},{""Id"":""753"",""T"":""V"",""X"":277.0,""Y"":96.0,""R"":0.0,""M"":40.0},{""Id"":""754"",""T"":""V"",""X"":383.0,""Y"":96.0,""R"":0.0,""M"":40.0},{""Id"":""755"",""T"":""V"",""X"":383.0,""Y"":564.0,""R"":0.0,""M"":40.0},{""Id"":""756"",""T"":""V"",""X"":277.0,""Y"":564.0,""R"":0.0,""M"":40.0}]}");

				var turnInput = JsonConvert.DeserializeObject<TurnInput>(
					@"{""Mine"":[{""Id"":""1"",""X"":577.30890001436046,""Y"":547.31967058184318,""SX"":0.48750427311379474,""SY"":-0.45640335222875072,""R"":55.497580750153787,""M"":769.99536727996008,""TTF"":0}],""Objects"":[{""T"":""F"",""X"":418.0,""Y"":398.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":531.0,""Y"":415.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":486.0,""Y"":454.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":425.0,""Y"":491.0,""R"":0.0,""M"":0.0},{""T"":""F"",""X"":504.0,""Y"":528.0,""R"":0.0,""M"":0.0},{""Id"":""4.1"",""T"":""P"",""X"":644.00071876924608,""Y"":365.43749415199926,""R"":15.99928123075397,""M"":63.994249975189071},{""Id"":""4.2"",""T"":""P"",""X"":638.30927224860636,""Y"":491.9947000996508,""R"":15.99928123075397,""M"":63.994249975189071},{""Id"":""21"",""T"":""V"",""X"":146.0,""Y"":205.0,""R"":0.0,""M"":40.0},{""Id"":""24"",""T"":""V"",""X"":146.0,""Y"":455.0,""R"":0.0,""M"":40.0},{""Id"":""266"",""T"":""V"",""X"":614.0,""Y"":123.0,""R"":0.0,""M"":40.0},{""Id"":""509"",""T"":""V"",""X"":89.0,""Y"":247.0,""R"":0.0,""M"":40.0},{""Id"":""753"",""T"":""V"",""X"":277.0,""Y"":96.0,""R"":0.0,""M"":40.0},{""Id"":""754"",""T"":""V"",""X"":383.0,""Y"":96.0,""R"":0.0,""M"":40.0},{""Id"":""755"",""T"":""V"",""X"":383.0,""Y"":564.0,""R"":0.0,""M"":40.0},{""Id"":""756"",""T"":""V"",""X"":277.0,""Y"":564.0,""R"":0.0,""M"":40.0}]}");

				var simState = new State(config);
				simState.Apply(prevTurnInput);
				simState.Apply(turnInput);

				var ai = new SimulationAi(config, new Evaluation(config, EvaluationArgs.CreateDefault()), 10, true,
					new SimpleAi(config));

				var global = new FastGlobal();
				global.checkpoints.Add(268.20020085559935, 489.52990276729895);

				var state = new Simulator(simState, 0);

				var direct = ai.GetDirect(&global, &state, 0, new TimeManager(config));

				Console.Out.WriteLine(direct);
			}
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

		public static void Main45()
		{
			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":1.1699651787931546,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":18.311331992550009,""MAX_FRAGS_CNT"":4,""SPEED_FACTOR"":61.964198190568723,""TICKS_TIL_FUSION"":428,""VIRUS_RADIUS"":27.671011120486231,""VIRUS_SPLIT_MASS"":82.838148942917172,""VISCOSITY"":0.1961202661671938}");
			var mechanic = new Mechanic(config, new List<PlayerStrategy>
			{
				new PlayerStrategy(config, c => new Strategy(c, new SimpleAi(c))),
				new PlayerStrategy(config, c => new Strategy(c, new SimpleAi(c))),
				new PlayerStrategy(config, c => new Strategy(c, new SimpleAi(c))),
				new PlayerStrategy(config, c => new Strategy(c, new SimulationAi(c, new Evaluation(c, EvaluationArgs.CreateDefault()), 10, false, new SimpleAi(c)))),
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