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
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class HediffCompProperties_SeverityFromVatExposure : HediffCompProperties
{
    public float severityPerGrownYear;
    public float vatjuicePerDoseSeverityModifier;
    public float learningNeedSeverityModifier;
    public HediffCompProperties_SeverityFromVatExposure() { compClass = typeof(HediffComp_SeverityFromVatExposure); }
}

public class HediffComp_SeverityFromVatExposure : HediffComp_SeverityModifierBase
{
    private HediffCompProperties_SeverityFromVatExposure Props => (HediffCompProperties_SeverityFromVatExposure)props;

    private CompOverclockedGrowthVat VatComp => (parent.pawn.ParentHolder as Building_GrowthVat)?.GetComp<CompOverclockedGrowthVat>();

    //base severity change is dependent on growth speed stat
    private float BaseSeverityChangePerDay => (float)((double)Props.severityPerGrownYear / GenDate.TicksPerYear * (VatComp.StatDerivedGrowthSpeed * GenDate.TicksPerDay));

    private float VatJuiceSeverityModifer =>
        parent.pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatJuiceEffect) is { } juiceHediff ? Props.vatjuicePerDoseSeverityModifier * juiceHediff.Severity : 0f;

    private float LearningNeedSeverityModifer => Pawn.needs.learning is { } learning ? Props.learningNeedSeverityModifier * learning.CurLevel : 0f;

    public override float SeverityChangePerDay()
    {
        float severityPerDay = BaseSeverityChangePerDay;
        //sum and apply modifiers
        severityPerDay += severityPerDay * Mathf.Min(1f, VatJuiceSeverityModifer + LearningNeedSeverityModifer);
        return severityPerDay;
    }

    public override string CompDebugString()
    {
        StringBuilder stringBuilder = new();
        if (!Pawn.Dead)
        {
            stringBuilder.AppendLine("base severity/day: " + BaseSeverityChangePerDay.ToString("F8"));
            stringBuilder.AppendLine("vatjuice usage modifier: " + VatJuiceSeverityModifer.ToStringPercent("F3"));
            stringBuilder.AppendLine("learning need modifier: " + LearningNeedSeverityModifer.ToStringPercent("F3"));
            stringBuilder.AppendLine("final severity/day: " + SeverityChangePerDay().ToString("F8"));
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }
}