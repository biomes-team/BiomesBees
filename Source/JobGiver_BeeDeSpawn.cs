using Verse;
using Verse.AI;

namespace BiomesBees
{
	public class JobGiver_BeeDeSpawn : ThinkNode_JobGiver
	{

		public JobGiver_BeeDeSpawn()
		{

		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			return JobMaker.MakeJob(JobDefOf_Bees.BMT_BeeDeSpawn);
		}
	}

}
