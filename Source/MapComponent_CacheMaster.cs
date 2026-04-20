using Verse;

namespace BiomesBees
{

	// To-Do: Move to Core
	public class MapComponent_CacheMaster : MapComponent
	{
		public MapComponent_CacheMaster(Map map) : base(map)
		{
		}

		public override void MapGenerated()
		{
			HiveUtility.ResetCache();
		}

		public override void MapRemoved()
		{
			HiveUtility.ResetCache();
		}

	}
}
