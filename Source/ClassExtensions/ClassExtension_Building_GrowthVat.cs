// Building_GrowthVat_Extensions.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/02/21 5:47 PM
// Last edited by: Anthony Chenevier on 2023/03/31 2:46 PM


using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.HediffGivers;
using GrowthVatsOverclocked.VatExtensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.ClassExtensions;

[HarmonyPatch(typeof(Building_GrowthVat))]
public static class ClassExtension_Building_GrowthVat
{
    //Public Accessors
    public static void FinishPawn_Public(this Building_GrowthVat vat) =>
        typeof(Building_GrowthVat).GetMethod("FinishPawn", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod).Invoke(vat, null);


    //Harmony Patches 

    //Override to get our property instead. Destructive prefix instead of postfix
    //to prevent a loop of referencing and re-caching the original VatLearning hediff
    [HarmonyPrefix]
    [HarmonyPatch("VatLearning", MethodType.Getter)]
    public static bool VatLearning_Prefix(Building_GrowthVat __instance, ref Hediff __result)
    {
        __result = __instance.GetComp<CompOverclockedGrowthVat>().VatLearning;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Notify_PawnRemoved")]
    public static void Notify_PawnRemoved_Postfix(Building_GrowthVat __instance)
    {
        //find any OnVatExit hediff givers and notify them
        Pawn pawn = __instance.SelectedPawn;
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs.Where(h => h.def.hediffGivers != null && h.def.hediffGivers.Any(g => g is HediffGiver_OnVatExit)))
        foreach (HediffGiver_OnVatExit giver in hediff.def.hediffGivers.OfType<HediffGiver_OnVatExit>())
            giver.Notify_PawnRemoved(pawn, hediff);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Building_GrowthVat.TryAcceptPawn))]
    public static void TryAcceptPawn_Postfix(Building_GrowthVat __instance)
    {
        if (!__instance.innerContainer.Contains(__instance.SelectedPawn))
            return;

        __instance.GetComp<CompCountdownTimerOwner_GrowthVat>().Notify_PawnEnteredVat();
    }

    [HarmonyPostfix]
    [HarmonyPatch("FinishPawn")]
    public static void FinishPawn_Postfix(Building_GrowthVat __instance) { __instance.GetComp<CompCountdownTimerOwner_GrowthVat>().Notify_PawnExitedVat(); }

    //Override to fix DEV: Learn gizmo
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Building_GrowthVat.GetGizmos))]
    public static IEnumerable<Gizmo> GetGizmos_Postfix(IEnumerable<Gizmo> gizmos, Building_GrowthVat __instance)
    {
        foreach (Gizmo gizmo in gizmos)
            //rebuild the dev:learn gizmo to use our extension comp's Learn() if possible
            if (gizmo is Command_Action { defaultLabel: "DEV: Learn" } command &&
                __instance.SelectedPawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.OverclockedVatLearningHediff)?.TryGetComp<HediffComp_OverclockedVatLearning>() is { } comp)
                yield return new Command_Action { defaultLabel = $"{command.defaultLabel} (overclocked)", action = comp.Learn };
            else //send the rest through untouched
                yield return gizmo;
    }
}
