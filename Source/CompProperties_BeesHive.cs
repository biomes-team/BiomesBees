using BiomesCore.LordJobs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;
using Verse.Sound;

namespace BiomesBees
{

	public class CompProperties_BeesHive : CompProperties
	{
		public bool flowerlessProduction = false;
		public StatDef failChanceStatDef;
		public float honeyProductionWithoutFlowers = 0.5f;
		public bool wildHive = false;

		[NoTranslate]
		public string sowFlowersIconPath = "UI/Designators/CutPlants";

		public float powerlessFactor = 0.5f;

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

		public float BeesHiveFactor
		{
			get
			{
				SimpleCurve curvePoints = new()
				{
					new CurvePoint(1f, 1.1f),
					new CurvePoint(5f, 1.0f),
					new CurvePoint(10f, 0.8f),
					new CurvePoint(15f, 0.4f),
					new CurvePoint(20f, 0.35f)
				};
				return curvePoints.Evaluate(HiveUtility.Hives.Where(hive => hive.Map == this.parent.Map).Count());
			}
		}

		private bool PowerOn
		{
			get
			{
				CompPowerTrader compPowerTrader = parent.GetComp<CompPowerTrader>();
				if (compPowerTrader == null)
				{
					return true;
				}
				return compPowerTrader.PowerOn;
			}
		}

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

		float flowersCount = 0;
		private bool DoTick(int delta)
		{
			nextTick -= delta;
			if (nextTick > 0)
			{
				return false;
			}
			_ = HiveUtility.Hives;
			nextTick = Props.honeyTick;
			float newHoney = (!Props.flowerlessProduction ? (flowersCount = GetForCell(parent.PositionHeld, Props.flowerRadius)) * Props.honeyPerFlower : Props.honeyProductionWithoutFlowers);
			if (!PowerOn)
			{
				newHoney *= Props.powerlessFactor;
			}
			newHoney += BeesHiveFactor;
			MakeHoney(beeHoney + newHoney);
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
			if (Props.failChanceStatDef != null)
			{
				failChance *= harvester.GetStatValue(Props.failChanceStatDef);
			}
			if (!CanAutoHarvest)
			{
				failChance *= (Props.bestHarvestAmount / beeHoney);
			}
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

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			if (parent.Faction != Faction.OfPlayer)
			{
				yield break;
			}
			if (beeHoney <= 0 || selPawn.Faction != Faction.OfPlayer || !selPawn.RaceProps.Humanlike)
			{
				yield break;
			}
			if (!selPawn.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Deadly))
			{
				yield break;
			}
			if (!selPawn.workSettings.WorkIsActive(WorkTypeDefOf.Handling))
			{
				yield break;
			}
			yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("BMT_Harvest".Translate(), delegate
			{
				Job job = JobMaker.MakeJob(JobDefOf_Bees.BMT_HarvestHoneyFromHive, parent);
				selPawn.jobs.TryTakeOrderedJob(job, requestQueueing: false);
			}), selPawn, parent);
		}

		//private static CachedTexture cachedPlantZoneTexture;
		//private CachedTexture PlantZone
		//{
		//	get
		//	{
		//		if (cachedPlantZoneTexture == null)
		//		{
		//			cachedPlantZoneTexture = new(Props.sowFlowersIconPath);
		//		}
		//		return cachedPlantZoneTexture;
		//	}
		//}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			//yield return new Command_Action
			//{
			//	defaultLabel = "GrowingZone".Translate(),
			//	defaultDesc = "BMT_CreateGrowingZone".Translate(),
			//	icon = PlantZone.Texture,
			//	action = CreateFowersZone
			//};
			if (DesignatorUtility.FindAllowedDesignator<Designator_ZoneAdd_Growing>() != null)
			{
				Command_Action command_Action = new Command_Action();
				command_Action.action = MakeMatchingGrowZone;
				command_Action.hotKey = KeyBindingDefOf.Misc2;
				command_Action.defaultDesc = "BMT_CreateGrowingZone".Translate();
				command_Action.icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing");
				command_Action.defaultLabel = "GrowingZone".Translate();
				yield return command_Action;
			}
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
						foreach (Thing thing in HiveUtility.Hives)
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
				yield return new Command_Action
				{
					defaultLabel = "DEV: SpawnPollinator4000",
					action = delegate
					{
						SpawnPollinator4000();
					}
				};
			}
		}

		public IEnumerable<IntVec3> GrowableCells => GenRadial.RadialCellsAround(parent.Position, parent.def.specialDisplayRadius, useCenter: true);
		private void MakeMatchingGrowZone()
		{
			Designator_ZoneAdd_Growing designator = DesignatorUtility.FindAllowedDesignator<Designator_ZoneAdd_Growing>();
			designator.DesignateMultiCell(GrowableCells.Where((IntVec3 tempCell) => designator.CanDesignateCell(tempCell).Accepted));
		}
		//private IEnumerable<IntVec3> RadialCells => GenRadial.RadialCellsAround(parent.Position, Props.flowerRadius, useCenter: true);
		//private void CreateFowersZone()
		//{
		//	List<Thing> selectedTrees = Find.Selector.SelectedObjects.OfType<Thing>().Where((thing) => thing.TryGetComp<CompBeesHive>() != null).ToList();
		//	if (parent.Map.zoneManager.ZoneAt(parent.Position) != null)
		//	{
		//		Zone zone = parent.Map.zoneManager.ZoneAt(parent.Position);
		//		Zone_Growing existing = zone as Zone_Growing;
		//		if (existing == null)
		//		{
		//			return;
		//		}
		//		parent.Map.floodFiller.FloodFill(parent.Position, (IntVec3 c) => selectedTrees.Any((Thing tree) => tree.TryGetComp<CompBeesHive>().RadialCells.Contains(c)) && (parent.Map.zoneManager.ZoneAt(c) == null || parent.Map.zoneManager.ZoneAt(c) == existing) && (bool)Designator_ZoneAdd.IsZoneableCell(c, parent.Map), delegate (IntVec3 c)
		//		{
		//			if (!existing.ContainsCell(c))
		//			{
		//				existing.AddCell(c);
		//			}
		//		});
		//		return;
		//	}
		//	Zone_Growing stockpile = new Zone_Growing(parent.Map.zoneManager);
		//	stockpile.SetPlantDefToGrow(DefDatabase<ThingDef>.AllDefsListForReading.Where((t) => t.plant != null && IsFlower(t)).ToList().RandomElement());
		//	parent.Map.zoneManager.RegisterZone(stockpile);
		//	Zone_Growing existingStockpile = null;
		//	parent.Map.floodFiller.FloodFill(parent.Position, delegate (IntVec3 c)
		//	{
		//		if (parent.Map.zoneManager.ZoneAt(c) is Zone_Growing zone_Stockpile)
		//		{
		//			existingStockpile = zone_Stockpile;
		//		}
		//		return selectedTrees.Any((Thing tree) => tree.TryGetComp<CompBeesHive>().RadialCells.Contains(c)) && parent.Map.zoneManager.ZoneAt(c) == null && (bool)Designator_ZoneAdd.IsZoneableCell(c, parent.Map);
		//	}, delegate (IntVec3 c)
		//	{
		//		stockpile.AddCell(c);
		//	});
		//	if (existingStockpile == null)
		//	{
		//		return;
		//	}
		//	List<IntVec3> list = stockpile.Cells.ToList();
		//	stockpile.Delete();
		//	foreach (IntVec3 item in list)
		//	{
		//		existingStockpile.AddCell(item);
		//	}
		//}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			HiveUtility.ResetCache();
			if (respawningAfterLoad)
			{
				nextTick = Props.honeyTick;
			}
		}

		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			HiveUtility.ResetCache();
		}

		public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
		{
			if (Props.wildHive)
			{
				PawnKindDef named = Props.angryBeesDefs.RandomElement();
				if (named != null)
				{
					Faction faction = ((Props.angryBeeFaction != null && FactionUtility.DefaultFactionFrom(Props.angryBeeFaction) != null) ? FactionUtility.DefaultFactionFrom(Props.angryBeeFaction) : null);
					PawnGenerationRequest request = new PawnGenerationRequest(named, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: true, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: false, allowPregnant: true, allowFood: true, allowAddictions: false);
					Pawn pawnToCreate = PawnGenerator.GeneratePawn(request);
					GenSpawn.Spawn(pawnToCreate, CellFinder.RandomClosewalkCellNear(parent.Position, prevMap, Props.pawnSpawnRadius), prevMap);
					if (Props.spawnSound != null)
					{
						Props.spawnSound.PlayOneShot(parent);
					}
					pawnToCreate.health.AddHediff(HediffDefOf.Scaria);
				}
			}
		}

		private void SpawnPollinator4000()
		{
			PawnKindDef named = Props.angryBeesDefs.RandomElement();
			if (named != null)
			{
				PawnGenerationRequest request = new PawnGenerationRequest(named, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: true, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: false, allowPregnant: true, allowFood: true, allowAddictions: false);
				Pawn pawnToCreate = PawnGenerator.GeneratePawn(request);
				GenSpawn.Spawn(pawnToCreate, CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, Props.pawnSpawnRadius), parent.Map);
				if (pawnToCreate.Faction != null)
				{
					pawnToCreate.SetFaction(null);
				}
				StartPollinate(pawnToCreate);
			}
		}

		public static void StartPollinate(Pawn bee)
		{
			Thing plant = GetClosestPlant(bee, false, null);
			try
			{
				bee.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf_Bees.BMT_BeePollinate, plant), JobTag.SatisfyingNeeds, false);
			}
			catch (Exception arg)
			{
				Log.Error("Failed pollinate job. Reason: " + arg.Message);
			}
		}

		private static Plant GetClosestPlant(Pawn bee, bool forced, List<ThingDef> allowedThingDefs)
		{
			Danger danger = (forced ? Danger.Deadly : Danger.Some);
			return (Plant)GenClosest.ClosestThingReachable(bee.Position, bee.Map, ThingRequest.ForGroup(ThingRequestGroup.NonStumpPlant), PathEndMode.InteractionCell, TraverseParms.For(bee, danger), 9999f, delegate (Thing t)
			{
				return !t.IsForbidden(bee) && bee.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced) && (allowedThingDefs == null || allowedThingDefs.Contains(t.def));
			});
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			HiveUtility.ResetCache();
		}
		public override string CompInspectStringExtra()
		{
			if (parent.Faction != Faction.OfPlayer)
			{
				return null;
			}
			if (!Props.flowerlessProduction || flowersCount > 0)
			{
				return "BMT_CollectedHoneyWithFlowers".Translate(Props.productDef.label, flowersCount, beeHoney.ToString());
			}
			return "BMT_CollectedHoney".Translate(Props.productDef.label, beeHoney.ToString());
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref nextTick, "nextTick" + "_" + Props.uniqueTag);
			Scribe_Values.Look(ref beeHoney, "beeHoney" + "_" + Props.uniqueTag);
			Scribe_Values.Look(ref flowersCount, "flowersCount");
		}

	}

}
