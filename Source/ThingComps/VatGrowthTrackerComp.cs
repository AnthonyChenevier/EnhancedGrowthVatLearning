// VatGrowthTrackerComp.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/12 3:38 PM
// Last edited by: Anthony Chenevier on 2022/11/12 3:38 PM


using System.Collections.Generic;
using System.Linq;
using EnhancedGrowthVatLearning.Data;
using Verse;

namespace EnhancedGrowthVatLearning.ThingComps;

public class VatGrowthTrackerComp : ThingComp
{
    private Pawn Pawn => (Pawn)parent;
    private long VatTicks => Pawn.ageTracker.vatGrowTicks;

    private Dictionary<int, long> _biologicalTicks = new();
    public long BiologicalTicksTotal => _biologicalTicks.Values.Sum();


    private Dictionary<LearningMode, long> _modeVatTicks = new()
    {
        { LearningMode.Default, 0 },
        { LearningMode.Labor, 0 },
        { LearningMode.Combat, 0 },
        { LearningMode.Leader, 0 },
    };

    public Dictionary<LearningMode, long> ModeVatTicks => _modeVatTicks;


    private Dictionary<LearningMode, long> _modeBiologicalTicks = new()
    {
        { LearningMode.Default, 0 },
        { LearningMode.Labor, 0 },
        { LearningMode.Combat, 0 },
        { LearningMode.Leader, 0 },
    };

    public Dictionary<LearningMode, long> BiologicalTicksModeBiologicalicks => _modeBiologicalTicks;

    public long DefaultModeVatTicks => _modeVatTicks[LearningMode.Default];
    public long LaborModeVatTicks => _modeVatTicks[LearningMode.Labor];
    public long SoldierModeVatTicks => _modeVatTicks[LearningMode.Combat];
    public long LeaderModeVatTicks => _modeVatTicks[LearningMode.Leader];
    public long EnhancedModesNonLeaderTicks => DefaultModeVatTicks + LaborModeVatTicks + SoldierModeVatTicks;

    private float DefaultModePercent => LearningModePercent(LearningMode.Default);
    private float LaborModePercent => LearningModePercent(LearningMode.Labor);
    private float SoldierModePercent => LearningModePercent(LearningMode.Combat);
    private float LeaderModePercent => LearningModePercent(LearningMode.Leader);
    private float LearningModesNonLeaderPercent => DefaultModePercent + LaborModePercent + SoldierModePercent;
    private float AllEnhancedModesPercent => LeaderModePercent + LearningModesNonLeaderPercent;
    public float NormalGrowthPercent => 1f - AllEnhancedModesPercent;

    public float LearningModePercent(LearningMode mode) { return 1f / BiologicalTicksTotal * _modeBiologicalTicks[mode]; }


    public float TicksToAdulthoodPercent(long adultMinAgeTicks) { return 1f / adultMinAgeTicks * BiologicalTicksTotal; }

    public float TicksForLifeStagePercent(int lifeStageIndex, long minAge, long maxAge)
    {
        long lifeStageTotalTicks = maxAge - minAge;
        if (!_biologicalTicks.ContainsKey(lifeStageIndex))
            return 0f;

        if (_biologicalTicks[lifeStageIndex] >= lifeStageTotalTicks)
            return 1f;

        return 1f / lifeStageTotalTicks * (_biologicalTicks[lifeStageIndex] - minAge);
    }


    public void TrackGrowthTicks(int ticks, int lifeStageIndex, LearningMode mode)
    {
        //track vat ticks like Pawn_AgeTracker, but for each mode
        _modeVatTicks[mode]++;
        //also track biological ticks (note: biological ticks in vat are
        //not adjusted for storyteller growth speed so this should track 1:1)
        if (!_biologicalTicks.ContainsKey(lifeStageIndex))
            _biologicalTicks.Add(lifeStageIndex, ticks);
        else
            _biologicalTicks[lifeStageIndex] += ticks;

        _modeBiologicalTicks[mode] += ticks;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref _biologicalTicks, nameof(_biologicalTicks), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref _modeVatTicks, nameof(_modeVatTicks), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref _modeBiologicalTicks, nameof(_modeBiologicalTicks), LookMode.Value, LookMode.Value);
    }
}
