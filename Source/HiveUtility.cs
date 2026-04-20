using System.Collections.Generic;
using Verse;

namespace BiomesBees
{
	internal static class HiveUtility
	{

		private static List<Thing> cachedHives;
		public static bool disabled = false;
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
	}
}