using Game.Protocol;

namespace Game.Types
{
	public class Direct : Point
	{
		public readonly Config config;
		public string debug;
		public string debugSpriteId;
		public string debugSpriteInfo;
		public bool eject;
		public bool split;

		public Direct(Point target, Config config, bool split = false, bool eject = false) 
			: this(target.x, target.y, config, split, eject)
		{
		}

		public Direct(double x, double y, Config config, bool split = false, bool eject = false) : base(x, y)
		{
			this.config = config;
			this.split = split;
			this.eject = eject;
		}

		public override string ToString()
		{
			return $"{base.ToString()}{(split ? ", split" : "")}{(eject ? ", eject" : "")}";
		}

		public void Limit()
		{
			if (x > config.GAME_WIDTH)
				x = config.GAME_WIDTH;
			else if (x < 0)
				x = 0;

			if (y > config.GAME_HEIGHT)
				y = config.GAME_HEIGHT;
			else if (y < 0)
				y = 0;
		}

		public TurnOutput ToOutput()
		{
			return new TurnOutput
			{
				X = x,
				Y = y,
				Split = split,
				Eject = eject,
				Debug = debug,
				Sprite = string.IsNullOrEmpty(debugSpriteInfo)
					? null
					: new TurnOutput.SpriteData
					{
						Id = debugSpriteId,
						S = debugSpriteInfo
					}
			};
		}
	}
}