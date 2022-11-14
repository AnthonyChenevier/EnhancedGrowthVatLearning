// Hediff_EnhancedVatLearning.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/03 3:02 PM
// Last edited by: Anthony Chenevier on 2022/11/03 3:02 PM


using System.Collections.Generic;
using System.Linq;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.Hediffs;

public class Hediff_EnhancedVatLearning : Hediff_VatLearning
{
    public override string Description =>
        pawn.ParentHolder is not Building_GrowthVat vat ? base.Description : $"{base.Description}\n\n{vat.GetComp<EnhancedGrowthVatComp>().ModeDisplay}";

    public Hediff_EnhancedVatLearning()
    {
        //set severity from mod settings
        comps.Add(new HediffComp_SeverityPerDay
        {
            parent = this,
            props = new HediffCompProperties_SeverityPerDay
            {
                severityPerDay = EnhancedGrowthVatMod.Settings.VatLearningHediffSeverityPerDay
            },
        });
    }


    public override void PostTick()
    {
        //pause learning hediff severity gain and learning if vat growth is paused for letter
        if (pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<EnhancedGrowthVatComp>() is { PausedForLetter: true })
            return;

        PostTickBase();

        if (Severity < (double)def.maxSeverity)
            return;

        EnhancedLearn();
    }

    //copy of hediffWithComps.PostTick().I need to inherit from
    //Hediff_VatLearning and override PostTick() to prevent bugs elsewhere
    //and I still need to call base.PostTick(). This will break if
    //HediffWithComp.PostTick() changes in any way and is bad OOP practice,
    //but I couldn't think of a better way to get past a invalid cast bug
    //in code I can't touch.
    private void PostTickBase()
    {
        if (comps == null)
            return;

        float severityAdjustment = 0.0f;
        foreach (HediffComp hediff in comps)
            hediff.CompPostTick(ref severityAdjustment);

        if (severityAdjustment == 0.0)
            return;

        Severity += severityAdjustment;
    }

    public void EnhancedLearn()
    {
        Severity = def.initialSeverity;
        if (pawn.skills == null || pawn.ParentHolder is not Building_GrowthVat vat)
            return;

        string learningMode = vat.GetComp<EnhancedGrowthVatComp>().Mode.ToString();
        Dictionary<string, float> skillsMatrix = EnhancedGrowthVatMod.Settings.SkillsMatrix(learningMode);
        SkillRecord randomSkill = pawn.skills.skills.Where(s => !s.TotallyDisabled).RandomElementByWeight(s => Mathf.Pow(skillsMatrix[s.def.defName], 2));

        randomSkill.Learn(EnhancedGrowthVatMod.Settings.XpToAward[learningMode] * LearningUtility.LearningRateFactor(pawn), true);
    }
}