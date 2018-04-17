using System.Diagnostics;
using Game.Protocol;

namespace Game
{
	public class TimeManager
	{
		private readonly Stopwatch stopwatch = new Stopwatch();
		public readonly long totalTime;
		public readonly long totalTicks;
		public long timeElapsed;
		public long ticksPassed;
		public long millisPerTick;

		public TimeManager(Config config)
		{
			totalTime = config.GAME_TICKS * Settings.MILLIS_PER_TICK;
			totalTicks = config.GAME_TICKS;
			millisPerTick = Settings.MILLIS_PER_TICK - 2;
		}

		public void TickStarted()
		{
			stopwatch.Restart();
		}

		public void TickFinished()
		{
			stopwatch.Stop();
			timeElapsed += stopwatch.ElapsedMilliseconds + 2;
			ticksPassed++;
			millisPerTick = totalTicks == ticksPassed ? 0 : (totalTime - timeElapsed) / (totalTicks - ticksPassed);
			if (millisPerTick > Settings.MAX_MILLIS_PER_TICK)
				millisPerTick = Settings.MAX_MILLIS_PER_TICK;
		}

		public bool IsExpired => stopwatch.ElapsedMilliseconds >= millisPerTick;
		public bool BeStupid => millisPerTick <= Settings.BE_STUPID_MILLIS_PER_TICK;
		public bool BeSmart => millisPerTick >= Settings.BE_SMART_MILLIS_PER_TICK;
		public bool IsExpiredGlobal => timeElapsed > totalTime;
		public long Elapsed => stopwatch.ElapsedMilliseconds;

		public override string ToString()
		{
			return $"{nameof(totalTime)}: {totalTime}, {nameof(totalTicks)}: {totalTicks}, {nameof(timeElapsed)}: {timeElapsed}, {nameof(ticksPassed)}: {ticksPassed}, {nameof(millisPerTick)}: {millisPerTick}";
		}
	}
}