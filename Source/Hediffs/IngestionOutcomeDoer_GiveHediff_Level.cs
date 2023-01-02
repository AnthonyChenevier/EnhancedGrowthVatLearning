// IngestionOutcomeDoer_GiveHediff_Level.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/11/25 11:07 PM
// Last edited by: Anthony Chenevier on 2022/11/25 11:07 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Hediffs;

public class IngestionOutcomeDoer_GiveHediff_Level : IngestionOutcomeDoer_GiveHediff
{
    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
    {
        if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) is Hediff_Level hediffLevel)
        {
            hediffLevel.ChangeLevel((int)severity);
        }
        else
        {
            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            hediff.Severity = severity;
            hediff.Part = pawn.health.hediffSet.GetBrain();
            pawn.health.AddHediff(hediff);
        }
    }
}