// EnhancedGrowthVatComp.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 11:05 PM
// Last edited by: Anthony Chenevier on 2022/11/10 8:42 PM


using System.Collections.Generic;
using EnhancedGrowthVatLearning.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.ThingComps;

public class EnhancedGrowthVatComp : ThingComp
{
    private bool enabled;

    //state variables
    private LearningMode mode;

    private bool pausedForLetter;

    //cache once
    private CompPowerMulti powerMulti;
    public LearningMode Mode => mode;
    public bool Enabled => enabled;

    public bool PausedForLetter
    {
        get => pausedForLetter;
        internal set => pausedForLetter = value;
    }


    private Building_GrowthVat GrowthVat => (Building_GrowthVat)parent;
    private CompPowerMulti PowerMulti => powerMulti ??= parent.GetComp<CompPowerMulti>();

    public int VatAgingFactor =>
        mode is LearningMode.Leader
            ? EnhancedGrowthVatMod.Settings.VatAgingFactor - EnhancedGrowthVatMod.Settings.LeaderAgingFactorModifier
            : EnhancedGrowthVatMod.Settings.VatAgingFactor;


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


    private static float LearningNeedForModeWithVariance(LearningMode currentMode)
    {
        float modeLearningNeed = currentMode switch
        {
            LearningMode.Combat or LearningMode.Labor => EnhancedGrowthVatMod.Settings.SpecializedModesLearningNeed,
            LearningMode.Leader => EnhancedGrowthVatMod.Settings.LeaderModeLearningNeed,
            _ => EnhancedGrowthVatMod.Settings.DefaultModeLearningNeed
        };

        float randRange = EnhancedGrowthVatMod.Settings.LearningNeedVariance;
        float randVariance = 1f - Rand.Range(-randRange, randRange);

        return modeLearningNeed * randVariance;
    }

    public override void CompTick()
    {
        base.CompTick();
        //vary learning need by small random amount a number of times daily
        if (enabled &&
            GrowthVat.SelectedPawn is { } pawn &&
            pawn.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Child &&
            parent.IsHashIntervalTick(60000 / EnhancedGrowthVatMod.Settings.LearningNeedDailyChangeRate))
            pawn.needs.learning.CurLevel = LearningNeedForModeWithVariance(mode);
    }


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
            > 5f and <= 10f => $"<color=#5c7d59>{skill.LabelCap}: +</color>", //dark green
            > 10f and <= 15f => $"<color=#57b94d>{skill.LabelCap}: ++</color>", //mid green
            >= 20f => $"<color=#18ea03>{skill.LabelCap}: +++</color>", //light green
            _ => $"<color=#434343>{skill.LabelCap}: -</color>", //grey
        };
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
            toggleAction = () => { SetEnabled(!enabled); },
        };

        if (!vatResearch.IsFinished)
            enhancedLearningGizmo.Disable("EnhancedLearningResearchRequired_DisabledReason".Translate(vatResearch.LabelCap));
        else if (GrowthVat.SelectedPawn == null)
            enhancedLearningGizmo.Disable("VatOccupantRequired_DisabledReason".Translate());
        else if (GrowthVat.SelectedPawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby)
            enhancedLearningGizmo.Disable("VatBabiesForbidden_DisabledReason".Translate());

        yield return enhancedLearningGizmo;

        ResearchProjectDef soldierResearch = ModDefOf.VatLearningSoldierProjectDef;
        ResearchProjectDef laborResearch = ModDefOf.VatLearningLaborProjectDef;
        ResearchProjectDef leaderResearch = ModDefOf.VatLearningLeaderProjectDef;

        string mainDesc = "LearningModeSwitch_Desc".Translate();

        string modeDescription = enabled ? ModeDisplay : $"<color=red>{"LearningModeDisabled_Notice".Translate()}</color>\n\n{ModeDisplay}";

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
            defaultDesc = $"{mainDesc}\n\n{modeDescription}\n\n{nextMode}",
            icon = ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{mode}"),
            activateSound = SoundDefOf.Designate_Claim,
            action = () =>
            {
                //change mode and update growth point rate for occupant
                mode = nextUnlockedMode;
                if (GrowthVat.SelectedPawn != null && GrowthVat.SelectedPawn.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Child)
                    GrowthVat.SelectedPawn.needs.learning.CurLevel = LearningNeedForModeWithVariance(mode);
            },
        };

        if (!laborResearch.IsFinished && !soldierResearch.IsFinished)
            cycleLearningModeGizmo.Disable("LearningModeResearchRequired_DisabledReason".Translate(laborResearch.LabelCap, soldierResearch.LabelCap));

        yield return cycleLearningModeGizmo;
    }

    public void SetEnabled(bool enable)
    {
        enabled = enable;
        if (!enabled)
            pausedForLetter = false;

        SetPowerProfile(enabled);

        if (GrowthVat.SelectedPawn == null || GrowthVat.SelectedPawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby)
            return;

        //13-18 any non-baby) can still benefit from skill learning and growth speed hediffs
        SetVatHediffs(GrowthVat.SelectedPawn.health);

        //but only children have learning need
        if (GrowthVat.SelectedPawn.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Child)
            GrowthVat.SelectedPawn.needs.learning.CurLevel = enabled ? LearningNeedForModeWithVariance(mode) : 0.2f;
    }

    private void SetPowerProfile(bool enable)
    {
        string powerProfile = enable ? "EnhancedLearning" : "Default";
        if (!PowerMulti.TrySetPowerProfile(powerProfile))
            Log.Error($"VariablePowerComp profile name \"{powerProfile}\" could not be found");
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

    //save/load stuff
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref enabled, nameof(enabled));
        Scribe_Values.Look(ref pausedForLetter, nameof(pausedForLetter));
        Scribe_Values.Look(ref mode, nameof(mode));
        base.PostExposeData();
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        SetEnabled(enabled);
    }
}
