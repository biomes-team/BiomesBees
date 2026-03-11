using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;
using RimWorld;

namespace BiomesBees
{

	// Dev
	public class Building_BeeHive : Building
	{

		private int nextTick = -1;
		private float beeHoney = 0;

		public override void TickLong()
		{
			nextTick -= 1500;
			if (nextTick > 0)
			{
				return;
			}
			nextTick = 5000;
			MakeHoney();
		}

		// Dev
		public int WorkRadius => 5;

		public void MakeHoney()
		{
			beeHoney += GetForCell(PositionHeld, WorkRadius);
		}

		public float GetForCell(IntVec3 cell, float radius)
		{
			float value = 0f;
			foreach (Thing item in GenRadial.RadialDistinctThingsAround(cell, MapHeld, radius, useCenter: false))
			{
				if (item is Plant plant && IsFlower(plant))
				{
					value++;
				}
			}
			return value;
		}

		// Dev
		public bool IsFlower(Plant plant)
		{
			return true;
		}

	}

}
