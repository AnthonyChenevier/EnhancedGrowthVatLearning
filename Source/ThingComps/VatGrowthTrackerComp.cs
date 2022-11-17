// VatGrowthTrackerComp.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/12 3:38 PM
// Last edited by: Anthony Chenevier on 2022/11/12 3:38 PM


using System.Collections.Generic;
using EnhancedGrowthVatLearning.Data;
using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.ThingComps;

public class VatGrowthTrackerComp : ThingComp
{
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
    public bool RequiresVatBackstory => VatTicksBiological >= EnhancedGrowthVatMod.Settings.VatDaysForBackstory * GenDate.TicksPerDay;

    public float LearningModePercent(LearningMode mode) { return 1f / _vatTicksBiological * _modeTicks[mode]; }

    public void TrackGrowthTicks(int ticks, bool enhancedEnabled, LearningMode mode)
    {
        //track biological ticks (note: biological ticks in vat are
        //not adjusted for storyteller growth speed so this should track 1:1)
        _vatTicksBiological += ticks;
        if (enhancedEnabled)
            _modeTicks[mode] += ticks;
    }

    public override void PostExposeData()
    {
        //if we are loading then make sure we have data so we don't keep ourselves attached to every colonist
        long tempValue = _vatTicksBiological;
        Scribe_Values.Look(ref tempValue, nameof(_vatTicksBiological));
        if (Scribe.mode == LoadSaveMode.PostLoadInit && tempValue == 0)
        {
            //Log.Message("EnhancedGrowthVatLearningMod:: VatGrowthTrackerComp was added to untracked pawn to try and load values - _vatTicksBiological was 0. Removing now.");
            parent.AllComps.Remove(this);
            return;
        }

        //keep loading/saving as normal if we have data
        _vatTicksBiological = tempValue;
        Scribe_Collections.Look(ref _modeTicks, nameof(_modeTicks), LookMode.Value, LookMode.Value);
    }
}
