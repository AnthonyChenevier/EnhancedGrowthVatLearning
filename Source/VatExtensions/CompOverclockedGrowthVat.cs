// EnhancedGrowthVatComp.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 11:05 PM
// Last edited by: Anthony Chenevier on 2022/11/10 8:42 PM


using System.Collections.Generic;
using GrowthVatsOverclocked.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

/// <summary>
/// Central part of the mod, does most of the heavy lifting that isn't done by harmony patches
/// </summary>
public class CompOverclockedGrowthVat : ThingComp
{
    //exposed comp vars
    internal bool overclockingEnabled; //main switch for all overclocked behaviour
    internal LearningMode currentMode; //the selected learning suite/mode
    internal bool vatgrowthPaused; //overclocked vats pause growth and learning when a growth letter is pending so growth points aren't wasted.

    //overclocked vats have higher power requirements. Cache comp here for easy access
    private CompPowerMulti powerMulti;

    //accessors for comp vars
    public bool IsOverclocked => overclockingEnabled;

    public LearningMode CurrentMode
    {
        get => currentMode;
        set
        {
            //change value and recalculate learning need
            currentMode = value;
            CalculateLearningNeed();
        }
    }

    public bool VatgrowthPaused
    {
        get => vatgrowthPaused;
        set => vatgrowthPaused = value;
    }

    private Building_GrowthVat GrowthVat => (Building_GrowthVat)parent;

    //re-cache occupant speed once per day to reduce expensive computation

    public int ModeGrowthSpeed => IsOverclocked ? currentMode.Settings().growthSpeed : Building_GrowthVat.AgeTicksPerTickInGrowthVat;

    public int StatDerivedGrowthSpeed => Mathf.FloorToInt(ModeGrowthSpeed * GrowthVat.SelectedPawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed));


    public Hediff VatLearning
    {
        get
        {
            HediffDef learningHediffDef = overclockingEnabled && GrowthVat.SelectedPawn is { } pawn && !pawn.ageTracker.CurLifeStage.developmentalStage.Baby()
                                              ? GVODefOf.OverclockedVatLearningHediff
                                              : HediffDefOf.VatLearning;

            return GrowthVat.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(learningHediffDef) ?? GrowthVat.SelectedPawn.health.AddHediff(learningHediffDef);
        }
    }
    //Overrides

    //cache power multi comp and refresh on spawn
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        powerMulti = parent.GetComp<CompPowerMulti>();
        Refresh();
    }

    public override void CompTick()
    {
        base.CompTick();
        //vary learning need by small random amount a number of times daily
        if (overclockingEnabled && parent.IsHashIntervalTick(GenDate.TicksPerDay / GrowthVatsOverclockedMod.Settings.Data.learningNeedDailyChangeRate))
            CalculateLearningNeed();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        ResearchProjectDef vatResearch = GVODefOf.GrowthVatOverclockingResearch;
        ResearchProjectDef soldierResearch = GVODefOf.VatLearningSoldierResearch;
        ResearchProjectDef laborResearch = GVODefOf.VatLearningLaborResearch;
        ResearchProjectDef leaderResearch = GVODefOf.VatLearningLeaderResearch;
        ResearchProjectDef playResearch = GVODefOf.VatLearningPlayResearch;

        //enhanced learning toggle
        string disabledForBabyNotice = "";
        if (GrowthVat.SelectedPawn is { } pawn && pawn.ageTracker.CurLifeStage.developmentalStage.Baby())
            disabledForBabyNotice = $"\n\n{"EnhancedLearningDisabledBabies_Notice".Translate().Colorize(ColorLibrary.RedReadable)}";

        Command_Toggle enhancedLearningGizmo = new()
        {
            defaultLabel = "ToggleLearning_Label".Translate(),
            defaultDesc = $"{"ToggleLearning_Desc".Translate()}{disabledForBabyNotice}",
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EnhancedLearningGizmo"),
            activateSound = overclockingEnabled ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn,
            isActive = () => overclockingEnabled,
            toggleAction = () => { EnableOverclocking(!overclockingEnabled); },
        };

        if (!vatResearch.IsFinished)
            enhancedLearningGizmo.Disable("EnhancedLearningResearchRequired_DisabledReason".Translate(vatResearch.LabelCap));

        yield return enhancedLearningGizmo;

        //learning mode switch
        string disabledNotEnhancedNotice = !overclockingEnabled ? $"\n\n{"LearningModeDisabled_Notice".Translate().Colorize(ColorLibrary.RedReadable)}" : "";
        Command_Action learningModeGizmo = new()
        {
            defaultLabel = "LearningModeSwitch_Label".Translate(),
            defaultDesc =
                $"{"LearningModeSwitch_Desc".Translate()}\n\n{"CurrentMode_Label".Translate(currentMode.Label())}\n\n{currentMode.Description()}{disabledNotEnhancedNotice}",
            icon = currentMode.Icon(),
            activateSound = SoundDefOf.Click,
            action = () => { Find.WindowStack.Add(new FloatMenu(ModeMenuOptions(playResearch, soldierResearch, laborResearch, leaderResearch))); },
        };

        if (!laborResearch.IsFinished && !soldierResearch.IsFinished && !playResearch.IsFinished)
            learningModeGizmo.Disable("LearningModeResearchRequired_DisabledReason".Translate(laborResearch.LabelCap, soldierResearch.LabelCap, playResearch.LabelCap));

        yield return learningModeGizmo;
    }

    //save/load
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref overclockingEnabled, nameof(overclockingEnabled));
        Scribe_Values.Look(ref vatgrowthPaused, nameof(vatgrowthPaused));
        Scribe_Values.Look(ref currentMode, nameof(currentMode));
        BackCompatibility.PostExposeData(this);
    }

    //comp methods

    public void Refresh() { EnableOverclocking(overclockingEnabled); }

    public void EnableOverclocking(bool enable)
    {
        overclockingEnabled = enable;
        if (!overclockingEnabled)
            vatgrowthPaused = false; //if we're turning off unpause growth

        //empty vats, embryos & babies skip overclocked features. Refresh on pawn entry and life stage started to re-check
        if (GrowthVat.SelectedPawn is not { } pawn || pawn.ageTracker.CurLifeStage.developmentalStage.Baby())
            return;

        //set working power profile
        SetPowerProfile();

        //13-18 (child & teenager(adult)) can still benefit from skill learning and growth speed hediffs
        SetLearningHediff(pawn.health);

        //add exposure hediff if it doesn't exist
        if (!pawn.health.hediffSet.HasHediff(GVODefOf.VatgrowthExposureHediff))
            pawn.health.AddHediff(GVODefOf.VatgrowthExposureHediff);

        //update learning need if required
        CalculateLearningNeed();
    }

    private void SetPowerProfile()
    {
        string powerProfile = overclockingEnabled ? "Overclocked" : "Default";
        if (!powerMulti.TrySetPowerProfile(powerProfile))
            Log.Error($"GrowthVatsOverclocked :: VariablePowerComp profile name \"{powerProfile}\" could not be found when attempting to set.");
    }

    private void CalculateLearningNeed()
    {
        //only pawns with learning need (kids) continue
        if (GrowthVat.SelectedPawn is not { } pawn || pawn.needs.learning is not { } learning)
            return;

        //if overclocking off emulate low learning (its not used anyway)
        if (!overclockingEnabled)
        {
            learning.CurLevel = 0.02f;
            return;
        }

        //randomize learning need by variance value
        float randRange = GrowthVatsOverclockedMod.Settings.Data.learningNeedVariance;
        learning.CurLevel = currentMode.Settings().baseLearningNeed * (1f - Rand.Range(-randRange, randRange)) * pawn.GetStatValue(StatDefOf.LearningRateFactor);
    }

    private void SetLearningHediff(Pawn_HealthTracker healthTracker)
    {
        HediffDef oldDef = overclockingEnabled ? HediffDefOf.VatLearning : GVODefOf.OverclockedVatLearningHediff;
        HediffDef newDef = overclockingEnabled ? GVODefOf.OverclockedVatLearningHediff : HediffDefOf.VatLearning;

        if (!healthTracker.hediffSet.HasHediff(oldDef))
            return;

        Hediff oldHediff = healthTracker.hediffSet.GetFirstHediffOfDef(oldDef);

        healthTracker.RemoveHediff(oldHediff);
        Hediff newHediff = healthTracker.AddHediff(newDef);

        if (overclockingEnabled)
            newHediff.Severity = oldHediff.Severity;
    }

    private List<FloatMenuOption> ModeMenuOptions(ResearchProjectDef playResearch,
                                                  ResearchProjectDef soldierResearch,
                                                  ResearchProjectDef laborResearch,
                                                  ResearchProjectDef leaderResearch)
    {
        List<FloatMenuOption> options = new();

        if (playResearch.IsFinished)
        {
            bool overPlayAge = GrowthVat.SelectedPawn is { } pawn && pawn.ageTracker.CurLifeStage.developmentalStage.Adult();
            string disabledForTeenNotice = overPlayAge ? $"\n\n{"PlayModeDisabled_Notice".Translate().Colorize(ColorLibrary.RedReadable)}" : "";
            options.Add(ModeMenuOption(LearningMode.Play, $"{LearningMode.Play.Description()}{disabledForTeenNotice}", overPlayAge));
        }

        if (soldierResearch.IsFinished)
            options.Add(ModeMenuOption(LearningMode.Combat, LearningMode.Combat.Description()));

        if (laborResearch.IsFinished)
            options.Add(ModeMenuOption(LearningMode.Labor, LearningMode.Labor.Description()));

        if (leaderResearch.IsFinished)
            options.Add(ModeMenuOption(LearningMode.Leader, LearningMode.Leader.Description()));

        //default always visible
        options.Add(ModeMenuOption(LearningMode.Default, LearningMode.Default.Description()));

        return options;
    }

    private FloatMenuOption ModeMenuOption(LearningMode learningMode, string tooltip, bool isDisabled = false)
    {
        return new FloatMenuOption("SwitchToMode_Label".Translate(learningMode.Label()), () => CurrentMode = learningMode, learningMode.Icon(), Color.white)
        {
            tooltip = new TipSignal(tooltip),
            Disabled = isDisabled
        };
    }
}