// VatGrowthTrackerComp.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/11/12 3:38 PM
// Last edited by: Anthony Chenevier on 2022/11/12 3:38 PM


using System.Collections.Generic;
using GrowthVatsOverclocked.Data;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked;

public class VatGrowthTracker : IExposable
{
    private Pawn _pawn;
    public Pawn Pawn => _pawn;

    private long _vatTicksBiological;
    public long VatTicksBiological => _vatTicksBiological;

    private Dictionary<LearningMode, long> _modeTicks = new()
    {
        { LearningMode.Default, 0 },
        { LearningMode.Labor, 0 },
        { LearningMode.Combat, 0 },
        { LearningMode.Leader, 0 },
        { LearningMode.Play, 0 },
    };

    public VatGrowthTracker(Pawn pawn) { _pawn = pawn; }
    public VatGrowthTracker() { }

    public Dictionary<LearningMode, long> ModeTicks => _modeTicks;

    public float NormalGrowthPercent =>
        1f -
        LearningModePercent(LearningMode.Default) -
        LearningModePercent(LearningMode.Labor) -
        LearningModePercent(LearningMode.Combat) -
        LearningModePercent(LearningMode.Leader) -
        LearningModePercent(LearningMode.Play);

    public LearningMode MostUsedMode => _modeTicks.Keys.MaxBy(p => _modeTicks[p]);
    public float MostUsedModePercent => LearningModePercent(MostUsedMode);
    public bool RequiresVatBackstory => VatTicksBiological >= GrowthVatsOverclockedMod.Settings.Data.vatDaysForBackstory * GenDate.TicksPerDay;

    public float LearningModePercent(LearningMode mode) { return 1f / _vatTicksBiological * _modeTicks[mode]; }

    public void TrackGrowthTicks(int ticks, bool enhancedEnabled, LearningMode mode)
    {
        //track biological ticks (note: biological ticks in vat are
        //not adjusted for storyteller growth speed so this should track 1:1)
        _vatTicksBiological += ticks;
        if (enhancedEnabled)
            _modeTicks[mode] += ticks;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref _pawn, nameof(_pawn));
        Scribe_Values.Look(ref _vatTicksBiological, nameof(_vatTicksBiological));
        Scribe_Collections.Look(ref _modeTicks, nameof(_modeTicks), LookMode.Value, LookMode.Value);
    }
}
