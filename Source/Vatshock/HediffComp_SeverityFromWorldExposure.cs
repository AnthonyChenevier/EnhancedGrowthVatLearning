// HediffComp_SeverityFromWorldExposure.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 1:27 PM
// Last edited by: Anthony Chenevier on 2023/01/05 12:32 PM


using System.Text;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class HediffCompProperties_SeverityFromWorldExposure : HediffCompProperties
{
    public float youngChildModifier;
    public float severityPerSocialAction;
    public float socialActionFamilyModifier;
    public float severityPerLearningAction;
    public HediffCompProperties_SeverityFromWorldExposure() { compClass = typeof(HediffComp_SeverityFromWorldExposure); }
}

public class HediffComp_SeverityFromWorldExposure : HediffComp_SeverityPerDay
{
    private int socialActionCount;
    private int familySocialActionCount;
    private int learningActionCount;
    HediffCompProperties_SeverityFromWorldExposure Props => (HediffCompProperties_SeverityFromWorldExposure)props;

    private float SocialActionSeverity => socialActionCount * Props.severityPerSocialAction;
    private float FamilySocialActionSeverity => familySocialActionCount * (Props.severityPerSocialAction * Props.socialActionFamilyModifier);
    private float LearningActionSeverity => learningActionCount * Props.severityPerLearningAction;

    public override float SeverityChangePerDay()
    {
        //only works out of vat
        if (parent.pawn.ParentHolder is Building_GrowthVat)
            return 0;


        float severityChange = base.SeverityChangePerDay();
        //modify by age modifier
        if (Pawn.ageTracker.AgeBiologicalTicks / GenDate.TicksPerYear < 13)
            severityChange *= Props.youngChildModifier;

        //add all severity changes together
        severityChange += SocialActionSeverity + FamilySocialActionSeverity + LearningActionSeverity;

        return severityChange;
    }

    public override string CompDebugString()
    {
        StringBuilder stringBuilder = new();
        if (!Pawn.Dead)
        {
            stringBuilder.AppendLine("base severity/day: " + base.SeverityChangePerDay().ToString("F8"));
            stringBuilder.AppendLine("social action severity/day: " + SocialActionSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("family social action severity/day: " + FamilySocialActionSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("learning action severity/day: " + LearningActionSeverity.ToStringPercent("F3"));
            stringBuilder.AppendLine("final severity/day: " + SeverityChangePerDay().ToString("F8"));
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }
}
