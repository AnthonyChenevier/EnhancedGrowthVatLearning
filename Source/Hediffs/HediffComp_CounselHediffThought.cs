// HediffComp_CounselHediffThought.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/24 6:46 PM
// Last edited by: Anthony Chenevier on 2022/12/24 6:46 PM


using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Hediffs;

public class HediffCompProperties_CounselHediffThought : HediffCompProperties
{
    public ThoughtDef thoughtDef;
    public HediffCompProperties_CounselHediffThought() => compClass = typeof(HediffComp_CounselHediffThought);
}

public class HediffComp_CounselHediffThought : HediffComp
{
    private int counselCount;
    private Thought stressThought;
    public int CounselCount => counselCount;

    private HediffCompProperties_CounselHediffThought Props => (HediffCompProperties_CounselHediffThought)props;

    private List<Thought> PawnThoughts
    {
        get
        {
            List<Thought> thoughts = new();
            Pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
            return thoughts;
        }
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        stressThought ??= PawnThoughts.FirstOrDefault(t => t.def == Props.thoughtDef);
        if (stressThought?.CurStageIndex <= 0 || !PawnThoughts.Any(t => t.def == ThoughtDefOf.Counselled_MoodBoost))
            return;

        Pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.Counselled_MoodBoost);
        counselCount++;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref counselCount, nameof(counselCount));
    }
}
