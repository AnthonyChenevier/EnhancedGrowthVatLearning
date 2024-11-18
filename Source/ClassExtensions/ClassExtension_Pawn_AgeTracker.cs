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
using Verse;

namespace GrowthVatsOverclocked.ClassExtensions;

[HarmonyPatch(typeof(Pawn_AgeTracker))]
public static class ClassExtension_Pawn_AgeTracker
{
    [HarmonyPrefix]
    [HarmonyPatch("BirthdayBiological")]
    public static void BirthdayBiological_Prefix(Pawn ___pawn, int birthdayAge)
    {
        //remove ownership of vat if over 18
        if (birthdayAge >= 18f && ___pawn.ownership.AssignedGrowthVat() != null)
            ___pawn.ownership.UnclaimGrowthVat();
    }

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


    //override to use our growth point formula 
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_AgeTracker.Notify_TickedInGrowthVat))]
    public static bool Notify_TickedInGrowthVat_Prefix(ref int ticks, Pawn ___pawn)
    {
        if (___pawn.ParentHolder is not Building_GrowthVat vat || vat.GetComp<CompOverclockedGrowthVat>() is not { } comp)
            return true;

        //allow dev: aging gizmo ticks and un-overclocked vats through without modification.
        if (comp.IsOverclocked && ticks < GenDate.TicksPerYear)
        {
            //Stop processing and prevent original tracker from running
            //if aging is paused for a growth moment letter
            if (comp.VatgrowthPaused)
                return false;

            //get ticks for the current mode
            ticks = comp.ModeAgeTicks(ticks);
        }

        //track ticks for backstories and stats.
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
