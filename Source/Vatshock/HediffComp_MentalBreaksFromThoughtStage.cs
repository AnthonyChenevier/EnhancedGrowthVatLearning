// Hediff_PVSD.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/08 1:25 AM
// Last edited by: Anthony Chenevier on 2022/12/12 3:40 PM


using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class ThoughtBreak
{
    public MentalBreakDef mentalBreak;
    public float mtbDays;
}

public class HediffCompProperties_MentalBreaksFromThoughtStage : HediffCompProperties
{
    public ThoughtDef thoughtDef;
    public SimpleCurve thoughtStageMtbMultiplierCurve;
    public float cutoffOverMtbDays;
    public List<ThoughtBreak> thoughtBreaks;
    public HediffCompProperties_MentalBreaksFromThoughtStage() => compClass = typeof(HediffComp_MentalBreaksFromThoughtStage);
}

public class HediffComp_MentalBreaksFromThoughtStage : HediffComp
{
    private const int CheckInterval = 150;

    private HediffCompProperties_MentalBreaksFromThoughtStage Props => (HediffCompProperties_MentalBreaksFromThoughtStage)props;

    private List<Thought> PawnThoughts
    {
        get
        {
            List<Thought> thoughts = new();
            Pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
            return thoughts;
        }
    }


    //check for and start mental breaks if possible to have them
    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (!Pawn.IsHashIntervalTick(CheckInterval) ||
            Pawn.Downed ||
            !Pawn.Awake() ||
            Pawn.InMentalState ||
            Pawn.Faction != Faction.OfPlayer ||
            !TestMentalBreaks(out MentalBreakDef breakDef))
            return;

        breakDef.Worker.TryStart(Pawn, "MentalStateReason_Hediff".Translate(), false);
    }

    private bool TestMentalBreaks(out MentalBreakDef breakDef)
    {
        Thought stressThought = PawnThoughts.FirstOrDefault(t => t.def == Props.thoughtDef);
        foreach (ThoughtBreak stressBreak in Props.thoughtBreaks)
        {
            float mtbDays = stressBreak.mtbDays * Props.thoughtStageMtbMultiplierCurve.Evaluate(stressThought.CurStageIndex);
            if (!(mtbDays < Props.cutoffOverMtbDays) || !Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, CheckInterval))
                continue;

            breakDef = stressBreak.mentalBreak;
            return true;
        }

        breakDef = null;
        return false;
    }
}