using System;

namespace Game.Types
{
	public class Point
	{
		public double x;
		public double y;

		public Point(Point other) : this(other.x, other.y)
		{
		}

		public Point(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public double Distance(Point other) => Math.Sqrt(QDistance(other));

		public double QDistance(Point other)
		{
			var dx = x - other.x;
			var dy = y - other.y;
			return dx * dx + dy * dy;
		}

		public override string ToString()
		{
			return $"{x},{y}";
		}
	}
}