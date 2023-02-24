// Pawn_AgeTracker_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/02/21 5:47 PM
// Last edited by: Anthony Chenevier on 2023/02/24 1:03 PM


using System.Linq;
using System.Reflection;
using GrowthVatsOverclocked.VatExtensions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_AgeTracker))]
public static class Pawn_AgeTracker_HarmonyPatch
{
    //override to use enhanced growth point rate if enhanced learning is enabled, and to modify
    //this rate by child growth stage and storyteller settings so the results are more balanced
    //to naturally grown pawns
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_AgeTracker.GrowthPointsPerDay), MethodType.Getter)]
    public static float GrowthPointsPerDay_Postfix(float __result, Pawn_AgeTracker __instance, Pawn ___pawn)
    {
        if (___pawn.ParentHolder is not Building_GrowthVat growthVat ||
            ___pawn.needs.learning is not { } learning ||
            growthVat.GetComp<CompOverclockedGrowthVat>() is not { IsOverclocked: true } comp ||
            typeof(Pawn_AgeTracker).GetMethod("GrowthPointsPerDayAtLearningLevel", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod) is not
                { } methodInfo)
            return __result;

        //if overclocking is enabled, get normal growth point value for learning need level and scale it by the daily growth point factor (comp growth speed/child aging rate).
        //Run it through the GrowthPointsPerDayAtLearningLevel method to scale result for storyteller settings and age like natural aged kids' need.
        return (float)methodInfo.Invoke(__instance, new object[] { learning.CurLevel * (comp.StatDerivedGrowthSpeed / Find.Storyteller.difficulty.childAgingRate) });
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_AgeTracker.Notify_TickedInGrowthVat))]
    public static bool Notify_TickedInGrowthVat_Prefix(ref int ticks, Pawn ___pawn)
    {
        if (___pawn.ParentHolder is not Building_GrowthVat vat || vat.GetComp<CompOverclockedGrowthVat>() is not { } comp)
            return true;

        //attempt simple check. Not as robust as using GetStatValue, but muuuuuch more performant
        //should only allow the dev gizmo through without modification now. 
        if (comp.IsOverclocked && ticks < GenDate.TicksPerYear)
        {
            //Stop processing and prevent original tracker from running
            //if aging is paused for a growth moment letter
            if (comp.VatgrowthPaused)
                return false;

            //use mode growth speed and decompose the modifier from ticks
            //with math so we don't have to touch GetStatValue at all here.
            ticks = Mathf.FloorToInt(comp.ModeGrowthSpeed * ((float)ticks / Building_GrowthVat.AgeTicksPerTickInGrowthVat));
        }

        //finally, track ticks for backstories and stats ourselves.
        //tracker might be null because of dev tools so check for that
        GrowthVatsOverclockedMod.GetTrackerFor(___pawn)?.TrackGrowthTicks(ticks, comp.IsOverclocked, comp.CurrentMode);
        return true;
    }


    //Tick any hediffs that have our extension
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_AgeTracker.Notify_TickedInGrowthVat))]
    public static void Notify_TickedInGrowthVat_Postfix(Pawn ___pawn)
    {
        //tick all hediffs with TickInGrowthVatModExtension
        foreach (Hediff hediff in ___pawn.health.hediffSet.hediffs.Where(h => h.def.GetModExtension<DefModExtension_HediffTickInGrowthVat>() is { tickInVat: true }))
        {
            hediff.Tick();
            hediff.PostTick();
            if (hediff is { ShouldRemove: true })
                ___pawn.health.RemoveHediff(hediff);
        }
    }
}
