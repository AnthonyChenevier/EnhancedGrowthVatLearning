// HediffComp_EnhancedVatGrowing.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:27 AM
// Last edited by: Anthony Chenevier on 2022/11/04 12:27 AM


using GrowthVatsOverclocked.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.Hediffs;

public class HediffComp_EnhancedVatGrowing : HediffComp
{
    public override bool CompShouldRemove => Pawn.Spawned || Pawn.ParentHolder is not Building_GrowthVat;

    public override string CompTipStringExtra
    {
        get
        {
            int vatAgingFactor = 5318008; //shouldn't be seen if everything is working
            if (Pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<CompOverclockedGrowthVat>() is { Enabled: true } comp)
                vatAgingFactor = comp.PausedForLetter ? 0 : comp.ModeAgingFactor; //show base value or 0 if paused

            //explain final growth speed (if growStat matters)
            float growStat = Pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed, true, GenDate.TicksPerDay);
            int finalSpeed = Mathf.FloorToInt(vatAgingFactor * growStat);
            string growthStatMod = growStat == 1f ? "" : $" {"AgingSpeedWithOccupantStatModifier".Translate(finalSpeed, growStat.ToStringPercent())}";

            return $"{"AgingSpeed".Translate()}: x{vatAgingFactor}{growthStatMod}";
        }
    }
}