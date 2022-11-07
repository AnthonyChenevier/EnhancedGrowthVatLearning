// EnhancedGrowthVatComp.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 2:41 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 AM


using System.Collections.Generic;
using EnhancedGrowthVat.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVat.ThingComps;

public class EnhancedGrowthVatComp : ThingComp
{
    //state variables
    private LearningMode mode;

    private bool enabled;

    //internal cache for adding random variance to growth
    private float growthPointsInt = EnhancedGrowthVatMod.Settings.VatLearningRate;

    public LearningMode Mode => mode;
    public bool Enabled => enabled;

    private Building_GrowthVat GrowthVat => (Building_GrowthVat)parent;

    private CompPowerMulti powerMulti;
    private CompPowerMulti PowerMulti => powerMulti ??= parent.GetComp<CompPowerMulti>();

    public float GrowthPointsPerDay => growthPointsInt;

    public int GrowthFactor =>
        mode is LearningMode.Leader
            ? EnhancedGrowthVatMod.Settings.VatGrowthFactor - EnhancedGrowthVatMod.Settings.LeaderGrowthFactorModifier
            : EnhancedGrowthVatMod.Settings.VatGrowthFactor;


    public Hediff VatLearning =>
        GrowthVat.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(enabled ? ModDefOf.EnhancedVatLearningHediffDef : HediffDefOf.VatLearning) ??
        GrowthVat.SelectedPawn.health.AddHediff(enabled ? ModDefOf.EnhancedVatLearningHediffDef : HediffDefOf.VatLearning);

    public string ModeDisplay
    {
        get
        {
            string currentMode = "CurrentLearningMode".Translate($"LearningModes_{mode}".Translate());
            string modeDescription = $"{mode}Mode_Desc".Translate();
            string modeTrainingPriorities = ModeTrainingPriorities(mode.ToString());
            return $"{currentMode}\n{modeDescription}\n\n{modeTrainingPriorities}";
        }
    }

    private float LearningRate =>
        mode is LearningMode.Combat or LearningMode.Labor
            ? EnhancedGrowthVatMod.Settings.VatLearningRate * EnhancedGrowthVatMod.Settings.SpecializedRateModifier
            : EnhancedGrowthVatMod.Settings.VatLearningRate;

    public static string ModeTrainingPriorities(string modeString)
    {
        return $"{"TrainingPriorities_Label".Translate()}:\n" +
               $"\t{ColorByWeight(SkillDefOf.Shooting, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Melee, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Construction, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Mining, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Cooking, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Plants, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Animals, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Crafting, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Artistic, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Medicine, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Social, modeString)}\n" +
               $"\t{ColorByWeight(SkillDefOf.Intellectual, modeString)}";
    }

    private static string ColorByWeight(SkillDef skill, string learningMode)
    {
        return EnhancedGrowthVatMod.Settings.SkillsMatrix(learningMode)[skill.defName] switch
        {
            10f => $"<color=#5c7d59>{skill.LabelCap}: +</color>", //dark green
            15f => $"<color=#57b94d>{skill.LabelCap}: ++</color>", //mid green
            20f => $"<color=#18ea03>{skill.LabelCap}: +++</color>", //light green
            _ => $"<color=#434343>{skill.LabelCap}: -</color>" //grey
        };
    }

    public void SetVatHediffs(Pawn_HealthTracker pawnHealth)
    {
        if (enabled)
        {
            if (pawnHealth.hediffSet.HasHediff(HediffDefOf.VatGrowing))
            {
                pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatGrowing));
                pawnHealth.AddHediff(ModDefOf.EnhancedVatGrowingHediffDef);
            }

            if (!pawnHealth.hediffSet.HasHediff(HediffDefOf.VatLearning))
                return;

            pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatLearning));
            pawnHealth.AddHediff(ModDefOf.EnhancedVatLearningHediffDef);
        }
        else
        {
            if (pawnHealth.hediffSet.HasHediff(ModDefOf.EnhancedVatGrowingHediffDef))
            {
                pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(ModDefOf.EnhancedVatGrowingHediffDef));
                pawnHealth.AddHediff(HediffDefOf.VatGrowing);
            }

            if (!pawnHealth.hediffSet.HasHediff(ModDefOf.EnhancedVatLearningHediffDef))
                return;

            pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(ModDefOf.EnhancedVatLearningHediffDef));
            pawnHealth.AddHediff(HediffDefOf.VatLearning);
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.IsHashIntervalTick(50000) || !enabled) //12 times a day
            return;

        float varianceBounds = EnhancedGrowthVatMod.Settings.GrowthPointDailyVariance / 2;
        //vary growth rate by a small random amount each day
        float randVariance = 1f - Rand.Range(-varianceBounds, varianceBounds);
        growthPointsInt = randVariance * LearningRate;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        ResearchProjectDef vatResearch = ModDefOf.EnhancedGrowthVatResearchProjectDef;

        Command_Toggle enhancedLearningGizmo = new()
        {
            defaultLabel = "ToggleLearning_Label".Translate(),
            defaultDesc = "ToggleLearning_Desc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Icons/Learning/Lessontaking"),
            activateSound = enabled ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn,
            isActive = () => enabled,
            toggleAction = () =>
            {
                //toggle enabled, update hediffs and change power consumption
                SetEnabled(!enabled);
            },
        };

        if (!vatResearch.IsFinished)
            enhancedLearningGizmo.Disable("EnhancedLearningResearchRequired_DisabledReason".Translate(vatResearch.LabelCap));
        else if (!GrowthVat.Working)
            enhancedLearningGizmo.Disable("VatOccupantRequired_DisabledReason".Translate());
        else if (GrowthVat.SelectedPawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby)
            enhancedLearningGizmo.Disable("VatBabiesForbidden_DisabledReason".Translate());

        yield return enhancedLearningGizmo;


        ResearchProjectDef soldierResearch = ModDefOf.VatLearningSoldierProjectDef;
        ResearchProjectDef laborResearch = ModDefOf.VatLearningLaborProjectDef;
        ResearchProjectDef leaderResearch = ModDefOf.VatLearningLeaderProjectDef;

        string mainDesc = "LearningModeSwitch_Desc".Translate();
        LearningMode nextUnlockedMode = mode switch
        {
            LearningMode.Combat => laborResearch.IsFinished ? LearningMode.Labor : LearningMode.Default,
            LearningMode.Labor => leaderResearch.IsFinished ? LearningMode.Leader : LearningMode.Default,
            LearningMode.Leader => LearningMode.Default,
            _ => soldierResearch.IsFinished ? LearningMode.Combat : LearningMode.Labor
        };

        string nextMode = "NextLearningMode".Translate($"LearningModes_{nextUnlockedMode}".Translate());

        Command_Action cycleLearningModeGizmo = new()
        {
            defaultLabel = "LearningModeSwitch_Label".Translate(),
            defaultDesc = $"{mainDesc}\n\n{ModeDisplay}\n\n{nextMode}",
            icon = ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{mode}"),
            activateSound = SoundDefOf.Designate_Claim,
            action = () =>
            {
                //change mode and update internal learning rate
                mode = nextUnlockedMode;
                growthPointsInt = LearningRate;
            },
        };

        if (!laborResearch.IsFinished && !soldierResearch.IsFinished)
            cycleLearningModeGizmo.Disable("LearningModeResearchRequired_DisabledReason".Translate(laborResearch.LabelCap, soldierResearch.LabelCap));

        yield return cycleLearningModeGizmo;
    }

    public void SetEnabled(bool enable)
    {
        enabled = enable;
        SetVatHediffs(GrowthVat.SelectedPawn.health);
        string profileName = enabled ? "EnhancedLearning" : "Default";
        if (!PowerMulti.TrySetPowerProfile(profileName))
            Log.Error($"VariablePowerComp profile name \"{profileName}\" could not be found");
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref enabled, nameof(enabled));
        Scribe_Values.Look(ref mode, nameof(mode));
        base.PostExposeData();
    }
}
