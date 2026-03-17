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
			WorkGiver_HarvestHoney.ResetCache();
		}

		public override void MapRemoved()
		{
			WorkGiver_HarvestHoney.ResetCache();
		}

	}
}
