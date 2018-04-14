using System;
using Game.Protocol;

namespace Game.Types
{
	public static class Constants
	{
		public const int START_FOOD_SETS = 4;
		public const int ADD_FOOD_SETS = 2;
		public const int ADD_FOOD_DELAY = 40;
		public const double FOOD_RADIUS = 2.5;
		//const double FOOD_MASS = 1.0;

		public const int START_VIRUS_SETS = 1;
		public const int ADD_VIRUS_SETS = 1;
		public const int ADD_VIRUS_DELAY = 1200;
		//const double VIRUS_RADIUS = 22.0;
		public const double VIRUS_MASS = 40.0;

		public const int START_PLAYER_SETS = 1;
		public const int START_PLAYER_OFFSET = 400;
		public const double PLAYER_RADIUS_FACTOR = 2;
		public const double PLAYER_MASS = 40.0;
		public static readonly double PLAYER_RADIUS = PLAYER_RADIUS_FACTOR * Math.Sqrt(PLAYER_MASS);

		public const double VIS_FACTOR = 4.0; // vision = radius * VF
		public const double VIS_FACTOR_FR = 2.5; // vision = radius * VFF * qSqrt(fragments.count())
		public const double VIS_SHIFT = 10.0; // dx = qCos(angle) * VS; dy = qSin(angle) * VS
		public const double DRAW_SPEED_FACTOR = 14.0;
		
		public const double COLLISION_POWER = 20.0;
		public const double MASS_EAT_FACTOR = 1.20; // mass > food.mass * MEF
		public const double DIAM_EAT_FACTOR = 2.0/3.0; // dist - eject->getR() + (eject->getR() * 2) * DIAM_EAT_FACTOR < radius

		public const double RAD_HURT_FACTOR = 2.0 / 3.0; // (radius * RHF + player.radius) > dist
		public const double MIN_BURST_MASS = 60.0; // MBM * 2 < mass
											//const int MAX_FRAGS_CNT = 10;
		public const double BURST_START_SPEED = 8.0;
		public const double BURST_ANGLE_SPECTRUM = Math.PI; // angle - BAM / 2 + I*BAM / frags_cnt
												  //const double PLAYER_VISCOSITY = 0.25;

		//const int TICKS_TIL_FUSION = 250;

		public const double MIN_SPLIT_MASS = 120.0; // MSM < mass
		public const double SPLIT_START_SPEED = 9.0;

		public const double MIN_EJECT_MASS = 40.0;
		public const double EJECT_START_SPEED = 8.0;
		public const double EJECT_RADIUS = 4.0;
		public const double EJECT_MASS = 15.0;
		//const double EJECT_VISCOSITY = 0.25;

		//const double VIRUS_VISCOSITY = 0.25;
		public const double VIRUS_SPLIT_SPEED = 8.0;
		//const double VIRUS_SPLIT_MASS = 80.0;

		public const double MIN_SHRINK_MASS = 100;
		public const double SHRINK_FACTOR = 0.01; // (-1) * (mass - MSM) * SF
		public const int SHRINK_EVERY_TICK = 50;
		public const double BURST_BONUS = 5.0; // mass += BB

		public const int SCORE_FOR_FOOD = 1;
		public const int SCORE_FOR_PLAYER = 10;
		public const int SCORE_FOR_LAST = 100;
		public const int SCORE_FOR_BURST = 2;
		
		public const int MAX_GAME_FOOD = 2000;
		public const int MAX_GAME_VIRUS = 20;
	}
}