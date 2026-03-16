using BiomesCore.LordJobs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;
using Verse.Sound;

namespace BiomesBees
{

	public class CompProperties_BeesHive : CompProperties
	{

		public int flowerRadius = 5;
		public int honeyTick = 5000;
		public float honeyLimit = 50;
		public ThingDef productDef;
		public string uniqueTag = "BeeHive";
		public List<PawnKindDef> angryBeesDefs;
		public FactionDef angryBeeFaction;
		public int pawnSpawnRadius = 2;

		public int safeHarvestLevel = 16;
		public float xpPerTick = 1f;
		public float basicFailChance = 0.1f;
		public float defendRadius = 7f;
		public float wanderRadius = 6f;

		public SoundDef spawnSound;

		public CompProperties_BeesHive()
		{
			compClass = typeof(CompBeesHive);
		}
	}

	public class CompBeesHive : ThingComp
	{

		public CompProperties_BeesHive Props => (CompProperties_BeesHive)props;

		private int nextTick = -1;
		private float beeHoney = 0;

		public override void CompTickLong()
		{
			nextTick -= 1500;
			if (nextTick > 0)
			{
				return;
			}
			nextTick = Props.honeyTick;
			MakeHoney();
		}

		private void MakeHoney()
		{
			beeHoney = Mathf.Clamp(beeHoney + GetForCell(parent.PositionHeld, Props.flowerRadius), 0, Props.honeyLimit);
		}

		private float GetForCell(IntVec3 cell, float radius)
		{
			float value = 0f;
			foreach (Thing item in GenRadial.RadialDistinctThingsAround(cell, parent.MapHeld, radius, useCenter: false))
			{
				if (item is Plant plant && IsFlower(plant))
				{
					value++;
				}
			}
			return value;
		}

		public bool IsFlower(Plant plant)
		{
			return plant.def.plant.purpose == PlantPurpose.Beauty;
		}

		public void Harvest(Pawn harvester, int tick)
		{
			float? skillFactor = harvester.skills?.GetSkill(SkillDefOf.Animals)?.Level;
			float failChance = Props.basicFailChance / (skillFactor.HasValue && skillFactor.Value > 0 ? skillFactor.Value : 1f);
			if ((skillFactor == null || skillFactor.Value < Props.safeHarvestLevel) && Rand.Chance(failChance))
			{
				HarvestFail();
			}
			else
			{
				HarvestHoney(harvester);
			}
			harvester.skills?.Learn(SkillDefOf.Animals, Props.xpPerTick * tick);
		}

		private void HarvestHoney(Pawn harvester)
		{
			Thing thing = ThingMaker.MakeThing(Props.productDef);
			thing.stackCount = (int)beeHoney;
			GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near, out var lastResultingThing, null, default);
			if (harvester.Faction != Faction.OfPlayer)
			{
				lastResultingThing.SetForbidden(value: true);
			}
			//if (Props.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
			//{
			//	Messages.Message(Props.spawnMessage.Translate(productDef.LabelCap), thing, MessageTypeDefOf.PositiveEvent);
			//}
		}

		private void HarvestFail()
		{
			int tries = new IntRange(1, 3).RandomInRange;
			for (int i = 0; i < tries; i++)
			{
				SpawnAngryBee();
			}
		}

		private void SpawnAngryBee()
		{
			PawnKindDef named = Props.angryBeesDefs.RandomElement();
			if (named != null)
			{
				Faction faction = ((Props.angryBeeFaction != null && FactionUtility.DefaultFactionFrom(Props.angryBeeFaction) != null) ? FactionUtility.DefaultFactionFrom(Props.angryBeeFaction) : null);
				PawnGenerationRequest request = new PawnGenerationRequest(named, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: true, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: false, allowPregnant: true, allowFood: true, allowAddictions: false);
				Pawn pawnToCreate = PawnGenerator.GeneratePawn(request);
				GenSpawn.Spawn(pawnToCreate, CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, Props.pawnSpawnRadius), parent.Map);
				if (parent.Map != null)
				{
					Lord lord = null;
					if (parent.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != pawnToCreate))
					{
						lord = ((Pawn)GenClosest.ClosestThing_Global(parent.Position, parent.Map.mapPawns.SpawnedPawnsInFaction(faction), 30f, (Thing p) => p != pawnToCreate && ((Pawn)p).GetLord() != null)).GetLord();
					}
					if (lord == null)
					{
						lord = LordMaker.MakeNewLord(faction, new LordJob_DefendHive(parent.Position, Props.wanderRadius, defendRadius: Props.defendRadius), parent.Map);
					}
					lord.AddPawn(pawnToCreate);
				}
				if (Props.spawnSound != null)
				{
					Props.spawnSound.PlayOneShot(parent);
				}
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref nextTick, "nextTick" + "_" + Props.uniqueTag);
			Scribe_Values.Look(ref beeHoney, "beeHoney" + "_" + Props.uniqueTag);
		}

	}

}
