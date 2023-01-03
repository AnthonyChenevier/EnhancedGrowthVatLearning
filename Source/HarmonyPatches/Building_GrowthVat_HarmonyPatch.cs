// Building_GrowthVat_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 6:59 PM
// Last edited by: Anthony Chenevier on 2023/01/03 6:59 PM


using System.Collections.Generic;
using GrowthVatsOverclocked.Hediffs;
using GrowthVatsOverclocked.ThingComps;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(Building_GrowthVat))]
public static class Building_GrowthVat_HarmonyPatch
{
    //Override to get our property instead. Destructive prefix instead of postfix
    //to prevent a loop of referencing and re-caching the original VatLearning hediff
    [HarmonyPrefix]
    [HarmonyPatch("VatLearning", MethodType.Getter)]
    public static bool VatLearning_Prefix(Building_GrowthVat __instance, ref Hediff __result)
    {
        __result = __instance.GetComp<CompOverclockedGrowthVat>().VatLearning;
        return false;
    }

    //Override to refresh enhanced mode on pawn entry (fixes extra Vat Learning hediff if kids enter when not babies)
    [HarmonyPostfix]
    [HarmonyPatch("TryAcceptPawn")]
    public static void TryAcceptPawn_Postfix(Pawn pawn, Building_GrowthVat __instance)
    {
        if (pawn.ParentHolder == __instance)
            __instance.GetComp<CompOverclockedGrowthVat>().Refresh();
    }

    //Override to fix DEV: Learn gizmo
    [HarmonyPostfix]
    [HarmonyPatch("GetGizmos")]
    public static IEnumerable<Gizmo> GetGizmos_Postfix(IEnumerable<Gizmo> gizmos, Building_GrowthVat __instance)
    {
        foreach (Gizmo gizmo in gizmos)
            //rebuild the dev:learn gizmo to use the comp Learn()
            if (gizmo is Command_Action { defaultLabel: "DEV: Learn" } command &&
                ((Hediff_VatLearning)__instance.GetComp<CompOverclockedGrowthVat>().VatLearning)?.TryGetComp<HediffComp_VatLearningModeOverride>() is { } comp)
                yield return new Command_Action
                {
                    defaultLabel = $"{command.defaultLabel} Enhanced",
                    action = comp.Learn
                };
            else //send the rest through untouched
                yield return gizmo;
    }
}
