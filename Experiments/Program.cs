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
		public static void Main121212()
		{
			Logger.enableFile = false;
			Logger.enableConsole = true;
			Logger.minLevel = Logger.Level.Info;

			var dump = "D:\\Downloads\\173530_dump.log";
			var lines = File.ReadAllLines(dump);

			var config = JsonConvert.DeserializeObject<Config>(lines[0]);


			{
				var inputs = lines.SkipWhile(l => l != "T" + 1755).ToList();
				var turnInputs = new List<TurnInput>();
				for (int k = 0; k < 20; k++)
				{
					turnInputs.Add(JsonConvert.DeserializeObject<TurnInput>(inputs[k * 4 + 1]));
				}

				var ai = new SimulationAi(config, new FixedEvaluation(config, EvaluationArgs.CreateFixed()), 7, true,
					new SimpleAi(config));

				var global = new FastGlobal();
				global.checkpoints.Add(0, 0);
				
				var simState = new State(config, true);
				foreach (var turnInput in turnInputs)
				{
					simState.Apply(turnInput);
					var state = new Simulator(simState, 0);
					var direct = ai.GetDirect(&global, &state, 0, new TimeManager(config));
					Console.Out.WriteLine(direct);
				}
			}
		}

		public static void Main_X()
		{
			Logger.enableFile = false;
			Logger.enableConsole = true;
			Logger.minLevel = Logger.Level.Debug;

			var dump = "D:\\Downloads\\173530_dump.log";
			var lines = File.ReadAllLines(dump);

			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":3.3367345502164101,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":15.098544076033486,""MAX_FRAGS_CNT"":7,""SPEED_FACTOR"":95.469368125824587,""TICKS_TIL_FUSION"":326,""VIRUS_RADIUS"":35.518560095127832,""VIRUS_SPLIT_MASS"":99.95162648148262,""VISCOSITY"":0.36555321345247982}");

			//var config = JsonConvert.DeserializeObject<Config>(
			//	@"{""FOOD_MASS"":3.3367345502164101,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":15.098544076033486,""MAX_FRAGS_CNT"":7,""SPEED_FACTOR"":95.469368125824587,""TICKS_TIL_FUSION"":326,""VIRUS_RADIUS"":35.518560095127832,""VIRUS_SPLIT_MASS"":99.95162648148262,""VISCOSITY"":0.36555321345247982}");


			{
				var prevTurnInput = JsonConvert.DeserializeObject<TurnInput>(
					@"{""Mine"":[{""Id"":""4"",""M"":213.38469921228523,""R"":29.215386303267341,""SX"":-2.320838373496831,""SY"":1.3390102472211514,""X"":553.87814166777969,""Y"":619.90304358343735}],""Objects"":[{""Id"":""3.21"",""M"":138.31563669149909,""R"":23.521533682266476,""T"":""P"",""X"":481.03608764035147,""Y"":736.1905360202353},{""Id"":""3.19"",""M"":144.49232587482297,""R"":24.040992148813075,""T"":""P"",""X"":523.27581017241209,""Y"":677.2320981443761},{""Id"":""2.19"",""M"":90.61927249392734,""R"":19.038831108440174,""T"":""P"",""X"":585.98812839028233,""Y"":513.83639261531494},{""Id"":""3.16"",""M"":204.58274876482147,""R"":28.606485192335075,""T"":""P"",""X"":445.21151521638546,""Y"":668.80224586120744},{""Id"":""3.18"",""M"":144.49232587482297,""R"":24.040992148813075,""T"":""P"",""X"":529.40275163970512,""Y"":633.54105622338864},{""Id"":""3.20"",""M"":138.31563669149909,""R"":23.521533682266476,""T"":""P"",""X"":538.15565730510423,""Y"":716.285820339662},{""Id"":""22"",""M"":40,""T"":""V"",""X"":857.64631010692142,""Y"":190.35368989307855},{""Id"":""24"",""M"":40,""T"":""V"",""X"":132.35368989307855,""Y"":799.64631010692142},{""Id"":""265"",""M"":40,""T"":""V"",""X"":149.35368989307855,""Y"":292.35368989307858},{""Id"":""266"",""M"":40,""T"":""V"",""X"":840.64631010692142,""Y"":292.35368989307858},{""Id"":""267"",""M"":40,""T"":""V"",""X"":840.64631010692142,""Y"":697.64631010692142},{""Id"":""268"",""M"":40,""T"":""V"",""X"":149.35368989307855,""Y"":697.64631010692142},{""Id"":""509"",""M"":40,""T"":""V"",""X"":218.35368989307855,""Y"":100.35368989307855},{""Id"":""510"",""M"":40,""T"":""V"",""X"":771.64631010692142,""Y"":100.35368989307855},{""Id"":""511"",""M"":40,""T"":""V"",""X"":771.64631010692142,""Y"":889.64631010692142}]}");

				var turnInput = JsonConvert.DeserializeObject<TurnInput>(
					@"{""Mine"":[{""Id"":""4"",""M"":212.25085222016239,""R"":29.137663064848724,""SX"":-2.2662482831309934,""SY"":1.1174611269795731,""X"":551.61189338464874,""Y"":621.02050471041696}],""Objects"":[{""Id"":""3.21"",""M"":137.93248032458411,""R"":23.488931889260876,""T"":""P"",""X"":484.10997748183672,""Y"":734.16999754100402},{""Id"":""3.19"",""M"":144.04740261607475,""R"":24.003949892971761,""T"":""P"",""X"":525.68488355219813,""Y"":675.6960918853764},{""Id"":""3.15"",""M"":140.41835494388062,""R"":23.69965020365327,""T"":""P"",""X"":440.65371047795225,""Y"":719.7462286521195},{""Id"":""2.19"",""M"":90.61927249392734,""R"":19.038831108440174,""T"":""P"",""X"":589.18054281487775,""Y"":506.94170278645095},{""Id"":""3.16"",""M"":203.53692127717326,""R"":28.533273298181072,""T"":""P"",""X"":447.15604267963499,""Y"":666.68175387296401},{""Id"":""3.18"",""M"":144.04740261607475,""R"":24.003949892971761,""T"":""P"",""X"":529.76933598599419,""Y"":633.18847789158281},{""Id"":""3.20"",""M"":137.93248032458411,""R"":23.488931889260876,""T"":""P"",""X"":540.00200897651791,""Y"":713.13677081364722},{""Id"":""22"",""M"":40,""T"":""V"",""X"":857.64631010692142,""Y"":190.35368989307855},{""Id"":""24"",""M"":40,""T"":""V"",""X"":132.35368989307855,""Y"":799.64631010692142},{""Id"":""265"",""M"":40,""T"":""V"",""X"":149.35368989307855,""Y"":292.35368989307858},{""Id"":""266"",""M"":40,""T"":""V"",""X"":840.64631010692142,""Y"":292.35368989307858},{""Id"":""267"",""M"":40,""T"":""V"",""X"":840.64631010692142,""Y"":697.64631010692142},{""Id"":""268"",""M"":40,""T"":""V"",""X"":149.35368989307855,""Y"":697.64631010692142},{""Id"":""509"",""M"":40,""T"":""V"",""X"":218.35368989307855,""Y"":100.35368989307855},{""Id"":""510"",""M"":40,""T"":""V"",""X"":771.64631010692142,""Y"":100.35368989307855},{""Id"":""511"",""M"":40,""T"":""V"",""X"":771.64631010692142,""Y"":889.64631010692142}]}");

				var simState = new State(config, true);
				simState.Apply(prevTurnInput);
				simState.Apply(turnInput);

				var ai = new SimulationAi(config, new FixedEvaluation(config, EvaluationArgs.CreateFixed()), 7, true,
					new SimpleAi(config));

				var checkpoints =
					"379,136;309,740;902,365;534,453;212,699;887,816;363,281;65,792;481,850;503,278;71,374;253,695;909,216;890,592;388,913;781,155";
				var nextCheckpoint = 14;

				var global = new FastGlobal();
				foreach (var cps in checkpoints.Split(';'))
				{
					var cp = cps.Split(',');
					global.checkpoints.Add(double.Parse(cp[0]), double.Parse(cp[1]));
				}

				var state = new Simulator(simState, nextCheckpoint);

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
				new PlayerStrategy(config, c => new Strategy(c, new SimpleAi(c), true)),
				new PlayerStrategy(config, c => new Strategy(c, new SimpleAi(c), true)),
				new PlayerStrategy(config, c => new Strategy(c, new SimpleAi(c), true)),
				new PlayerStrategy(config, c => new Strategy(c, new SimulationAi(c, new Evaluation(c, EvaluationArgs.CreateDefault()), 10, false, new SimpleAi(c)), true)),
			});
			mechanic.Play();
			foreach (var kvp in mechanic.playerScores)
			{
				Console.Out.WriteLine($"{mechanic.strategies[kvp.Key]}: {kvp.Value}");
			}
		}

		// brutal tester
		public static void Main()
		{
			Logger.enableConsole = false;
			Logger.enableFile = false;
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