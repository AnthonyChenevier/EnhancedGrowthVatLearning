// HediffComp_SeverityFromVatgrowth.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/23 4:23 PM
// Last edited by: Anthony Chenevier on 2022/12/23 4:23 PM


using System.Text;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.VatExtensions;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class HediffCompProperties_SeverityFromVatExposure : HediffCompProperties
{
    public float severityPerGrownYear;
    public float vatjuicePerDoseSeverityModifier;
    public float learningNeedSeverityModifier;
    public HediffCompProperties_SeverityFromVatExposure() { compClass = typeof(HediffComp_SeverityFromVatExposure); }
}

public class HediffComp_SeverityFromVatExposure : HediffComp
{
    private const int CheckInterval = 200;
    private HediffCompProperties_SeverityFromVatExposure Props => (HediffCompProperties_SeverityFromVatExposure)props;

    private CompOverclockedGrowthVat VatComp => (parent.pawn.ParentHolder as Building_GrowthVat)?.GetComp<CompOverclockedGrowthVat>();

    private float VatJuiceSeverityModifer =>
        parent.pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatJuiceEffect) is { } juiceHediff ? Props.vatjuicePerDoseSeverityModifier * juiceHediff.Severity : 0f;

    private float LearningNeedSeverityModifer => Pawn.needs.learning is { } learning ? Props.learningNeedSeverityModifier * learning.CurLevel : 0f;

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!Pawn.IsHashIntervalTick(CheckInterval) || VatComp == null)
            return;

        severityAdjustment += SeverityChangePerTick() * 300f; //GenDate.TicksPerDay / CheckInterval;
    }

    private float SeverityChangePerTick()
    { //convert yearly growth severity to per-tick
        float severityChange = (float)((double)Props.severityPerGrownYear / GenDate.TicksPerYear) * VatComp?.StatDerivedGrowthSpeed ?? 0f;
        //sum and apply modifiers
        severityChange += severityChange * (VatJuiceSeverityModifer + LearningNeedSeverityModifer);
        return severityChange;
    }

    public override string CompDebugString()
    {
        if (Pawn.Dead)
            return "";

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("vatjuice usage modifier: " + VatJuiceSeverityModifer.ToStringPercent("F3"));
        stringBuilder.AppendLine("learning need modifier: " + LearningNeedSeverityModifer.ToStringPercent("F3"));
        stringBuilder.AppendLine("final severity/tick: " + SeverityChangePerTick().ToString("F8"));

        return stringBuilder.ToString().TrimEndNewlines();
    }
}