using Game.Protocol;
using Game.Sim;
using Game.Sim.Types;

namespace Game.Ai
{
	public unsafe class SimpleAi : IAi
	{
		private readonly Config config;

		public SimpleAi(Config config)
		{
			this.config = config;
		}

		public FastDirect GetDirect(FastGlobal* global, Simulator* state, int player)
		{
			var minDist = double.PositiveInfinity;
			var fragments = &state->fragments0 + player;
			FastPoint* target = null;
			FastFragment* awayTarget = null;
			FastFragment* source = null;
			var frag = (FastFragment*)fragments->data;
			for (var i = 0; i < fragments->count; i++, frag++)
			{
				var food = (FastPoint*)state->foods.data;
				for (var f = 0; f < state->foods.count; f++, food++)
				{
					if (food->x < frag->radius && food->y < frag->radius)
					{
						if (food->QDistance(new FastPoint(frag->radius, frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					if (food->x < frag->radius && food->y > config.GAME_HEIGHT - frag->radius)
					{
						if (food->QDistance(new FastPoint(frag->radius, config.GAME_HEIGHT - frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					if (food->x > config.GAME_WIDTH - frag->radius && food->y < frag->radius)
					{
						if (food->QDistance(new FastPoint(config.GAME_WIDTH - frag->radius, frag->radius)) > frag->radius * frag->radius)
							continue;
					}
					if (food->x > config.GAME_WIDTH - frag->radius && food->y > config.GAME_HEIGHT - frag->radius)
					{
						if (food->QDistance(new FastPoint(config.GAME_WIDTH - frag->radius, config.GAME_HEIGHT - frag->radius)) > frag->radius * frag->radius)
							continue;
					}
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
				for (var e = 0; e < 4; e++, efragments++)
				{
					if (e == player)
						continue;
					var efrag = (FastFragment*)efragments->data;
					for (var ef = 0; ef < efragments->count; ef++, efrag++)
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
				if (player == 0)
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
			else if (awayTarget != null)
				result = new FastDirect(source->x + source->x - awayTarget->x, source->y + source->y - awayTarget->y);
			else
				result = new FastDirect(0, 0);

			result.Limit(config);
			return result;
		}
	}
}