// HediffGiver_VatgrowthStress.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/13 3:29 PM
// Last edited by: Anthony Chenevier on 2022/12/23 1:10 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Hediffs;

public class HediffGiver_RandomSeverityCurved : HediffGiver
{
    public float minAge;
    public float minCauseSeverity = 0.5f;
    public SimpleCurve mtbSeverityCurve;
    public SimpleCurve severityMapCurve = null;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (cause.Severity < minCauseSeverity ||
            pawn.ageTracker.AgeBiologicalYears < minAge ||
            pawn.health.hediffSet.HasHediff(hediff) ||
            pawn.health.immunity.AnyGeneMakesFullyImmuneTo(hediff))
            return;

        if (!Rand.MTBEventOccurs(mtbSeverityCurve.Evaluate(cause.Severity), GenDate.TicksPerDay, HealthTuning.HediffGiverUpdateInterval) || !TryApply(pawn))
            return;

        pawn.health.hediffSet.GetFirstHediffOfDef(hediff).Severity = severityMapCurve?.Evaluate(cause.Severity) ?? cause.Severity;
        SendLetter(pawn, cause);
    }
}
