// CompTimedActionOwner_GrowthVat.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/25 6:24 PM
// Last edited by: Anthony Chenevier on 2023/03/25 6:24 PM


using System;
using System.Collections.Generic;
using System.Reflection;
using GrowthVatsOverclocked.ClassExtensions;
using GrowthVatsOverclocked.CountdownTimers;
using GrowthVatsOverclocked.Data;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class CompCountdownTimerOwner_GrowthVat : CompCountdownTimerOwner
{
    private class VatTimerResearchMod : ResearchMod
    {
        private readonly CompCountdownTimerOwner_GrowthVat owner;

        public VatTimerResearchMod(CompCountdownTimerOwner_GrowthVat owner) { this.owner = owner; }

        public override void Apply()
        {
            //only apply in-game, not on game load
            if (Current.ProgramState != ProgramState.Playing)
                return;

            owner.ejectCountdownTimer.SetEnabled(true);
            owner.recallCountdownTimer.SetEnabled(false);
        }
    }

    private const string ejectTimerID = "EjectTimer";
    private const string recallTimerID = "RecallTimer";

    private List<CountdownTimerSettings> settings;

    private CountdownTimer ejectCountdownTimer;
    private CountdownTimer recallCountdownTimer;

    private Building_GrowthVat Vat => (Building_GrowthVat)parent;
    private Pawn AssignedPawn => parent.GetComp<CompAssignableToPawn_GrowthVat>().AssignedPawn;

    public override bool TimerTabVisible => base.TimerTabVisible && GVODefOf.VatTimersResearch.IsFinished;

    public override int VatTimeFactor => Vat.GetComp<CompOverclockedGrowthVat>()?.ModeGrowthSpeed ?? Building_GrowthVat.AgeTicksPerTickInGrowthVat;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        ejectCountdownTimer ??= new CountdownTimer(this, ejectTimerID, EjectCallback, 18, CountdownTimer.SpanType.YearSpan, CountdownTimer.TickType.PawnAgeTick);
        recallCountdownTimer ??= new CountdownTimer(this, recallTimerID, RecallCallback, 3, CountdownTimer.SpanType.DaySpan, CountdownTimer.TickType.GameTimeTick);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref ejectCountdownTimer, nameof(ejectCountdownTimer), this, ejectTimerID, new Action(EjectCallback));
        Scribe_Deep.Look(ref recallCountdownTimer, nameof(recallCountdownTimer), this, recallTimerID, new Action(RecallCallback));


        if (Scribe.mode != LoadSaveMode.LoadingVars)
            return;

        FieldInfo researchModsField = typeof(ResearchProjectDef).GetField("researchMods", BindingFlags.NonPublic | BindingFlags.Instance);
        if (researchModsField == null)
            return;

        if (researchModsField.GetValue(GVODefOf.VatTimersResearch) is not List<ResearchMod> currentMods)
            currentMods = new List<ResearchMod>();

        currentMods.Add(new VatTimerResearchMod(this));
        researchModsField.SetValue(GVODefOf.VatTimersResearch, currentMods);
    }

    public override IEnumerable<CountdownTimer> GetTimers()
    {
        yield return ejectCountdownTimer;
        yield return recallCountdownTimer;
    }

    public override IEnumerable<CountdownTimerSettings> GetSettings() =>
        settings ??= new List<CountdownTimerSettings> { new(ejectCountdownTimer), new(recallCountdownTimer, CountdownTimer.TickType.VatTimeTick) };

    public override AcceptanceReport TimerCanStart(CountdownTimer timer)
    {
        Pawn pawn = AssignedPawn;
        if (pawn == null || pawn.Dead)
            return "NoStartReason_NoAssignedPawn".Translate();

        if (timer == ejectCountdownTimer && !Vat.innerContainer.Contains(pawn))
            return "NoStartReason_VatEmpty".Translate();

        if (timer == recallCountdownTimer)
        {
            if (pawn.Spawned)
            {
                if (pawn.Map != parent.Map)
                    return "NoStartReason_PawnNotOnMap".Translate(pawn.Named("PAWN"));
            }
            else //!Spawned
            {
                if (pawn.ParentHolder is { } holder)
                {
                    if (holder == Vat)
                        return "NoStartReason_PawnInVat".Translate(pawn.Named("PAWN"));

                    return "NoStartReason_PawnHeld".Translate(pawn.Named("PAWN"), (Thing)holder);
                }
            }
        }

        return true;
    }

    public override AcceptanceReport ValidateSettings(float newSpanAmount, CountdownTimer.SpanType newSpanType, CountdownTimer.TickType newTickType)
    {
        float spanTicks = newSpanAmount * CountdownTimer.TicksPerSpan(newSpanType);
        int ageTicksForAutoEject = 18 * GenDate.TicksPerYear;

        switch (newTickType)
        {
            case CountdownTimer.TickType.GameTimeTick:
            {
                if (AssignedPawn.ageTracker.AgeBiologicalTicks + spanTicks >= ageTicksForAutoEject)
                    return "NotValidReason_PawnOverEighteen".Translate(AssignedPawn.Named("PAWN"));

                break;
            }
            case CountdownTimer.TickType.VatTimeTick:
            {
                if (AssignedPawn.ageTracker.AgeBiologicalTicks + spanTicks * VatTimeFactor >= ageTicksForAutoEject)
                    return "NotValidReason_PawnOverEighteen".Translate(AssignedPawn.Named("PAWN"));

                break;
            }
            case CountdownTimer.TickType.PawnAgeTick:
            {
                if (spanTicks >= ageTicksForAutoEject)
                    return "NotValidReason_PawnOverEighteen".Translate(AssignedPawn.Named("PAWN"));

                break;
            }
        }

        return true;
    }

    public override int GetTicks(CountdownTimer.TickType tickType, bool startTicks = false)
    {
        if (tickType == CountdownTimer.TickType.GameTimeTick)
            return GenTicks.TicksGame;

        if (AssignedPawn is not { } pawn)
            return -1;

        return tickType switch
        {
            CountdownTimer.TickType.VatTimeTick => (int)pawn.ageTracker.vatGrowTicks * VatTimeFactor,
            CountdownTimer.TickType.PawnAgeTick => startTicks ? 0 : (int)pawn.ageTracker.AgeBiologicalTicks,
            _ => -1
        };
    }


    //timed action methods
    private void EjectCallback()
    {
        if (Vat.innerContainer.Count > 0)
            Vat.FinishPawn_Public();
    }

    private void RecallCallback()
    {
        if (AssignedPawn is { IsPrisonerOfColony: false, Downed: false, Spawned: true } pawn && pawn.Map == parent.Map)
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.EnterBuilding, (LocalTargetInfo)parent));
    }

    //hooks for vat entrance and exit
    public void Notify_PawnEnteredVat()
    {
        if (recallCountdownTimer.IsRunning)
            recallCountdownTimer.Stop();

        if (ejectCountdownTimer.IsEnabled)
            ejectCountdownTimer.Start();
    }

    public void Notify_PawnExitedVat()
    {
        if (ejectCountdownTimer.IsRunning)
            ejectCountdownTimer.Stop();

        if (recallCountdownTimer.IsEnabled)
            recallCountdownTimer.Start(true);
    }
}
