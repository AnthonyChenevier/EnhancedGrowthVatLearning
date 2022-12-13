// ThoughtWorker_Precept_EnhancedVat.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/17 9:28 PM
// Last edited by: Anthony Chenevier on 2022/11/18 12:29 AM


using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.Thoughts;

public abstract class ThoughtWorker_Precept_EnhancedVat : ThoughtWorker_Precept
{
    protected abstract bool ActiveForMode(LearningMode mode);

    public override string PostProcessLabel(Pawn p, string label)
    {
        int multiplier = Mathf.RoundToInt(MoodMultiplier(p));
        return multiplier <= 1 ? base.PostProcessLabel(p, label) : base.PostProcessLabel(p, label) + " x" + multiplier;
    }

    protected override ThoughtState ShouldHaveThought(Pawn p) => !ModsConfig.IdeologyActive || !ModsConfig.BiotechActive ? ThoughtState.Inactive : VatChildrenInActiveModes(p) > 0;

    private int VatChildrenInActiveModes(Pawn pawn)
    {
        int count = 0;
        foreach (Pawn child in pawn.relations.Children)
            if (child.MapHeld == pawn.MapHeld &&
                child.DevelopmentalStage.Child() &&
                child.ParentHolder is Building_GrowthVat growthVat &&
                growthVat.GetComp<EnhancedGrowthVatComp>() is { Enabled: true } vatComp &&
                ActiveForMode(vatComp.Mode))
                count++;

        return count;
    }

    public override float MoodMultiplier(Pawn p) => Mathf.Min(def.stackLimit, VatChildrenInActiveModes(p));
}