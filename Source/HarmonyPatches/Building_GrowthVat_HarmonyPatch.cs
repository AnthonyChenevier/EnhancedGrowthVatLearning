// Building_GrowthVat_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 6:59 PM
// Last edited by: Anthony Chenevier on 2023/01/03 6:59 PM


using System.Collections.Generic;
using System.Linq;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.HediffGivers;
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
    public static void TryAcceptPawn_Postfix(Pawn ___selectedPawn, Building_GrowthVat __instance)
    {
        if (!___selectedPawn.health.hediffSet.HasHediff(GVODefOf.VatgrowthExposureHediff))
            ___selectedPawn.health.AddHediff(GVODefOf.VatgrowthExposureHediff);

        //refresh vat comp
        __instance.GetComp<CompOverclockedGrowthVat>().Refresh();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Notify_PawnRemoved")]
    public static void Notify_PawnRemoved_Postfix(Pawn ___selectedPawn, Building_GrowthVat __instance)
    {
        //make sure we were ejected first
        if (___selectedPawn.ParentHolder != null || ___selectedPawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatgrowthExposureHediff) is not { } hediff)
            return;

        //find any hediff givers 
        hediff.def.hediffGivers.OfType<HediffGiver_OnVatExit>().FirstOrDefault()?.Notify_PawnRemoved(___selectedPawn, hediff);
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
