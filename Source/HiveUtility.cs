using BiomesCore;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BiomesBees
{
	internal static class HiveUtility
	{

		private static IEnumerable<Thing> cachedHives;
		private static List<CompBeesHive> cachedCompHives;
		public static bool disabled = false;
		public static IEnumerable<Thing> Hives
		{
			get
			{
				if (cachedHives == null)
				{
					GetHives();
				}
				return cachedHives;
			}
		}
		public static List<CompBeesHive> HivesComps
		{
			get
			{
				if (cachedCompHives == null)
				{
					GetHives();
				}
				return cachedCompHives;
			}
		}

		private static void GetHives()
		{
			List<CompBeesHive> list = new();
			foreach (Map map in Find.Maps)
			{
				foreach (Thing thing in map.spawnedThings)
				{
					CompBeesHive compBeesHive = thing.TryGetComp<CompBeesHive>();
					if (compBeesHive != null)
					{
						list.Add(compBeesHive);
					}
				}
			}
			cachedCompHives = list;
			cachedHives = list.Select(hive => hive.parent);
			if (cachedHives.Any())
			{
				HarmonyPatch_GrowRate();
			}
			else
			{
				disabled = true;
			}
		}

		public static void ResetCache()
		{
			cachedHives = null;
			disabled = false;
		}

		private static Harmony cachedHarmony;
		public static Harmony Harmony
		{
			get
			{
				if (cachedHarmony == null)
				{
					cachedHarmony = new Harmony("wvc.sergkart.races.biotech");
				}
				return cachedHarmony;
			}
		}

		private static bool plantsGrowRate_Patched = false;
		public static void HarmonyPatch_GrowRate()
		{
			if (plantsGrowRate_Patched)
			{
				return;
			}
			try
			{
				Harmony.Patch(AccessTools.DeclaredPropertyGetter(typeof(Plant), "GrowthRate"), postfix: new HarmonyMethod(typeof(BiomesBees.HiveUtility).GetMethod(nameof(GrowRatePatch))));
			}
			catch (Exception arg)
			{
				Log.Error("Non-critical error. Failed apply grow rate patch. Reason: " + arg.Message);
			}
			plantsGrowRate_Patched = true;
		}

		public static void GrowRatePatch(Plant __instance, ref float __result)
		{
			//Log.Error("0");
			if (BiomesBees.HiveUtility.disabled)
			{
				return;
			}
			//Log.Error("1");
			float factor = 1f;
			foreach (CompBeesHive compBeesHive in BiomesBees.HiveUtility.HivesComps)
			{
				if (compBeesHive.Plants.Contains(__instance))
				{
					//Log.Error("2");
					factor = compBeesHive.Props.pollinationFactor;
					break;
				}
			}
			__result *= factor;
		}

	}
}