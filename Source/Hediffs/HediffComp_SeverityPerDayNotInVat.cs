// HediffComp_SeverityPerDayNotInVat.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/01 9:35 PM
// Last edited by: Anthony Chenevier on 2023/01/01 9:35 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Hediffs;

public class HediffComp_SeverityPerDayNotInVat : HediffComp_SeverityPerDay
{
    public override float SeverityChangePerDay()
    {
        if (parent.pawn.ParentHolder is Building_GrowthVat)
            return 0;

        return base.SeverityChangePerDay();
    }
}
