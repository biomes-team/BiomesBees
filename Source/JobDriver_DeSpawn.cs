using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiomesBees
{
	public class JobDriver_DeSpawn : JobDriver
	{

		//public Thing Plant => job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			//pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetA.Cell);
			return true;
		}

		//public int Tick => 720;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			//this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			//yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(320);
			yield return Toils_General.Do(delegate
			{
				pawn.Destroy();
			});
		}

	}

}
