using System;
using System.Diagnostics;
using System.Linq;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Types;
using Game.Strategies;
using Game.Types;
using Newtonsoft.Json;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public unsafe class Simuator_Test
	{
		[Test]
		public void METHOD()
		{
			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":1.1699651787931546,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":18.311331992550009,""MAX_FRAGS_CNT"":4,""SPEED_FACTOR"":61.964198190568723,""TICKS_TIL_FUSION"":428,""VIRUS_RADIUS"":27.671011120486231,""VIRUS_SPLIT_MASS"":82.838148942917172,""VISCOSITY"":0.1961202661671938}");
			var turnInput = JsonConvert.DeserializeObject<TurnInput>(
				@"{""Mine"":[{""Id"":""2.4"",""M"":68.5728535406204,""R"":16.561745504700934,""SX"":5.9930106236799006,""SY"":3.771832101187925,""TTF"":184,""X"":358.35100949819929,""Y"":329.43087243626752},{""Id"":""2.1"",""M"":68.5728535406204,""R"":16.561745504700934,""SX"":5.6600164324445936,""SY"":3.7565868588049112,""TTF"":184,""X"":347.47052173334743,""Y"":300.39993486208209},{""Id"":""2.2"",""M"":69.742818719413549,""R"":16.7024332023108,""SX"":5.0875474799896336,""SY"":4.0734603382629979,""TTF"":184,""X"":378.58030426799132,""Y"":308.14944955196643},{""Id"":""2.3"",""M"":65.062958004240954,""R"":16.132322585944152,""SX"":5.0019605021731648,""SY"":4.573778974891427,""TTF"":184,""X"":394.43927479192507,""Y"":335.76913777088782}],""Objects"":[{""T"":""F"",""X"":420,""Y"":400},{""T"":""F"",""X"":420,""Y"":400},{""Id"":""21"",""M"":40,""T"":""V"",""X"":272.34202224097248,""Y"":163.34202224097245},{""Id"":""22"",""M"":40,""T"":""V"",""X"":717.65797775902752,""Y"":163.34202224097245},{""Id"":""23"",""M"":40,""T"":""V"",""X"":717.65797775902752,""Y"":826.65797775902752},{""Id"":""24"",""M"":40,""T"":""V"",""X"":272.34202224097248,""Y"":826.65797775902752},{""Id"":""265"",""M"":40,""T"":""V"",""X"":392.34202224097248,""Y"":349.34202224097248},{""Id"":""266"",""M"":40,""T"":""V"",""X"":597.65797775902752,""Y"":349.34202224097248},{""Id"":""268"",""M"":40,""T"":""V"",""X"":392.34202224097248,""Y"":640.65797775902752},{""Id"":""509"",""M"":40,""T"":""V"",""X"":439.34202224097248,""Y"":383.34202224097248},{""Id"":""510"",""M"":40,""T"":""V"",""X"":550.65797775902752,""Y"":383.34202224097248},{""Id"":""511"",""M"":40,""T"":""V"",""X"":550.65797775902752,""Y"":606.65797775902752},{""Id"":""512"",""M"":40,""T"":""V"",""X"":439.34202224097248,""Y"":606.65797775902752}]}");


			var simState = new State(config);
			simState.Apply(turnInput);


			var gs = new FastGlobal();
			gs.checkpoints.Add(0, 0);
			gs.checkpoints.Add(0, 0);
			gs.checkpoints.Add(0, 0);
			gs.checkpoints.Add(0, 0);

			var fs = new Simulator(simState, 0);

			Console.Out.WriteLine($"size: {sizeof(Simulator)}");

			var directs = new FastDirect.List();
			directs.Add(new FastDirect(0, 0));
			directs.Add(new FastDirect(0, 0));
			directs.Add(new FastDirect(0, 0));
			directs.Add(new FastDirect(0, 0));

			var stopwatch = Stopwatch.StartNew();

			for (int iter = 0; iter < 100000; iter++)
			{
				var clone = fs;
				for (int ticks = 0; ticks < 10; ticks++)
				{
					clone.Tick(&gs, &directs, config);
				}
			}

			stopwatch.Stop();

			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
		}

		[Test]
		public void METHOD2()
		{
			var config = JsonConvert.DeserializeObject<Config>(
				@"{""FOOD_MASS"":1.1699651787931546,""GAME_HEIGHT"":990,""GAME_TICKS"":7500,""GAME_WIDTH"":990,""INERTION_FACTOR"":18.311331992550009,""MAX_FRAGS_CNT"":4,""SPEED_FACTOR"":61.964198190568723,""TICKS_TIL_FUSION"":428,""VIRUS_RADIUS"":27.671011120486231,""VIRUS_SPLIT_MASS"":82.838148942917172,""VISCOSITY"":0.1961202661671938}");
			var turnInput = JsonConvert.DeserializeObject<TurnInput>(
				@"{""Mine"":[{""Id"":""2.4"",""M"":68.5728535406204,""R"":16.561745504700934,""SX"":5.9930106236799006,""SY"":3.771832101187925,""TTF"":184,""X"":358.35100949819929,""Y"":329.43087243626752},{""Id"":""2.1"",""M"":68.5728535406204,""R"":16.561745504700934,""SX"":5.6600164324445936,""SY"":3.7565868588049112,""TTF"":184,""X"":347.47052173334743,""Y"":300.39993486208209},{""Id"":""2.2"",""M"":69.742818719413549,""R"":16.7024332023108,""SX"":5.0875474799896336,""SY"":4.0734603382629979,""TTF"":184,""X"":378.58030426799132,""Y"":308.14944955196643},{""Id"":""2.3"",""M"":65.062958004240954,""R"":16.132322585944152,""SX"":5.0019605021731648,""SY"":4.573778974891427,""TTF"":184,""X"":394.43927479192507,""Y"":335.76913777088782}],""Objects"":[{""T"":""F"",""X"":420,""Y"":400},{""T"":""F"",""X"":420,""Y"":400},{""Id"":""21"",""M"":40,""T"":""V"",""X"":272.34202224097248,""Y"":163.34202224097245},{""Id"":""22"",""M"":40,""T"":""V"",""X"":717.65797775902752,""Y"":163.34202224097245},{""Id"":""23"",""M"":40,""T"":""V"",""X"":717.65797775902752,""Y"":826.65797775902752},{""Id"":""24"",""M"":40,""T"":""V"",""X"":272.34202224097248,""Y"":826.65797775902752},{""Id"":""265"",""M"":40,""T"":""V"",""X"":392.34202224097248,""Y"":349.34202224097248},{""Id"":""266"",""M"":40,""T"":""V"",""X"":597.65797775902752,""Y"":349.34202224097248},{""Id"":""268"",""M"":40,""T"":""V"",""X"":392.34202224097248,""Y"":640.65797775902752},{""Id"":""509"",""M"":40,""T"":""V"",""X"":439.34202224097248,""Y"":383.34202224097248},{""Id"":""510"",""M"":40,""T"":""V"",""X"":550.65797775902752,""Y"":383.34202224097248},{""Id"":""511"",""M"":40,""T"":""V"",""X"":550.65797775902752,""Y"":606.65797775902752},{""Id"":""512"",""M"":40,""T"":""V"",""X"":439.34202224097248,""Y"":606.65797775902752}]}");

			turnInput.Mine = turnInput.Mine.Take(1).ToArray();
			turnInput.Mine[0].M = 200;
			turnInput.Mine[0].R = Constants.PLAYER_RADIUS_FACTOR * Math.Sqrt(turnInput.Mine[0].M);
			var virus = turnInput.Objects.First(o => o.T == "V");
			virus.X = turnInput.Mine[0].X - 10;
			virus.Y = turnInput.Mine[0].Y - 10;

			var simState = new State(config);
			simState.Apply(turnInput);

			var gs = new FastGlobal();
			gs.checkpoints.Add(0, 0);
			gs.checkpoints.Add(0, 0);
			gs.checkpoints.Add(0, 0);
			gs.checkpoints.Add(0, 0);

			var fs = new Simulator(simState, 0);
			var directs = new FastDirect.List();
			directs.Add(new FastDirect(0, 0));
			directs.Add(new FastDirect(0, 0));
			directs.Add(new FastDirect(0, 0));
			directs.Add(new FastDirect(0, 0));

			fs.Tick(&gs, &directs, config);
			fs.Tick(&gs, &directs, config);
			fs.Tick(&gs, &directs, config);
			fs.Tick(&gs, &directs, config);

			for (var i = 0; i < 4; i++)
			{
				Console.Out.WriteLine($"Player {i}");
				for (int j = 0; j < (&fs.fragments0 + i)->count; j++)
				{
					var frags = (FastFragment*) (&fs.fragments0 + i)->data;
					Console.Out.WriteLine(frags[j]);
				}
			}
		}
	}
}