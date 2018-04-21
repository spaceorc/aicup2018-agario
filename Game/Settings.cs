namespace Game
{
	// pack: 0
	public static class Settings
	{
		public const int CHECKPOINT_GAIN_TICKS_LIMIT = 100;
		public const int FOOD_FORGET_TICKS = 20; // todo increase? this constant
		public const int ENEMY_FORGET_TICKS = 20; // todo increase? this constant
		public const int MILLIS_PER_TICK = 24;
		public const int MAX_MILLIS_PER_TICK = 4000;
		public const int BE_STUPID_MILLIS_PER_TICK = 10;
		public const int BE_SMART_MILLIS_PER_TICK = 40;
		public const string DefaultStrategy = "sim_7_split_fixed_noFoodStuck";
	}
}