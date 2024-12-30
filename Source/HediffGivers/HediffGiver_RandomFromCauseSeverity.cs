// HediffGiver_VatgrowthStress.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/13 3:29 PM
// Last edited by: Anthony Chenevier on 2022/12/23 1:10 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HediffGivers;

public class HediffGiver_RandomFromCauseSeverity : HediffGiver_CauseHediffSeverityBase
{
    public SimpleCurve causeSeverityMtbDaysCurve; //cause severity -> mtbDays
    private string checkSetting;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (!CanApply(pawn, cause) ||
            !Rand.MTBEventOccurs(causeSeverityMtbDaysCurve.Evaluate(cause.Severity), GenDate.TicksPerDay, HealthTuning.HediffGiverUpdateInterval) ||
            !TryApply(pawn))
            return;

        ApplyCauseSeverity(pawn, cause);
        SendLetter(pawn, cause);
    }

    protected override bool CanApply(Pawn pawn, Hediff cause)
    {
        bool b = GrowthVatsOverclockedMod.Settings.CheckBoolSetting(checkSetting);
        return b && base.CanApply(pawn, cause);
    }
}