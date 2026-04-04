using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiomesBees
{
	public class JobDriver_Pollinate : JobDriver
	{

		public Thing Plant => job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Plant, job, 1, -1, null, errorOnFailed);
		}

		public int Tick => 720;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, Tick, useProgressBar: true).WithEffect(EffecterDefOf.Harvest_Plant, TargetIndex.A);
			yield return Toils_General.Do(delegate
			{
				try
				{
					if (Plant is Plant plant)
					{
						plant.Growth += 0.1f;
					}
				}
				catch (Exception arg) 
				{
					Log.Error("Failed pollinate: " + Plant.def.defName + " . Reason: " + arg.Message);
				}
				if (Rand.Chance(0.8f))
				{
					CompBeesHive.StartPollinate(pawn);
				}
				else
				{
					pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf_Bees.BMT_BeeDeSpawn), JobTag.SatisfyingNeeds, false);
				}
			});
		}

	}

}
