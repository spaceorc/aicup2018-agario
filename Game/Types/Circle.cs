using Game.Protocol;

namespace Game.Types
{
	public abstract class Circle : Point
	{
		public readonly int id;
		public readonly Config config;
		public double mass;
		public double radius;

		public Circle(int id, double x, double y, double radius, double mass, Config config) : base(x, y)
		{
			this.id = id;
			this.radius = radius;
			this.mass = mass;
			this.config = config;
		}

		public bool Intersects(Circle other)
		{
			var qdist = QDistance(other);
			var dr = radius + other.radius;
			return qdist < dr * dr;
		}

		public bool Intersects(double x, double y, double radius)
		{
			var qdist = QDistance(new Point(x, y));
			var dr = this.radius + radius;
			return qdist < dr * dr;
		}

		public virtual string IdToString() => id.ToString();

		public override string ToString()
		{
			return $"{IdToString()} [{GetType().Name}] {base.ToString()}, {nameof(radius)}: {radius}, {nameof(mass)}: {mass}";
		}

		public new Circle MemberwiseClone()
		{
			return (Circle) base.MemberwiseClone();
		}
	}
}