// HediffComp_VatLearningModeOverride.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2023/01/02 2:03 PM
// Last edited by: Anthony Chenevier on 2023/01/02 6:46 PM


using System.Collections.Generic;
using System.Linq;
using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.Hediffs;

public class HediffComp_VatLearningModeOverride : HediffComp
{
    private const float BasicXPToAward = 8000f;
    private Hediff_VatLearning Parent => (Hediff_VatLearning)parent;

    public HediffComp_VatLearningModeOverride()
    {
        //set severity from mod settings
        HediffCompProperties_SeverityPerDay comp = (HediffCompProperties_SeverityPerDay)Parent.TryGetComp<HediffComp_SeverityPerDay>()?.props;
        if (comp == null)
            return;

        comp.severityPerDay = EnhancedGrowthVatMod.Settings.VatLearningHediffSeverityPerDay;
    }

    public void LearnMode(Pawn_SkillTracker skillTracker, LearningMode mode)
    {
        Dictionary<string, float> skillsMatrix = EnhancedGrowthVatMod.Settings.SkillsMatrix(mode);
        SkillRecord skill = skillTracker.skills.Where(s => !s.TotallyDisabled).RandomElementByWeight(s => Mathf.Pow(skillsMatrix[s.def.defName], 2));

        float modeXPToAward = EnhancedGrowthVatMod.Settings.XpToAward[mode.ToString()] * LearningUtility.LearningRateFactor(Parent.pawn);
        skill.Learn(modeXPToAward, true);
    }

    //copy of Hediff_VatLearning.Learn() behaviour because we destroy access to the original with our harmony patch
    public void LearnBasic(Pawn_SkillTracker skillTracker)
    {
        if (skillTracker.skills.Where(x => !x.TotallyDisabled).TryRandomElement(out SkillRecord skill))
            skill.Learn(BasicXPToAward, true);
    }

    public void Learn()
    {
        if (Parent.pawn.skills is { } skillTracker)
        {
            if (Parent.pawn.ParentHolder is Building_GrowthVat vat && vat.GetComp<EnhancedGrowthVatComp>() is { Enabled: true } comp)
                LearnMode(skillTracker, comp.Mode);
            else
                LearnBasic(skillTracker);
        }

        Parent.Severity = Parent.def.initialSeverity;
    }
}
