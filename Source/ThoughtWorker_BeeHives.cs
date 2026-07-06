using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BiomesBees
{
	public class ThoughtWorker_BeeHives : ThoughtWorker
	{

		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			if (HiveUtility.disabled || ThoughtUtility.ThoughtNullified(p, def))
			{
				return ThoughtState.Inactive;
			}
			return HiveUtility.Hives.Any(hive => hive.Map == p.MapHeld);
		}
	}

}
