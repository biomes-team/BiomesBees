using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiomesBees
{
	public class WorkGiver_HarvestHoney : WorkGiver_Scanner
	{
		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return HiveUtility.Hives.Where((thing) => thing.Map == pawn.Map);
		}

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (HiveUtility.disabled)
			{
				return false;
			}
			if (!HiveUtility.Hives.Contains(t))
			{
				return false;
			}
			if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			return t.TryGetComp<CompBeesHive>().CanAutoHarvest;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!HiveUtility.Hives.Contains(t))
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf_Bees.BMT_HarvestHoneyFromHive, t);
			return job;
		}
	}

}
