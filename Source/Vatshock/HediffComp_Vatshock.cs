// HediffComp_CounselHediffThought.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/24 6:46 PM
// Last edited by: Anthony Chenevier on 2022/12/24 6:46 PM


using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class HediffCompProperties_Vatshock : HediffCompProperties
{
    public ThoughtDef thoughtDef;
    public IntRange counselTokensPerUse;
    public SimpleCurve thoughtChangeSeverityMtbQuadrumCurve;
    public HediffCompProperties_Vatshock() => compClass = typeof(HediffComp_Vatshock);
}

public class HediffComp_Vatshock : HediffComp
{
    private const int CheckInterval = 2000;

    private int counselTokens;
    private int thoughtStageIndex;

    private HediffCompProperties_Vatshock Props => (HediffCompProperties_Vatshock)props;

    public int CurThoughtStageIndex => thoughtStageIndex;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        //attempt changing thought stage each long tick. mtbUnit is a quadrum. mtb curve is based on hediff severity so higher severity causes fluctuation more often
        if (!Pawn.IsHashIntervalTick(CheckInterval) ||
            !Rand.MTBEventOccurs(Props.thoughtChangeSeverityMtbQuadrumCurve.Evaluate(parent.Severity), GenDate.TicksPerQuadrum, CheckInterval))
            return;

        int newStageIndex = RandomWeightedThoughtStage();
        if (newStageIndex > thoughtStageIndex)
            SendThoughtChangeMessage();

        thoughtStageIndex = newStageIndex;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref counselTokens, nameof(counselTokens));
        Scribe_Values.Look(ref thoughtStageIndex, nameof(thoughtStageIndex));
    }

    private void SendThoughtChangeMessage()
    {
        //setup and post a message when thought stage changes.
        //"pawn's vatshock has taken a turn for the worse. Try counseling them to reduce the chance of further turns
    }

    public void Counsel()
    {
        //increment counselcount by random tokens (default 1-2)
        counselTokens = Props.counselTokensPerUse.RandomInRange;
    }

    private int RandomWeightedThoughtStage()
    {
        //randomly select thoughtStage from available range.
        ////max thought stage is (the lower of highest thought stage or current hediff stage * 2), reduced by times counseled
        int max = Mathf.Max(Mathf.Min(Props.thoughtDef.stages.Count - 1, parent.CurStageIndex * 2) - counselTokens, 0);

        //weight each element in the range by ^2 + 1 (so 0 index still has a tiny weight against others). Current stage has equally small weight for being kept.
        return Enumerable.Range(0, max).RandomElementByWeight(i => i == thoughtStageIndex ? 1 : i * i + 1);
    }

    public void Notify_InpirationUsed()
    {
        //handle inspiration 
    }
}
