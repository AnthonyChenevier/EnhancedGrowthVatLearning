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
            string powerProfile = enabled ? "EnhancedLearning" : "Default";
            if (!PowerMulti.TrySetPowerProfile(powerProfile))
                Log.Error($"VariablePowerComp profile name \"{powerProfile}\" could not be found");

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
    public int VatTicks => Mathf.FloorToInt(ModeAgingFactor * GrowthVat.SelectedPawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed, cacheStaleAfterTicks: GenDate.TicksPerDay));

    public int ModeAgingFactor =>
        mode is LearningMode.Leader
            ? EnhancedGrowthVatMod.Settings.VatAgingFactor - EnhancedGrowthVatMod.Settings.LeaderAgingFactorModifier
            : EnhancedGrowthVatMod.Settings.VatAgingFactor;


    public Hediff VatLearning
    {
        get
        {
            HediffDef learningHediffDef;
            if (enabled && GrowthVat.SelectedPawn is { } pawn && !pawn.ageTracker.CurLifeStage.developmentalStage.Baby())
                learningHediffDef = ModDefOf.EnhancedVatLearningHediffDef;
            else
                learningHediffDef = HediffDefOf.VatLearning;

            return GrowthVat.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(learningHediffDef) ?? GrowthVat.SelectedPawn.health.AddHediff(learningHediffDef);
        }
    }


    //Overrides


    public override void CompTick()
    {
        base.CompTick();
        //vary learning need by small random amount a number of times daily
        if (enabled && parent.IsHashIntervalTick(GenDate.TicksPerDay / EnhancedGrowthVatMod.Settings.LearningNeedDailyChangeRate))
            CalculateHeldPawnLearningNeed();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        ResearchProjectDef vatResearch = ModDefOf.EnhancedGrowthVatResearchProjectDef;
        ResearchProjectDef soldierResearch = ModDefOf.VatLearningSoldierProjectDef;
        ResearchProjectDef laborResearch = ModDefOf.VatLearningLaborProjectDef;
        ResearchProjectDef leaderResearch = ModDefOf.VatLearningLeaderProjectDef;
        ResearchProjectDef playResearch = ModDefOf.VatLearningPlayProjectDef;

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
            string disabledForTeenNotice = "";
            if (GrowthVat.SelectedPawn is { } pawn && pawn.ageTracker.CurLifeStage.developmentalStage.Adult())
                disabledForTeenNotice = $"\n\n{"PlayModeDisabled_Notice".Translate().Colorize(ColorLibrary.RedReadable)}";

            options.Add(new FloatMenuOption("SwitchToMode_Label".Translate(LearningMode.Play.Label()), () => Mode = LearningMode.Play, LearningMode.Play.Icon(), Color.white)
            {
                Disabled = GrowthVat.SelectedPawn is { } pawn1 && pawn1.ageTracker.CurLifeStage.developmentalStage.Adult(),
                tooltip = new TipSignal($"{LearningMode.Play.Description()}{disabledForTeenNotice}"),
            });
        }

        if (soldierResearch.IsFinished)
            options.Add(new FloatMenuOption("SwitchToMode_Label".Translate(LearningMode.Combat.Label()), () => Mode = LearningMode.Combat, LearningMode.Combat.Icon(), Color.white)
            {
                tooltip = new TipSignal(LearningMode.Combat.Description()),
            });

        if (laborResearch.IsFinished)
            options.Add(new FloatMenuOption("SwitchToMode_Label".Translate(LearningMode.Labor.Label()), () => Mode = LearningMode.Labor, LearningMode.Labor.Icon(), Color.white)
            {
                tooltip = new TipSignal(LearningMode.Labor.Description()),
            });

        if (leaderResearch.IsFinished)
            options.Add(new FloatMenuOption("SwitchToMode_Label".Translate(LearningMode.Leader.Label()), () => Mode = LearningMode.Leader, LearningMode.Leader.Icon(), Color.white)
            {
                tooltip = new TipSignal(LearningMode.Leader.Description()),
            });

        //default always visible
        options.Add(new FloatMenuOption("SwitchToMode_Label".Translate(LearningMode.Default.Label()), () => Mode = LearningMode.Default, LearningMode.Default.Icon(), Color.white)
        {
            tooltip = new TipSignal(LearningMode.Default.Description()),
        });

        return options;
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
        float randRange = EnhancedGrowthVatMod.Settings.LearningNeedVariance;
        float modeLearningNeed = mode switch
        {
            LearningMode.Combat or LearningMode.Labor => EnhancedGrowthVatMod.Settings.SpecializedModesLearningNeed,
            LearningMode.Play => EnhancedGrowthVatMod.Settings.PlayModeLearningNeed,
            LearningMode.Leader => EnhancedGrowthVatMod.Settings.LeaderModeLearningNeed,
            _ => EnhancedGrowthVatMod.Settings.DefaultModeLearningNeed
        };


        learning.CurLevel = modeLearningNeed * (1f - Rand.Range(-randRange, randRange)) * LearningUtility.LearningRateFactor(pawn);
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
}