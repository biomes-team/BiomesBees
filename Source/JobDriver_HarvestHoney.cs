using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiomesBees
{

	public class JobDriver_HarvestHoney : JobDriver
	{

		public Thing Hive => job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Hive, job, 1, -1, null, errorOnFailed);
		}

		public int Tick => 720;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, Tick, useProgressBar: true).WithEffect(EffecterDefOf.Harvest_Plant, TargetIndex.A);
			yield return Toils_General.Do(delegate
			{
				Hive?.TryGetComp<CompBeesHive>()?.Harvest(pawn, Tick);
			});
		}

	}

}
