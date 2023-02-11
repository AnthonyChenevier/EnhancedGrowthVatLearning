// HediffComp_SeverityFromWorldExposure.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 1:27 PM
// Last edited by: Anthony Chenevier on 2023/01/05 12:32 PM


using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class HediffCompProperties_SeverityFromChildhoodEvent : HediffCompProperties
{
    public float youngChildModifier;
    public float severityPerSocialAction;
    public float severityPerFamilySocialAction;
    public float severityPerLearningAction;
    public float severityPerRitual;

    public HediffCompProperties_SeverityFromChildhoodEvent() { compClass = typeof(HediffComp_SeverityFromChildhoodEvent); }
}

public class HediffComp_SeverityFromChildhoodEvent : HediffComp
{
    private const int CheckInterval = 2500; //long tick

    private int socialActionCount;
    private int familySocialActionCount;
    private int learningActionCount;
    private float ritualPercentage;

    private HediffCompProperties_SeverityFromChildhoodEvent Props => (HediffCompProperties_SeverityFromChildhoodEvent)props;
    private float SocialActionSeverity => socialActionCount * Props.severityPerSocialAction;
    private float FamilySocialActionSeverity => familySocialActionCount * Props.severityPerFamilySocialAction;
    private float LearningActionSeverity => learningActionCount * Props.severityPerLearningAction;
    public float RitualSeverity => ritualPercentage * Props.severityPerRitual;
    private bool PawnIsYoung => Pawn.ageTracker.AgeBiologicalTicks / GenDate.TicksPerYear < GrowthUtility.GrowthMomentAges.First();


    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        //only ticks when out of vat on long tick.
        if (parent.pawn.ParentHolder is Building_GrowthVat || !Pawn.IsHashIntervalTick(CheckInterval))
            return;

        severityAdjustment += ActionCountSeverityAdjustment();
        //reset once applied
        socialActionCount = 0;
        familySocialActionCount = 0;
        learningActionCount = 0;
        ritualPercentage = 0f;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref socialActionCount, nameof(socialActionCount));
        Scribe_Values.Look(ref familySocialActionCount, nameof(familySocialActionCount));
        Scribe_Values.Look(ref learningActionCount, nameof(learningActionCount));
        Scribe_Values.Look(ref ritualPercentage, nameof(ritualPercentage));
    }

    private float ActionCountSeverityAdjustment()
    {
        float severityChange = SocialActionSeverity + FamilySocialActionSeverity + LearningActionSeverity;
        //modify by age
        if (PawnIsYoung)
            severityChange *= Props.youngChildModifier;

        return severityChange;
    }


    public override string CompDebugString()
    {
        StringBuilder stringBuilder = new();
        if (!Pawn.Dead)
        {
            stringBuilder.AppendLine("Severity changes applied next long tick:");
            stringBuilder.AppendLine("from learning actions: " + LearningActionSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("from social actions: " + SocialActionSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("from family social actions: " + FamilySocialActionSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("from rituals: " + RitualSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("modifier for age: " + (PawnIsYoung ? Props.youngChildModifier : 1).ToStringPercent("F3"));
            stringBuilder.AppendLine("final value: " + ActionCountSeverityAdjustment().ToStringPercent("F3"));
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }

    //keep these as lightweight as possible, they may be called often.
    public void Notify_SocialEvent(Pawn other)
    {
        if (other.relations.FamilyByBlood.Contains(Pawn))
            familySocialActionCount++;
        else
            socialActionCount++;
    }

    public void Notify_LearningEvent() => learningActionCount++;
}
