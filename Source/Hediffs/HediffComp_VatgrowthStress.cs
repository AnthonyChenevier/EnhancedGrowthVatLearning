// Hediff_PVSD.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/12/08 1:25 AM
// Last edited by: Anthony Chenevier on 2022/12/12 3:40 PM


using System.Collections.Generic;
using EnhancedGrowthVatLearning.Data;
using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.Hediffs;

public class HediffComp_VatgrowthStress : HediffComp
{
    private const int CheckInterval = 150;

    private HediffCompProperties_VatgrowthStress Props => (HediffCompProperties_VatgrowthStress)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        //adjust severity depending on state.
        // - While in vat, increase severity by a small amount each tick.
        //   Severity should reach a max of 0.49 if in the vat for the full
        //   18 years on vanilla mode with no other modifiers.
        // - If under effect of vat-juice severity per tick is multiplied


        //check for mental breaks
        if (!parent.pawn.IsHashIntervalTick(CheckInterval))
            return;

        if (TestStageMentalBreaks(out MentalBreakDef breakDef))
            TryDoMentalBreak(breakDef);
    }

    private bool TestStageMentalBreaks(out MentalBreakDef breakDef)
    {
        breakDef = null;
        foreach (StageMentalBreak stageMentalBreaks in Props.breakStages[parent.CurStageIndex])
        {
            if (!Rand.MTBEventOccurs(stageMentalBreaks.mtbDays, GenDate.TicksPerDay, CheckInterval))
                continue;

            breakDef = stageMentalBreaks.mentalBreak;
            return true;
        }

        return false;
    }


    private bool TryDoMentalBreak(MentalBreakDef breakDef)
    {
        if (parent.pawn.Downed || !parent.pawn.Awake() || parent.pawn.InMentalState || parent.pawn.Faction != Faction.OfPlayer)
            return false;

        List<Thought> thoughts = new();
        parent.pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
        Thought thought = thoughts.FirstOrDefault(t => t.def == ModDefOf.VatgrowthStressThoughtDef);
        TaggedString reason = "MentalStateReason_Hediff".Translate();
        if (thought != null)
            reason += "\n\n" + "FinalStraw".Translate((NamedArgument)thought.LabelCap);

        return breakDef.Worker.TryStart(parent.pawn, reason, false);
    }
}

public class StageMentalBreak
{
    public MentalBreakDef mentalBreak;
    public float mtbDays;
}

public class HediffCompProperties_VatgrowthStress : HediffCompProperties
{
    public List<List<StageMentalBreak>> breakStages;
    public HediffCompProperties_VatgrowthStress() => compClass = typeof(HediffComp_VatgrowthStress);
}
