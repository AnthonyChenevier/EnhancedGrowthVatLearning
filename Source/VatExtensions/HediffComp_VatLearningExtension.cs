// HediffComp_VatLearningModeOverride.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2023/01/02 2:03 PM
// Last edited by: Anthony Chenevier on 2023/01/02 6:46 PM


using System.Collections.Generic;
using System.Linq;
using GrowthVatsOverclocked.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class HediffCompProperties_VatLearningExtension : HediffCompProperties
{
    public string labelExtraOverclocked;
    public string descriptionExtraOverclocked;
    public string tipStringExtraPaused;
    public HediffCompProperties_VatLearningExtension() { compClass = typeof(HediffCompProperties_VatLearningExtension); }
}

/// <summary>
/// Extension for VatLearning hediff to override parent.Learn() functionality and add more details to the hediff.
/// </summary>
public class HediffComp_VatLearningExtension : HediffComp
{
    private const float BasicXPToAward = 8000f;
    private HediffCompProperties_VatLearningExtension Props => (HediffCompProperties_VatLearningExtension)props;
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
        SkillRecord skill = skillTracker.skills.Where(s => !s.TotallyDisabled).RandomElementByWeight(s => Mathf.Pow(skillsMatrix[s.def.defName], 2));

        float modeXPToAward = mode.Settings().skillXP * LearningUtility.LearningRateFactor(Parent.pawn);
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
        HediffCompProperties_SeverityPerDay compProps =
            (HediffCompProperties_SeverityPerDay)GVODefOf.EnhancedVatLearningHediff.comps.FirstOrDefault(c => c.compClass == typeof(HediffCompProperties_SeverityPerDay));

        compProps.severityPerDay = severityPerDay;

        //IEnumerable<Hediff_VatLearning> allVatLearningHediffs = Find.World.worldPawns.AllPawnsAlive.Where(p => p.health.hediffSet.HasHediff(GVODefOf.EnhancedVatLearningHediff))
        //                                                            .Select(p => p.health.hediffSet.GetFirstHediffOfDef(GVODefOf.EnhancedVatLearningHediff) as Hediff_VatLearning);

        //foreach (Hediff_VatLearning hediff in allVatLearningHediffs)
        //{
        //    HediffComp_SeverityPerDay comp = hediff.TryGetComp<HediffComp_SeverityPerDay>();
        //    if (comp != null)
        //        ((HediffCompProperties_SeverityPerDay)comp.props).severityPerDay = severityPerDay;
        //}
    }
}
