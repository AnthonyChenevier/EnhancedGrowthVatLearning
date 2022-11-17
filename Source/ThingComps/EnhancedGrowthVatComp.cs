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
            string modeTrainingPriorities = mode == LearningMode.Play ? "" : $"\n\n{ModeTrainingPriorities(mode.ToString())}";
            return $"{currentMode}\n{modeDescription}{modeTrainingPriorities}";
        }
    }

    public VatGrowthTrackerComp GrowthTracker
    {
        get
        {
            if (GrowthVat.SelectedPawn.GetComp<VatGrowthTrackerComp>() is { } trackerComp)
                return trackerComp;

            //don't add trackers to adults
            if (GrowthVat.SelectedPawn.ageTracker.CurLifeStageRace.minAge > GrowthUtility.GrowthMomentAges[GrowthUtility.GrowthMomentAges.Length - 1])
                return null;

            //setup a new growth tracker for our held pawn
            trackerComp = new VatGrowthTrackerComp
            {
                parent = GrowthVat.SelectedPawn,
            };

            GrowthVat.SelectedPawn.AllComps.Add(trackerComp);
            return trackerComp;
        }
    }

    public int VatAgingFactorWithStatModifier(Pawn pawn) { return Mathf.FloorToInt(VatAgingFactor * pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed)); }


    private static float LearningNeedForModeWithVariance(LearningMode currentMode)
    {
        float modeLearningNeed = currentMode switch
        {
            LearningMode.Combat or LearningMode.Labor => EnhancedGrowthVatMod.Settings.SpecializedModesLearningNeed,
            LearningMode.Play => EnhancedGrowthVatMod.Settings.PlayModeLearningNeed,
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
            pawn.needs.learning is { } learning &&
            parent.IsHashIntervalTick(GenDate.TicksPerDay / EnhancedGrowthVatMod.Settings.LearningNeedDailyChangeRate))
            learning.CurLevel = LearningNeedForModeWithVariance(mode);
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
        //use hex colors instead of .Colorize(), hard to get good color scale with that
        return EnhancedGrowthVatMod.Settings.SkillsMatrix(learningMode)[skill.defName] switch
        {
            > 5f and <= 10f => $"<color=#5c7d59>{skill.LabelCap}: +</color>", //muted green
            > 10f and <= 15f => $"<color=#57b94d>{skill.LabelCap}: ++</color>", //midGreen
            >= 20f => $"<color=#18ea03>{skill.LabelCap}: +++</color>", //brightGreen
            _ => $"<color=#434343>{skill.LabelCap}: -</color>", //grey
        };
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        ResearchProjectDef vatResearch = ModDefOf.EnhancedGrowthVatResearchProjectDef;
        ResearchProjectDef soldierResearch = ModDefOf.VatLearningSoldierProjectDef;
        ResearchProjectDef laborResearch = ModDefOf.VatLearningLaborProjectDef;
        ResearchProjectDef leaderResearch = ModDefOf.VatLearningLeaderProjectDef;
        ResearchProjectDef playResearch = ModDefOf.VatLearningPlayProjectDef;

        //enhanced learning toggle
        Command_Toggle enhancedLearningGizmo = new()
        {
            defaultLabel = "ToggleLearning_Label".Translate(),
            defaultDesc = "ToggleLearning_Desc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EnhancedLearningGizmo"),
            activateSound = enabled ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn,
            isActive = () => enabled,
            toggleAction = () => { SetEnabled(!enabled); },
        };

        if (!vatResearch.IsFinished)
            enhancedLearningGizmo.Disable("EnhancedLearningResearchRequired_DisabledReason".Translate(vatResearch.LabelCap));
        else if (GrowthVat.SelectedPawn == null)
            enhancedLearningGizmo.Disable("VatOccupantRequired_DisabledReason".Translate());
        else if (GrowthVat.SelectedPawn.ageTracker.CurLifeStageIndex == 0)
            enhancedLearningGizmo.Disable("VatBabiesForbidden_DisabledReason".Translate());

        yield return enhancedLearningGizmo;

        //learning mode switch
        string mainDesc = "LearningModeSwitch_Desc".Translate();
        string modeDescription = enabled ? ModeDisplay : $"{"LearningModeDisabled_Notice".Translate().Colorize(ColorLibrary.RedReadable)}\n\n{ModeDisplay}";

        Command_Action learningModeGizmo = new()
        {
            defaultLabel = "LearningModeSwitch_Label".Translate(),
            defaultDesc = $"{mainDesc}\n\n{modeDescription}",
            icon = ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{mode}"),
            activateSound = SoundDefOf.Designate_Claim,
            action = () => { Find.WindowStack.Add(new FloatMenu(ModeMenuOptions(playResearch, soldierResearch, laborResearch, leaderResearch))); },
        };

        if (!laborResearch.IsFinished && !soldierResearch.IsFinished && !playResearch.IsFinished)
            learningModeGizmo.Disable("LearningModeResearchRequired_DisabledReason".Translate(laborResearch.LabelCap, soldierResearch.LabelCap, playResearch.LabelCap));

        yield return learningModeGizmo;
    }

    private List<FloatMenuOption> ModeMenuOptions(ResearchProjectDef playResearch,
                                                  ResearchProjectDef soldierResearch,
                                                  ResearchProjectDef laborResearch,
                                                  ResearchProjectDef leaderResearch)
    {
        List<FloatMenuOption> floatMenuOptions = new();

        if (playResearch.IsFinished)
            floatMenuOptions.Add(new FloatMenuOption($"LearningModes_{LearningMode.Play}".Translate(),
                                                     () => SetMode(LearningMode.Play),
                                                     ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{LearningMode.Play}"),
                                                     Color.white));

        if (soldierResearch.IsFinished)
            floatMenuOptions.Add(new FloatMenuOption($"LearningModes_{LearningMode.Combat}".Translate(),
                                                     () => SetMode(LearningMode.Combat),
                                                     ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{LearningMode.Combat}"),
                                                     Color.white));

        if (laborResearch.IsFinished)
            floatMenuOptions.Add(new FloatMenuOption($"LearningModes_{LearningMode.Labor}".Translate(),
                                                     () => SetMode(LearningMode.Labor),
                                                     ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{LearningMode.Labor}"),
                                                     Color.white));

        if (leaderResearch.IsFinished)
            floatMenuOptions.Add(new FloatMenuOption($"LearningModes_{LearningMode.Leader}".Translate(),
                                                     () => SetMode(LearningMode.Leader),
                                                     ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{LearningMode.Leader}"),
                                                     Color.white));

        //default always visible
        floatMenuOptions.Add(new FloatMenuOption($"LearningModes_{LearningMode.Default}".Translate(),
                                                 () => SetMode(LearningMode.Default),
                                                 ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{LearningMode.Default}"),
                                                 Color.white));

        return floatMenuOptions;
    }

    public void SetMode(LearningMode learningMode)
    {
        //change mode and update growth point rate for occupant
        mode = learningMode;
        if (GrowthVat.SelectedPawn?.needs.learning is { } learning)
            learning.CurLevel = LearningNeedForModeWithVariance(mode);
    }

    public void SetEnabled(bool enable)
    {
        enabled = enable;
        if (!enabled)
            pausedForLetter = false;

        SetPowerProfile(enabled);

        //babies and nulls get nothing
        if (GrowthVat.SelectedPawn is not { } pawn || pawn.ageTracker.CurLifeStageIndex == 0)
            return;

        //13-18 (child & teenager(adult)) can still benefit from skill learning and growth speed hediffs
        SetVatHediffs(pawn.health);

        //but only children have learning need
        if (pawn.needs.learning != null)
            pawn.needs.learning.CurLevel = enabled ? LearningNeedForModeWithVariance(mode) * LearningUtility.LearningRateFactor(pawn) : 0.02f;
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
