// HediffComp_SeverityFromVatjuice.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/12/24 12:44 PM
// Last edited by: Anthony Chenevier on 2022/12/24 12:44 PM


using EnhancedGrowthVatLearning.Data;
using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.Hediffs;

public class HediffCompProperties_SeverityFromVatjuice : HediffCompProperties
{
    public float vatjuiceDoseOffset;

    public HediffCompProperties_SeverityFromVatjuice() { compClass = typeof(HediffComp_SeverityFromVatjuice); }
}

public class HediffComp_SeverityFromVatjuice : HediffComp
{
    private HediffCompProperties_SeverityFromVatjuice Props => (HediffCompProperties_SeverityFromVatjuice)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (parent.pawn.ParentHolder is not Building_GrowthVat)
            return;

        if (parent.pawn.health.hediffSet.GetFirstHediffOfDef(ModDefOf.VatJuiceEffect) is { } juiceHediff)
            severityAdjustment += severityAdjustment * Props.vatjuiceDoseOffset * juiceHediff.Severity;
    }
}
