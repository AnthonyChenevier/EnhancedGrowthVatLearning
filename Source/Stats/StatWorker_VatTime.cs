// StatWorker_VatTime.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/13 1:33 PM
// Last edited by: Anthony Chenevier on 2022/11/13 1:33 PM


using System.Text;
using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.Stats;

public class StatWorker_VatTime : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        return req.HasThing && req.Thing is Pawn pawn && pawn.RaceProps.Humanlike && pawn.GetComp<VatGrowthTrackerComp>() is { BiologicalTicksTotal: > 0 };
    }

    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        Pawn pawn = (Pawn)req.Thing;
        VatGrowthTrackerComp tracker = pawn.GetComp<VatGrowthTrackerComp>();
        val = Mathf.Max(tracker.TicksToAdulthoodPercent(pawn.ageTracker.AdultMinAgeTicks), 1f);
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        Pawn pawn = (Pawn)req.Thing;
        VatGrowthTrackerComp tracker = pawn.GetComp<VatGrowthTrackerComp>();

        StringBuilder sb = new();

        for (int i = 0; i < pawn.RaceProps.lifeStageAges.Count - 1; i++)
        {
            LifeStageAge lifeStageAge = pawn.RaceProps.lifeStageAges[i];
            LifeStageAge nextLifeStageAge = pawn.RaceProps.lifeStageAges[i + 1];
            if (lifeStageAge.def.developmentalStage != DevelopmentalStage.Child)
                continue;

            long minAge = (long)(lifeStageAge.minAge * 3600000L);
            long maxAge = (long)(nextLifeStageAge.minAge * 3600000L - 1L);
            float ticksForLifeStagePercent = tracker.TicksForLifeStagePercent(i, minAge, maxAge);
            sb.Append("StatsReport_LifeStageGrowTime".Translate(lifeStageAge.def.LabelCap, Mathf.Max(ticksForLifeStagePercent, 1f).ToStringPercent()));
            sb.Append("\n");
        }

        sb.Append("\n");
        sb.Append("StatsReport_VatNoLearningTime".Translate(tracker.NormalGrowthPercent.ToStringPercent()));

        foreach (LearningMode mode in tracker.ModeVatTicks.Keys)
        {
            sb.Append("\n");
            sb.Append("StatsReport_VatLearningTime".Translate($"LearningModes_{mode}".Translate(), tracker.LearningModePercent(mode).ToStringPercent()));
        }

        return sb.ToString();
    }
}
