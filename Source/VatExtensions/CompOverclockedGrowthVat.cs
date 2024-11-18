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

    //comp and parent accessors
    //internal caches
    private CompPowerMulti _powerMultiInt;
    private CompAssignableToPawn_GrowthVat _assignableToPawnInt;
    private Building_GrowthVat GrowthVat => (Building_GrowthVat)parent;
    private CompPowerMulti PowerMulti => _powerMultiInt ??= parent.GetComp<CompPowerMulti>();
    public CompAssignableToPawn_GrowthVat CompAssignableToPawn => _assignableToPawnInt ??= parent.GetComp<CompAssignableToPawn_GrowthVat>();


    public Pawn AssignedPawn => !CompAssignableToPawn.AssignedPawnsForReading.Any() ? null : CompAssignableToPawn.AssignedPawnsForReading[0];

    private bool OccupantIsBaby => GrowthVat.SelectedPawn is { ageTracker.CurLifeStage.developmentalStage: DevelopmentalStage.Baby };
    private bool OccupantIsAdult => GrowthVat.SelectedPawn is { ageTracker.CurLifeStage.developmentalStage : DevelopmentalStage.Adult };

    public int ModeGrowthSpeed => IsOverclocked && !OccupantIsBaby ? currentMode.Settings().growthSpeed : Building_GrowthVat.AgeTicksPerTickInGrowthVat;

    /// <summary>
    /// Uses expensive call to get stat value. Minimize downstream
    /// usage as much as possible.
    /// </summary>
    public int StatDerivedGrowthSpeed => Mathf.FloorToInt(ModeGrowthSpeed * GrowthVat.SelectedPawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed));

    private HediffDef VatLearningDef => overclockingEnabled && !OccupantIsBaby ? GVODefOf.OverclockedVatLearningHediff : HediffDefOf.VatLearning;
    public Hediff VatLearning => GrowthVat.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(VatLearningDef) ?? GrowthVat.SelectedPawn.health.AddHediff(VatLearningDef);

    //Overrides

    //cache power multi comp and refresh on spawn
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
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
        if (OccupantIsBaby)
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
            action = () => { Find.WindowStack.Add(new FloatMenu(ModeSelectOptions(playResearch, soldierResearch, laborResearch, leaderResearch))); },
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

    //use mode growth speed and decompose the modifier from ticks
    //with math so we don't have to touch GetStatValue at all here.
    public int ModeAgeTicks(int originalTicks) => Mathf.FloorToInt(ModeGrowthSpeed * ((float)originalTicks / Building_GrowthVat.AgeTicksPerTickInGrowthVat));

    public void Refresh() { EnableOverclocking(overclockingEnabled); }

    public void EnableOverclocking(bool enable)
    {
        overclockingEnabled = enable;

        if (!overclockingEnabled)
            vatgrowthPaused = false; //if we're turning off unpause growth

        //only non-baby pawns do anything beside change the overclocked setting
        if (GrowthVat.selectedEmbryo != null || GrowthVat.SelectedPawn == null || OccupantIsBaby)
            return;

        //set power profile for this vat
        string profileName = overclockingEnabled ? "Overclocked" : "Default";
        if (!PowerMulti.TrySetPowerProfile(profileName))
            Log.Error($"GrowthVatsOverclocked :: VariablePowerComp profile named \"{profileName}\" could not be found.");

        Pawn_HealthTracker pawnHealth = GrowthVat.SelectedPawn.health;
        //add exposure hediff if it doesn't exist
        if (!pawnHealth.hediffSet.HasHediff(GVODefOf.VatgrowthExposureHediff))
            pawnHealth.AddHediff(GVODefOf.VatgrowthExposureHediff);

        SetLearningHediff(pawnHealth);
        CalculateLearningNeed();
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

    private List<FloatMenuOption> ModeSelectOptions(ResearchProjectDef playResearch,
                                                    ResearchProjectDef soldierResearch,
                                                    ResearchProjectDef laborResearch,
                                                    ResearchProjectDef leaderResearch)
    {
        List<FloatMenuOption> options = new();

        if (playResearch.IsFinished)
        {
            string disabledForTeenNotice = OccupantIsAdult ? $"\n\n{"PlayModeDisabled_Notice".Translate().Colorize(ColorLibrary.RedReadable)}" : "";
            options.Add(CreateModeOption(LearningMode.Play, $"{LearningMode.Play.Description()}{disabledForTeenNotice}", OccupantIsAdult));
        }

        if (soldierResearch.IsFinished)
            options.Add(CreateModeOption(LearningMode.Combat, LearningMode.Combat.Description()));

        if (laborResearch.IsFinished)
            options.Add(CreateModeOption(LearningMode.Labor, LearningMode.Labor.Description()));

        if (leaderResearch.IsFinished)
            options.Add(CreateModeOption(LearningMode.Leader, LearningMode.Leader.Description()));

        //default always visible
        options.Add(CreateModeOption(LearningMode.Default, LearningMode.Default.Description()));

        return options;
    }

    private FloatMenuOption CreateModeOption(LearningMode learningMode, string tooltip, bool isDisabled = false)
    {
        return new FloatMenuOption("SwitchToMode_Label".Translate(learningMode.Label()), () => CurrentMode = learningMode, learningMode.Icon(), Color.white)
        {
            tooltip = new TipSignal(tooltip),
            Disabled = isDisabled
        };
    }
}