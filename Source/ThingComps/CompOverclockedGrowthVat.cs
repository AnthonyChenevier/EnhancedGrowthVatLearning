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

namespace GrowthVatsOverclocked.ThingComps;

/// <summary>
/// Central part of the mod, does most of the heavy lifting that isn't done by harmony patches
/// </summary>
public class CompOverclockedGrowthVat : ThingComp
{
    private bool enabled;

    //state variables
    private LearningMode mode;

    private bool pausedForLetter;

    //cache once
    private CompPowerMulti powerMulti;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!enabled)
                pausedForLetter = false;

            //babies don't use enhanced mode
            if (GrowthVat.SelectedPawn is not { } pawn || pawn.ageTracker.CurLifeStage.developmentalStage.Baby())
                return;

            //set up power profile
            string powerProfile = enabled ? "Overclocked" : "Default";
            if (!PowerMulti.TrySetPowerProfile(powerProfile))
                Log.Error($"GrowthVatsOverclocked :: VariablePowerComp profile name \"{powerProfile}\" could not be found when attempting to set.");

            //13-18 (child & teenager(adult)) can still benefit from skill learning and growth speed hediffs
            SetVatHediffs(pawn.health);

            //but only children have learning need to be updated
            CalculateHeldPawnLearningNeed();
        }
    }

    public LearningMode Mode
    {
        get => mode;
        set
        {
            //change value and update growth point rate for occupant
            mode = value;
            CalculateHeldPawnLearningNeed();
        }
    }

    public bool PausedForLetter
    {
        get => pausedForLetter;
        internal set => pausedForLetter = value;
    }

    private CompPowerMulti PowerMulti => powerMulti ??= parent.GetComp<CompPowerMulti>();
    private Building_GrowthVat GrowthVat => (Building_GrowthVat)parent;

    //re-cache occupant speed once per day to reduce expensive computation
    public int VatTicks =>
        Mathf.FloorToInt((Enabled ? ModeAgingFactor : Building_GrowthVat.AgeTicksPerTickInGrowthVat) *
                         GrowthVat.SelectedPawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed, cacheStaleAfterTicks: GenDate.TicksPerDay));

    public int ModeAgingFactor => mode.Settings().baseAgingFactor;

    public Hediff VatLearning
    {
        get
        {
            HediffDef learningHediffDef;
            if (enabled && GrowthVat.SelectedPawn is { } pawn && !pawn.ageTracker.CurLifeStage.developmentalStage.Baby())
                learningHediffDef = GVODefOf.EnhancedVatLearningHediff;
            else
                learningHediffDef = HediffDefOf.VatLearning;

            return GrowthVat.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(learningHediffDef) ?? GrowthVat.SelectedPawn.health.AddHediff(learningHediffDef);
        }
    }

    public Hediff VatStressBuildup =>
        GrowthVat.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatgrowthStressBuildup) ?? GrowthVat.SelectedPawn.health.AddHediff(GVODefOf.VatgrowthStressBuildup);

    public float DailyGrowthPointFactor => VatTicks / Find.Storyteller.difficulty.childAgingRate;


    //Overrides


    public override void CompTick()
    {
        base.CompTick();
        //vary learning need by small random amount a number of times daily
        if (enabled && parent.IsHashIntervalTick(GenDate.TicksPerDay / GrowthVatsOverclockedMod.Settings.LearningNeedDailyChangeRate))
            CalculateHeldPawnLearningNeed();
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
            activateSound = enabled ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn,
            isActive = () => enabled,
            toggleAction = () => { Enabled = !enabled; },
        };

        if (!vatResearch.IsFinished)
            enhancedLearningGizmo.Disable("EnhancedLearningResearchRequired_DisabledReason".Translate(vatResearch.LabelCap));

        yield return enhancedLearningGizmo;

        //learning mode switch
        string disabledNotEnhancedNotice = !enabled ? $"\n\n{"LearningModeDisabled_Notice".Translate().Colorize(ColorLibrary.RedReadable)}" : "";
        Command_Action learningModeGizmo = new()
        {
            defaultLabel = "LearningModeSwitch_Label".Translate(),
            defaultDesc = $"{"LearningModeSwitch_Desc".Translate()}\n\n{"CurrentMode_Label".Translate(mode.Label())}\n\n{mode.Description()}{disabledNotEnhancedNotice}",
            icon = mode.Icon(),
            activateSound = SoundDefOf.Click,
            action = () => { Find.WindowStack.Add(new FloatMenu(ModeMenuOptions(playResearch, soldierResearch, laborResearch, leaderResearch))); },
        };

        if (!laborResearch.IsFinished && !soldierResearch.IsFinished && !playResearch.IsFinished)
            learningModeGizmo.Disable("LearningModeResearchRequired_DisabledReason".Translate(laborResearch.LabelCap, soldierResearch.LabelCap, playResearch.LabelCap));

        yield return learningModeGizmo;
    }

    //save/load stuff
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref enabled, nameof(enabled));
        Scribe_Values.Look(ref pausedForLetter, nameof(pausedForLetter));
        Scribe_Values.Look(ref mode, nameof(mode));
        base.PostExposeData();
    }

    //refresh on spawn
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Refresh();
    }


    //comp methods
    public void Refresh() => Enabled = enabled;

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
        return new FloatMenuOption("SwitchToMode_Label".Translate(learningMode.Label()), () => Mode = learningMode, learningMode.Icon(), Color.white)
        {
            tooltip = new TipSignal(tooltip),
            Disabled = isDisabled
        };
    }

    private void CalculateHeldPawnLearningNeed()
    {
        if (GrowthVat.SelectedPawn is not { } pawn || pawn.needs.learning is not { } learning)
            return;

        //if turned off emulate low learning (its not used anyway)
        if (!enabled)
        {
            learning.CurLevel = 0.02f;
            return;
        }

        //randomize learning need by variance value
        float randRange = GrowthVatsOverclockedMod.Settings.LearningNeedVariance;
        learning.CurLevel = mode.Settings().baseLearningNeed * (1f - Rand.Range(-randRange, randRange)) * LearningUtility.LearningRateFactor(pawn);
    }

    public void SetVatHediffs(Pawn_HealthTracker pawnHealth)
    {
        if (!pawnHealth.hediffSet.HasHediff(GVODefOf.VatgrowthStressBuildup))
            pawnHealth.AddHediff(GVODefOf.VatgrowthStressBuildup);

        if (enabled)
        {
            if (pawnHealth.hediffSet.HasHediff(HediffDefOf.VatGrowing))
            {
                pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatGrowing));
                pawnHealth.AddHediff(GVODefOf.EnhancedVatGrowingHediff);
            }

            if (!pawnHealth.hediffSet.HasHediff(HediffDefOf.VatLearning))
                return;

            pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatLearning));
            pawnHealth.AddHediff(GVODefOf.EnhancedVatLearningHediff);
        }
        else
        {
            if (pawnHealth.hediffSet.HasHediff(GVODefOf.EnhancedVatGrowingHediff))
            {
                pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(GVODefOf.EnhancedVatGrowingHediff));
                pawnHealth.AddHediff(HediffDefOf.VatGrowing);
            }

            if (!pawnHealth.hediffSet.HasHediff(GVODefOf.EnhancedVatLearningHediff))
                return;

            pawnHealth.RemoveHediff(pawnHealth.hediffSet.GetFirstHediffOfDef(GVODefOf.EnhancedVatLearningHediff));
            pawnHealth.AddHediff(HediffDefOf.VatLearning);
        }
    }
}