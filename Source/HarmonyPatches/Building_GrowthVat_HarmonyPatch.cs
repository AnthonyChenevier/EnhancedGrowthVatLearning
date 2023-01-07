// Building_GrowthVat_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 6:59 PM
// Last edited by: Anthony Chenevier on 2023/01/03 6:59 PM


using System.Collections.Generic;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.VatExtensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(Building_GrowthVat))]
public static class Building_GrowthVat_HarmonyPatch
{
    //Refresh comp on pawn entry to ensure everything is set correctly
    //also ensures VatgrothStressBuildup hediff is applied at the same time as the other hediffs
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Building_GrowthVat.TryAcceptPawn))]
    public static void TryAcceptPawn_Postfix(Pawn pawn, Building_GrowthVat __instance)
    {
        //make sure we were accepted
        if (pawn.ParentHolder == __instance)
        {
            if (!pawn.health.hediffSet.HasHediff(GVODefOf.VatgrowthExposureHediff))
                pawn.health.AddHediff(GVODefOf.VatgrowthExposureHediff);

            //refresh vat comp
            __instance.GetComp<CompOverclockedGrowthVat>().Refresh();
        }
    }

    //Override to fix DEV: Learn gizmo
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Building_GrowthVat.GetGizmos))]
    public static IEnumerable<Gizmo> GetGizmos_Postfix(IEnumerable<Gizmo> gizmos, Building_GrowthVat __instance)
    {
        foreach (Gizmo gizmo in gizmos)
        {
            //rebuild the dev:learn gizmo to use our extension comp's Learn() if possible
            if (gizmo is Command_Action { defaultLabel: "DEV: Learn" } command)
                if (__instance.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatLearning)?.TryGetComp<HediffComp_VatLearningExtension>() is { } comp)
                    yield return new Command_Action
                    {
                        defaultLabel = $"{command.defaultLabel} Enhanced",
                        action = comp.Learn
                    };

            //send the rest through untouched
            yield return gizmo;
        }
    }
}
