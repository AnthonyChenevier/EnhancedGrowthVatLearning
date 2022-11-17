// ThoughtWorker_Precept_TeenInGrowthVat.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/17 7:53 PM
// Last edited by: Anthony Chenevier on 2022/11/17 7:53 PM


using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.Thoughts;

public class ThoughtWorker_Precept_TeenInGrowthVat : ThoughtWorker_Precept
{
    public override string PostProcessLabel(Pawn p, string label)
    {
        int multiplier = Mathf.RoundToInt(MoodMultiplier(p));
        return multiplier <= 1 ? base.PostProcessLabel(p, label) : base.PostProcessLabel(p, label) + " x" + multiplier;
    }

    protected override ThoughtState ShouldHaveThought(Pawn p) => !ModsConfig.IdeologyActive || !ModsConfig.BiotechActive || TeensInGrowthVatCount(p) > 0;

    private int TeensInGrowthVatCount(Pawn pawn) =>
        pawn.relations.Children.Count(teen => teen.MapHeld == pawn.MapHeld && teen.DevelopmentalStage.Adult() && teen.ParentHolder is Building_GrowthVat);

    public override float MoodMultiplier(Pawn p) => Mathf.Min(def.stackLimit, TeensInGrowthVatCount(p));
}