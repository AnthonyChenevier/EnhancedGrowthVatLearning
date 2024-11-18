// HediffGiver_CauseHediffSeverityBase.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/14 1:43 PM
// Last edited by: Anthony Chenevier on 2023/01/14 1:43 PM


using Verse;

namespace GrowthVatsOverclocked.HediffGivers;

public abstract class HediffGiver_CauseHediffSeverityBase : HediffGiver
{
    public bool giveWhileSuspended = false; //can handle growth vat ticking this way
    public float minAge;
    public float minCauseSeverity = 0.5f;
    public SimpleCurve causeSeverityMappingCurve; //cause severity -> hediff severity

    protected virtual bool CanApply(Pawn pawn, Hediff cause)
    {
        return (!pawn.Suspended || giveWhileSuspended) &&
               cause.Severity >= minCauseSeverity &&
               pawn.ageTracker.AgeBiologicalYears >= minAge &&
               !pawn.health.hediffSet.HasHediff(hediff);
    }

    protected virtual void ApplyCauseSeverity(Pawn pawn, Hediff cause)
    {
        if (pawn.health.hediffSet.GetFirstHediffOfDef(hediff) is { } newHediff)
            newHediff.Severity = causeSeverityMappingCurve?.Evaluate(cause.Severity) ?? cause.Severity;
    }
}
