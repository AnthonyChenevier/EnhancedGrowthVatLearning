// HediffGiver_VatgrowthStress.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/13 3:29 PM
// Last edited by: Anthony Chenevier on 2022/12/23 1:10 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HediffGivers;

public class HediffGiver_RandomSeverityCurved : HediffGiver
{
    public float minAge;
    public float minCauseSeverity = 0.5f;
    public SimpleCurve causeSeverityMtbCurve; //cause severity -> mtbDays
    public SimpleCurve causeSeverityMapCurve; //cause severity -> hediff severity

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (cause.Severity < minCauseSeverity ||
            pawn.ageTracker.AgeBiologicalYears < minAge ||
            pawn.health.hediffSet.HasHediff(hediff) ||
            pawn.health.immunity.AnyGeneMakesFullyImmuneTo(hediff))
            return;

        if (!Rand.MTBEventOccurs(causeSeverityMtbCurve.Evaluate(cause.Severity), GenDate.TicksPerDay, HealthTuning.HediffGiverUpdateInterval) || !TryApply(pawn))
            return;

        pawn.health.hediffSet.GetFirstHediffOfDef(hediff).Severity = causeSeverityMapCurve?.Evaluate(cause.Severity) ?? cause.Severity;
        SendLetter(pawn, cause);
    }
}
