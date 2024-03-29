﻿// StatWorker_VatTime.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/13 1:33 PM
// Last edited by: Anthony Chenevier on 2022/11/13 1:33 PM


using System.Text;
using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.Stats;

public class StatWorker_VatTime : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        return req.HasThing && req.Thing is Pawn pawn && pawn.RaceProps.Humanlike && EnhancedGrowthVatMod.GetTrackerFor(pawn) is { VatTicksBiological: > 0 };
    }

    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess) { val = EnhancedGrowthVatMod.GetTrackerFor((Pawn)req.Thing).VatTicksBiological; }

    public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute) { return ((int)val).ToStringTicksToPeriod(); }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        VatGrowthTracker tracker = EnhancedGrowthVatMod.GetTrackerFor((Pawn)req.Thing);

        StringBuilder sb = new();
        foreach (LearningMode mode in tracker.ModeTicks.Keys /*.Where(k => tracker.ModeTicks[k] > 0)*/)
        {
            sb.Append("\n");
            sb.Append("StatsReport_VatLearningTime".Translate(mode.Label(), tracker.LearningModePercent(mode).ToStringPercent()));
        }

        sb.Append("\n");
        sb.Append("StatsReport_VatNoLearningTime".Translate(tracker.NormalGrowthPercent.ToStringPercent()));
        sb.Append("\n\n");
        sb.Append("StatsReport_TotalVatTime".Translate(ValueToString(finalVal, true)));

        return sb.ToString();
    }
}
