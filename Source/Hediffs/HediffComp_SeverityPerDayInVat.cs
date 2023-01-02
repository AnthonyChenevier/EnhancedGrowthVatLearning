// HediffComp_SeverityFromVatgrowth.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/23 4:23 PM
// Last edited by: Anthony Chenevier on 2022/12/23 4:23 PM


using System.Text;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.Hediffs;

public class HediffCompProperties_SeverityPerDayInVat : HediffCompProperties
{
    public float severityPerGrownYear;
    public float vatjuicePerDoseSeverityModifier;
    public float learningNeedSeverityModifier;
    public HediffCompProperties_SeverityPerDayInVat() { compClass = typeof(HediffComp_SeverityPerDayInVat); }
}

public class HediffComp_SeverityPerDayInVat : HediffComp_SeverityModifierBase
{
    private HediffCompProperties_SeverityPerDayInVat Props => (HediffCompProperties_SeverityPerDayInVat)props;

    private CompOverclockedGrowthVat VatComp => (parent.pawn.ParentHolder as Building_GrowthVat)?.TryGetComp<CompOverclockedGrowthVat>();

    private float BaseSeverityChangePerDay =>
        VatComp is not null ? (float)((double)Props.severityPerGrownYear / GenDate.TicksPerYear * (VatComp.VatTicks * GenDate.TicksPerDay)) : 0f;

    private float VatJuiceSeverityModifer =>
        parent.pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatJuiceEffect) is { } juiceHediff ? Props.vatjuicePerDoseSeverityModifier * juiceHediff.Severity : 0f;

    //TODO - Use learning need
    private float DailyGrowthPointsSeverityModifer => 0f;
    //VatComp is not null ? parent.pawn.ageTracker.GrowthPointsPerDay / VatComp.DailyGrowthPointFactor * Props.learningNeedSeverityModifier : 0f;

    public override float SeverityChangePerDay()
    {
        float severityPerDay = BaseSeverityChangePerDay;
        //sum and apply modifiers
        severityPerDay += severityPerDay * Mathf.Min(1f, VatJuiceSeverityModifer + DailyGrowthPointsSeverityModifer);
        return severityPerDay;
    }

    public override string CompDebugString()
    {
        StringBuilder stringBuilder = new();
        if (!Pawn.Dead)
        {
            stringBuilder.AppendLine("base severity/day: " + BaseSeverityChangePerDay.ToString("F8"));
            stringBuilder.AppendLine("vatjuice usage modifier: " + VatJuiceSeverityModifer.ToStringPercent("F3"));
            stringBuilder.AppendLine("learning need modifier: " + DailyGrowthPointsSeverityModifer.ToStringPercent("F3"));
            stringBuilder.AppendLine("final severity/day: " + SeverityChangePerDay().ToString("F8"));
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }
}