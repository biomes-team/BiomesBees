using BiomesBees;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiomesBees;

public class WorkGiver_TakeHoneyWineOutOfFermentingBarrel : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(JobDefOf_Bees.BMT_MeadBarrel);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!(t is Building_HoneyFermentingBarrel { Fermented: true }))
        {
            return false;
        }

        if (t.IsBurning())
        {
            return false;
        }

        if (!t.IsForbidden(pawn))
        {
            LocalTargetInfo target = t;
            if (pawn.CanReserve(target, 1, -1, null, forced))
            {
                return true;
            }
        }

        return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return new Job(JobDefOf_Bees.BMT_TakeMeadOutOfBarrelJob, t);
    }
}
