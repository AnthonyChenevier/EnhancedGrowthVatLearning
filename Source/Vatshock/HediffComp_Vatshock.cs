// HediffComp_CounselHediffThought.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/24 6:46 PM
// Last edited by: Anthony Chenevier on 2022/12/24 6:46 PM


using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class HediffCompProperties_Vatshock : HediffCompProperties
{
    public ThoughtDef thoughtDef;

    [MustTranslate]
    public string flashbackStartLetterLabel;

    [MustTranslate]
    public string flashbackStartLetterText;

    [MustTranslate]
    public string inspirationReasonText;

    [MustTranslate]
    public string curedLetterText;

    [MustTranslate]
    public string curedLetterLabel;

    public ThoughtStageReducer thoughtStageReducers;

    public IntRange recoveryTokensPerAction;
    public SimpleCurve thoughtChangeSeverityMtbQuadrumCurve;
    public SimpleCurve mtbLifetimeSelfCureChanceCurve;
    public float inspirationFromFlashbackChance;
    public float recoveryCureModifier;

    public HediffCompProperties_Vatshock() => compClass = typeof(HediffComp_Vatshock);
}

public class ThoughtStageReducer
{
    public List<PreceptDef> precepts;
    public List<TraitDef> traits;
    public List<TraitRequirement> traitDegrees;
    public List<GeneDef> genes;

    public bool EffectReducedFor(Pawn pawn) { return PreceptReducesEffect(pawn) is not null || TraitReducesEffect(pawn) is not null || GeneReducesEffect(pawn) is not null; }

    public Gene GeneReducesEffect(Pawn pawn)
    {
        if (!ModsConfig.BiotechActive || genes == null || pawn.genes == null)
            return null;

        return genes.Select(pawn.genes.GetGene).FirstOrDefault(g => g is not null);
    }

    public Trait TraitReducesEffect(Pawn pawn)
    {
        if (traits != null)
            foreach (TraitDef traitDef in traits)
                if (pawn.story.traits.GetTrait(traitDef) is { } trait)
                    return trait;

        return traitDegrees?.Select(t => t.GetTrait(pawn)).FirstOrDefault(t => t is not null);
    }

    public PreceptDef PreceptReducesEffect(Pawn pawn) { return precepts?.FirstOrDefault(t => pawn.Ideo != null && pawn.Ideo.HasPrecept(t)); }
}

public class HediffComp_Vatshock : HediffComp
{
    private const int CheckInterval = 2000;

    private int flashbackRecoveryTokens;

    private HediffCompProperties_Vatshock Props => (HediffCompProperties_Vatshock)props;
    public override string CompLabelInBracketsExtra => HasActiveFlashback ? "flashing back" : "suppressed";

    public bool HasActiveFlashback => Pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(Props.thoughtDef) is not null;

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref flashbackRecoveryTokens, nameof(flashbackRecoveryTokens));
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        //active flashbacks prevent further processing
        if (!Pawn.IsHashIntervalTick(CheckInterval))
            return;

        //check for random cure event
        if (TryRandomCure(ref severityAdjustment))
            return;

        if (HasActiveFlashback)
        {
            TryStartInspiration();
            return;
        }

        //attempt flashback
        TryRandomFlashback();
    }

    private void TryRandomFlashback()
    {
        //mtbUnit is a quadrum. mtb curve is based on hediff severity so higher severity causes fluctuation more often
        if (!Rand.MTBEventOccurs(Props.thoughtChangeSeverityMtbQuadrumCurve.Evaluate(parent.Severity), GenDate.TicksPerQuadrum, CheckInterval))
            return;

        //add and set random memory stage
        MemoryThoughtHandler memories = Pawn.needs.mood.thoughts.memories;
        memories.TryGainMemory(Props.thoughtDef);

        int stageIndex = RandomWeightedStageIndex();
        memories.GetFirstMemoryOfDef(Props.thoughtDef).SetForcedStage(stageIndex);
        TrySendLetter(Props.flashbackStartLetterLabel,
                      Props.flashbackStartLetterText.Formatted(Pawn.LabelShortCap, Pawn.Named("PAWN")),
                      stageIndex > 0 ? LetterDefOf.NegativeEvent : LetterDefOf.NeutralEvent);
    }

    private bool TryRandomCure(ref float severityAdjustment)
    {
        float lifeExpectancyPercent = Pawn.ageTracker.AgeBiologicalYearsFloat / Pawn.RaceProps.lifeExpectancy;
        float mtb = Props.mtbLifetimeSelfCureChanceCurve.Evaluate(lifeExpectancyPercent) + flashbackRecoveryTokens * Props.recoveryCureModifier;
        if (!Rand.MTBEventOccurs(mtb, Pawn.RaceProps.lifeExpectancy * GenDate.TicksPerYear, CheckInterval))
            return false;

        //we're cured.
        severityAdjustment = -parent.Severity;

        //send cured letter
        TrySendLetter(Props.curedLetterLabel, Props.curedLetterText.Formatted(Pawn.LabelShortCap, Pawn.Named("PAWN")), LetterDefOf.PositiveEvent);
        return true;
    }

    private void TryStartInspiration()
    {
        InspirationHandler inspirationHandler = Pawn.mindState.inspirationHandler;
        if (Rand.Value <= Props.inspirationFromFlashbackChance || inspirationHandler.GetRandomAvailableInspirationDef() is not { } inspirationDef)
            return;

        inspirationHandler.TryStartInspiration(inspirationDef, Props.inspirationReasonText.Formatted(Pawn.LabelShortCap, Pawn.Named("PAWN")));
    }

    private void TrySendLetter(string beginLetterLabel, string letterText, LetterDef letterDef)
    {
        if (!PawnUtility.ShouldSendNotificationAbout(Pawn))
            return;

        string pawnLabelShortCap = $"{beginLetterLabel.CapitalizeFirst()}: {Pawn.LabelShortCap}";
        Find.LetterStack.ReceiveLetter(pawnLabelShortCap, letterText, letterDef, Pawn);
    }

    //randomly select thoughtStage from available range. Weights are calculated as index^2 + 1 so the worst flashbacks are much more common. Effect of thoughts is reduced here.
    private int RandomWeightedStageIndex()
    {
        return Props.thoughtStageReducers.EffectReducedFor(Pawn)
                   ? 0
                   : Enumerable.Range(0, Mathf.Max(Props.thoughtDef.stages.Count - flashbackRecoveryTokens - 1, 0)).RandomElementByWeight(i => i * i + 1);
    }

    public void Notify_RecoveryAction()
    {
        flashbackRecoveryTokens += Props.recoveryTokensPerAction.RandomInRange;
        flashbackRecoveryTokens = Mathf.Clamp(flashbackRecoveryTokens, 0, Props.thoughtDef.stages.Count);
    }
}
