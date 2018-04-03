namespace Game.Protocol
{
	public class TurnInput
	{
		public MineData[] Mine = new MineData[0];
		public ObjectData[] Objects = new ObjectData[0];

		public class MineData
		{
			public string Id;
			public double X;
			public double Y;
			public double SX;
			public double SY;
			public double R;
			public double M;
			public int TTF;
		}

		public class ObjectData
		{
			public string Id;
			public string pId;
			public string T;
			public double X;
			public double Y;
			public double R;
			public double M;
		}
	}
}