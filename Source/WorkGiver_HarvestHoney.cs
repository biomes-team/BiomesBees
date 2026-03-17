using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiomesBees
{
	public abstract class WorkGiver_HarvestHoney : WorkGiver_Scanner
	{

		private static List<Thing> cachedHives;
		private static bool disabled = false;
		public static List<Thing> Hives
		{
			get
			{
				if (cachedHives == null)
				{
					List<Thing> list = new List<Thing>();
					foreach (Map map in Find.Maps)
					{
						foreach (Thing thing in map.spawnedThings)
						{
							if (thing.TryGetComp<CompBeesHive>() != null)
							{
								list.Add(thing);
							}
						}
					}
					cachedHives = list;
					if (cachedHives.NullOrEmpty())
					{
						disabled = true;
					}
				}
				return cachedHives;
			}
		}

		public static void ResetCache()
		{
			cachedHives = null;
			disabled = false;
		}

		public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (disabled)
			{
				return false;
			}
			if (!Hives.Contains(t))
			{
				return false;
			}
			if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, forced))
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
			if (!Hives.Contains(t))
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf_Bees.BMT_HarvestHoneyFromHive, t);
			return job;
		}
	}

}
