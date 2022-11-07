// HediffComp_EnhancedVatGrowing.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:27 AM
// Last edited by: Anthony Chenevier on 2022/11/04 12:27 AM


using EnhancedGrowthVat.ThingComps;
using RimWorld;
using Verse;

namespace EnhancedGrowthVat.Hediffs;

public class HediffComp_EnhancedVatGrowing : HediffComp
{
    public override bool CompShouldRemove => Pawn.Spawned || !(Pawn.ParentHolder is Building_GrowthVat);

    public override string CompTipStringExtra =>
        $"{"AgingSpeed".Translate()}: x{(Pawn.ParentHolder is Building_GrowthVat growthVat ? growthVat.GetComp<EnhancedGrowthVatComp>().GrowthFactor : 1)}";
}