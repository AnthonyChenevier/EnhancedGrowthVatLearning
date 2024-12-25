// HediffComp_OverclockedVatLearning.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/02/12 10:54 AM
// Last edited by: Anthony Chenevier on 2023/02/21 5:33 PM


using System.Collections.Generic;
using System.Linq;
using GrowthVatsOverclocked.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class HediffCompProperties_OverclockedVatLearning : HediffCompProperties
{
    public string labelExtraOverclocked;
    public string descriptionExtraOverclocked;
    public string tipStringExtraPaused;
    public HediffCompProperties_OverclockedVatLearning() { compClass = typeof(HediffComp_OverclockedVatLearning); }
}

/// <summary>
/// Extension for VatLearning hediff to override parent.Learn() functionality and add more details to the hediff.
/// </summary>
public class HediffComp_OverclockedVatLearning : HediffComp
{
    private const float BasicXPToAward = 8000f;
    private HediffCompProperties_OverclockedVatLearning Props => (HediffCompProperties_OverclockedVatLearning)props;
    private Hediff_VatLearning Parent => (Hediff_VatLearning)parent;
    private CompOverclockedGrowthVat GrowthVatComp => ((Building_GrowthVat)Pawn.ParentHolder).GetComp<CompOverclockedGrowthVat>();

    //NOTE: can't use labelInBrackets as it's overridden by Hediff_VatLearning to skip comps.
    public override string CompLabelPrefix => GrowthVatComp.IsOverclocked ? Props.labelExtraOverclocked : "";
    public override string CompDescriptionExtra => GrowthVatComp.IsOverclocked ? Props.descriptionExtraOverclocked : "";

    public override string CompTipStringExtra
    {
        get
        {
            if (GrowthVatComp.VatgrowthPaused)
                return Props.tipStringExtraPaused;

            LearningMode mode = GrowthVatComp.CurrentMode;
            return GrowthVatComp.IsOverclocked ? CurrentModeTipString(mode) : "";
        }
    }

    private static string CurrentModeTipString(LearningMode mode) { return $"{"CurrentMode_Label".Translate(mode.Label())}\n\n{mode.TrainingPriorities()}"; }

    /// <summary>
    /// bypass Hediff_VatLearning.Learn() to use selected learning mode if overclocked.
    /// </summary>
    public void Learn()
    {
        if (Parent.pawn.skills is { } skillTracker)
        {
            if (Parent.pawn.ParentHolder is Building_GrowthVat vat && vat.GetComp<CompOverclockedGrowthVat>() is { IsOverclocked: true } comp)
                LearnMode(skillTracker, comp.CurrentMode);
            else
                LearnBasic(skillTracker);
        }

        Parent.Severity = Parent.def.initialSeverity;
    }

    public void LearnMode(Pawn_SkillTracker skillTracker, LearningMode mode)
    {
        Dictionary<string, float> skillsMatrix = mode.Settings().skillSelectionWeights;
        SkillRecord skill = skillTracker.skills.Where(s => !s.TotallyDisabled && skillsMatrix.ContainsKey(s.def.defName))
                                        .RandomElementByWeight(s => Mathf.Pow(skillsMatrix[s.def.defName], 2));

        float modeXPToAward = mode.Settings().skillXP * Parent.pawn.GetStatValue(StatDefOf.LearningRateFactor);
        skill.Learn(modeXPToAward, true);
    }

    //copy of Hediff_VatLearning.Learn() behaviour because we remove access to the original with our harmony patch
    public void LearnBasic(Pawn_SkillTracker skillTracker)
    {
        if (skillTracker.skills.Where(x => !x.TotallyDisabled).TryRandomElement(out SkillRecord skill))
            skill.Learn(BasicXPToAward, true);
    }

    //change severity per day for all active hediffs of this type (for changing settings mid-game)
    public static void SetGlobalSetting_SeverityPerDay(float severityPerDay)
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        HediffCompProperties_SeverityPerDay compProps =
            (HediffCompProperties_SeverityPerDay)HediffDefOf.VatLearning.comps.FirstOrDefault(c => c.compClass == typeof(HediffCompProperties_SeverityPerDay));

        if (compProps != null)
            compProps.severityPerDay = severityPerDay;
    }
}
