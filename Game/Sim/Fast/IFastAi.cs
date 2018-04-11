using System;
using Game.Protocol;

namespace Game.Sim.Fast
{
	public unsafe interface IFastAi
	{
		FastDirect GetDirect(FastGlobalState* global, FastState* state, int player);
	}

	public unsafe class SimpleFastAi : IFastAi
	{
		private readonly Config config;

		public SimpleFastAi(Config config)
		{
			this.config = config;
		}

		public FastDirect GetDirect(FastGlobalState* global, FastState* state, int player)
		{
			var minDist = double.PositiveInfinity;
			var fragments = &state->fragments0 + player;
			var frag = (FastFragment*)fragments->data;
			FastPoint* target = null;
			FastFragment* awayTarget = null;
			FastFragment* source = null;
			for (int i = 0; i < fragments->count; i++, frag++)
			{
				var food = (FastPoint*)state->foods.data;
				for (int f = 0; f < state->foods.count; f++, food++)
				{
					var qdist = frag->QDistance(food);
					if (qdist < minDist)
					{
						minDist = qdist;
						source = frag;
						target = food;
						awayTarget = null;
					}
				}

				var efragments = &state->fragments0;
				for (int e = 0; e < 4; e++, efragments++)
				{
					if (e == player)
						continue;
					var efrag = (FastFragment*)efragments->data;
					for (int ef = 0; ef < efragments->count; ef++, efrag++)
					{
						var qdist = frag->QDistance(efrag);
						if (qdist < minDist)
						{
							if (frag->Eatable(efrag))
							{
								minDist = qdist;
								source = frag;
								target = (FastPoint*)efrag;
								awayTarget = null;
							}
							else if (efrag->Eatable(frag))
							{
								minDist = qdist;
								target = null;
								source = frag;
								awayTarget = efrag;
							}
						}
					}
				}
			}

			if (target == null && awayTarget == null)
			{
				if (player != 0)
					throw new InvalidOperationException("Couldn't obtain target for player != 0");
				target = (FastPoint*)global->checkpoints.data + state->nextCheckpoint;
			}

			FastDirect result;
			if (target != null)
			{
				if (source == null)
					result = new FastDirect(target->x, target->y);
				else
				{
					var factor = config.INERTION_FACTOR / source->mass - 1;
					var nx = target->x + source->speed * source->ndx * factor;
					var ny = target->y + source->speed * source->ndy * factor;
					result = new FastDirect(nx, ny);
				}
			}
			else
			{
				result = new FastDirect(source->x + source->x - awayTarget->x, source->y + source->y - awayTarget->y);
			}
			result.Limit(config);
			return result;
		}
	}
}