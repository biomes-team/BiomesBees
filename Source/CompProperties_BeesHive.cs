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

		public float flowerRadius = 5;
		public int honeyTick = 5000;
		public float honeyPerFlower = 0.1f;
		public float bestHarvestAmount = 45;
		public float honeyLimit = 50;
		public ThingDef productDef;
		public string uniqueTag = "BeeHive";
		public List<PawnKindDef> angryBeesDefs;
		public FactionDef angryBeeFaction;
		public int pawnSpawnRadius = 2;

		public List<ThingDef> whitelist = new();
		public List<ThingDef> blacklist = new();
		public List<PlantPurpose> plantPurpose; // PlantPurpose.Beauty

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

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			if (bestHarvestAmount > honeyLimit)
			{
				bestHarvestAmount = honeyLimit;
			}
		}

	}

	public class CompBeesHive : ThingComp
	{

		public CompProperties_BeesHive Props => (CompProperties_BeesHive)props;

		private int nextTick = -1;
		private float beeHoney = 0;

		//public override void CompTickLong()
		//{
		//	DoTick();
		//}

		//public override void CompTickInterval(int delta)
		//{
		//	DoTick(delta);
		//}

		public override void CompTick()
		{
			DoTick(1);
		}

		public override void CompTickRare()
		{
			DoTick(720);
		}

		public override void CompTickLong()
		{
			DoTick(1500);
		}

		private bool DoTick(int delta)
		{
			nextTick -= delta;
			if (nextTick > 0)
			{
				return false;
			}
			_ = WorkGiver_HarvestHoney.Hives;
			nextTick = Props.honeyTick;
			MakeHoney(beeHoney + (GetForCell(parent.PositionHeld, Props.flowerRadius) * Props.honeyPerFlower));
			return true;
		}

		public bool CanAutoHarvest
		{
			get
			{
				return beeHoney >= Props.bestHarvestAmount;
			}
		}

		private void MakeHoney(float newHoney)
		{
			beeHoney = Mathf.Clamp(newHoney, 0, Props.honeyLimit);
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
			return IsFlower(plant.def);
		}

		public bool IsFlower(ThingDef def)
		{
			//return plant.def.plant.purpose == PlantPurpose.Beauty;
			if (Props.whitelist.Contains(def))
			{
				return true;
			}
			if (Props.blacklist.Contains(def))
			{
				return false;
			}
			//return (def.plant.purpose & Props.plantPurpose) != 0;
			return Props.plantPurpose.Contains(def.plant.purpose);
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
			beeHoney = 0;
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

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (DebugSettings.ShowDevGizmos)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: Honey +10",
					action = delegate
					{
						MakeHoney(beeHoney + 10f);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Log acceptable defs",
					action = delegate
					{
						string log = "Flowers:";
						foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
						{
							if (thingDef.plant != null && IsFlower(thingDef))
							{
								log += "\n" + thingDef.defName + " : " + thingDef.label;
							}
						}
						Log.Error(log);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Log hives",
					action = delegate
					{
						string log = "Hives:";
						foreach (Thing thing in WorkGiver_HarvestHoney.Hives)
						{
							log += "\n" + thing.def.defName + " : " + thing.def.label;
						}
						Log.Error(log);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: HarvestFail 1 bee",
					action = delegate
					{
						HarvestFail();
					}
				};
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			//if (respawningAfterLoad)
			//{
			//}
			// Always recache after spawn. Include save-load/reload.
			WorkGiver_HarvestHoney.ResetCache();
			if (respawningAfterLoad)
			{
				nextTick = Props.honeyTick;
			}
		}

		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			WorkGiver_HarvestHoney.ResetCache();
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			WorkGiver_HarvestHoney.ResetCache();
		}
		public override string CompInspectStringExtra()
		{
			return "BMT_CollectedHoney".Translate(Props.productDef.label, beeHoney.ToString());
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref nextTick, "nextTick" + "_" + Props.uniqueTag);
			Scribe_Values.Look(ref beeHoney, "beeHoney" + "_" + Props.uniqueTag);
		}

	}

}
