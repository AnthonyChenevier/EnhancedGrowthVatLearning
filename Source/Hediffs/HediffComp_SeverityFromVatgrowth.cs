// HediffComp_SeverityFromVatgrowth.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/12/23 4:23 PM
// Last edited by: Anthony Chenevier on 2022/12/23 4:23 PM


using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.Hediffs;

public class HediffCompProperties_SeverityFromVatgrowth : HediffCompProperties
{
    public float severityPerDayOutOfVat;
    public float severityPerBiologicalYear;
    public HediffCompProperties_SeverityFromVatgrowth() { compClass = typeof(HediffComp_SeverityFromVatgrowth); }
}

public class HediffComp_SeverityFromVatgrowth : HediffComp_SeverityModifierBase
{
    private const float AgeToEject = 18f; //copy of Building_GrowthVat's because it's a private const
    private HediffCompProperties_SeverityFromVatgrowth Props => (HediffCompProperties_SeverityFromVatgrowth)props;

    public override float SeverityChangePerDay()
    {
        Pawn pawn = parent.pawn;
        if (pawn.ParentHolder is not Building_GrowthVat growthVat)
            return Props.severityPerDayOutOfVat;

        EnhancedGrowthVatComp comp = growthVat.TryGetComp<EnhancedGrowthVatComp>();
        float severityInVat = Props.severityPerBiologicalYear * AgeToEject / (GenDate.TicksPerYear / (float)comp.VatTicks);

        return severityInVat;
    }
}