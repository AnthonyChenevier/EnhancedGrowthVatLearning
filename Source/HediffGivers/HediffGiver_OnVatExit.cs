// HediffGiver_OnVatExit.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/14 1:44 PM
// Last edited by: Anthony Chenevier on 2023/01/14 1:45 PM


using Verse;

namespace GrowthVatsOverclocked.HediffGivers;

public class HediffGiver_OnVatExit : HediffGiver_CauseHediffSeverityBase
{
    public SimpleCurve causeSeverityEventChanceCurve; //cause severity -> event chance

    public void Notify_PawnRemoved(Pawn pawn, Hediff cause)
    {
        if (causeSeverityEventChanceCurve == null || !CanApply(pawn, cause) || Rand.Value >= causeSeverityEventChanceCurve.Evaluate(cause.Severity) || !TryApply(pawn))
            return;

        ApplyCauseSeverity(pawn, cause);
        SendLetter(pawn, cause);
    }
}
