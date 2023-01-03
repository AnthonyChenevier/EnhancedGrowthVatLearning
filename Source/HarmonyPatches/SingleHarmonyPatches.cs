// HarmonyPatcher.cs
// 
// Part of BetterGrowthVatLearning - BetterGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/03 3:08 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 PM


using GrowthVatsOverclocked.Hediffs;
using GrowthVatsOverclocked.ThingComps;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

//refresh vatcomp to apply hediffs when babies age up
[HarmonyPatch(typeof(LifeStageWorker_HumanlikeChild), "Notify_LifeStageStarted")]
public static class LifeStageWorker_HumanlikeChild_Notify_LifeStageStarted_HP
{
    public static void Postfix(Pawn pawn)
    {
        if (Current.ProgramState != ProgramState.Playing ||
            !pawn.IsColonist ||
            pawn.ParentHolder is not Building_GrowthVat growthVat ||
            growthVat.GetComp<CompOverclockedGrowthVat>() is not { } vatComp)
            return;

        vatComp.Refresh();
    }
}

//backstory hooks
[HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult), "Notify_LifeStageStarted")]
public static class LifeStageWorker_HumanlikeAdult_Notify_LifeStageStarted_HP
{
    public static void Postfix(Pawn pawn)
    {
        if (Current.ProgramState != ProgramState.Playing || !pawn.IsColonist || GrowthVatsOverclockedMod.GetTrackerFor(pawn) is not { } tracker)
            return;

        if (tracker.RequiresVatBackstory && !(tracker.NormalGrowthPercent > tracker.MostUsedModePercent))
            GrowthVatsOverclockedMod.SetVatBackstoryFor(pawn, tracker.MostUsedMode);

        //remove the tracker now we're done with the backstory. No littering!
        GrowthVatsOverclockedMod.RemoveTrackerFor(pawn);
    }
}

//makes the growth tier gizmo visible for children in growth vats.
[HarmonyPatch(typeof(Gizmo_GrowthTier), "Visible", MethodType.Getter)]
public static class Gizmo_GrowthTier_Visible_HP
{
    public static bool Postfix(bool __result, Pawn ___child) =>
        __result || ___child is { ParentHolder: Building_GrowthVat } and ({ IsColonist: true } or { IsPrisonerOfColony: true } or { IsSlaveOfColony: true });
}

//Patch vat learning hediff to 
[HarmonyPatch(typeof(Hediff_VatLearning), "Learn")]
public static class Hediff_VatLearning_Learn_HP
{
    public static bool Prefix(Hediff_VatLearning __instance)
    {
        if (__instance.TryGetComp<HediffComp_VatLearningModeOverride>() is not { } comp)
            return true; //not our hediff, continue.

        //use our comp instead of default Learn()
        comp.Learn();
        return false;
    }
}
