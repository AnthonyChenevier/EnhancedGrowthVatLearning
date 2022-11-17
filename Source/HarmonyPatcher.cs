﻿// HarmonyPatcher.cs
// 
// Part of BetterGrowthVatLearning - BetterGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/03 3:08 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 PM


using System;
using System.Collections.Generic;
using System.Reflection;
using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.Hediffs;
using EnhancedGrowthVatLearning.ThingComps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning;

[StaticConstructorOnStartup]
public static class HarmonyPatcher
{
    static HarmonyPatcher()
    {
        //Harmony.DEBUG = true;
        Harmony harmony = new("makeitso.bettergrowthvatlearning");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

[HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult), "Notify_LifeStageStarted")]
public static class LifeStageWorker_HumanlikeAdult_Notify_LifeStageStarted_HP
{
    public static void Postfix(Pawn pawn)
    {
        if (Current.ProgramState != ProgramState.Playing || !pawn.IsColonist || pawn.GetComp<VatGrowthTrackerComp>() is not { } tracker)
            return;

        if (tracker.RequiresVatBackstory && !(tracker.NormalGrowthPercent > tracker.MostUsedModePercent))
            EnhancedGrowthVatMod.SetVatBackstoryFor(pawn, tracker.MostUsedMode, pawn.skills.skills.MaxBy(s => s.Level));

        //remove the tracker now we're done with the backstory. No littering!
        pawn.AllComps.Remove(tracker);
    }
}

//add the tracker comp to every pawn with vat growth ticks > 0 on init
//so values can be loaded from save file if they exist. We will remove
//any comps that get added here that don't actually have any tracked vat
//time in the comp ExposeData() method
[HarmonyPatch(typeof(ThingWithComps), "InitializeComps")]
public static class ThingWithComps_InitializeComps_HP
{
    public static void Postfix(ThingWithComps __instance)
    {
        if (__instance is not Pawn pawn || !pawn.RaceProps.Humanlike || !(pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony))
            return;
        //agetracker is null at this point so we can't check for this :(
        //if (pawn.ageTracker is not { vatGrowTicks: > 0 })
        //    return;

        ThingComp trackerComp = null;
        try
        {
            trackerComp = (ThingComp)Activator.CreateInstance(typeof(VatGrowthTrackerComp));
            trackerComp.parent = pawn;
            pawn.AllComps.Add(trackerComp);
        }
        catch (Exception ex)
        {
            Log.Error("Could not instantiate or initialize a ThingComp: " + ex);
            pawn.AllComps.Remove(trackerComp);
        }
    }
}

//makes the growth tier gizmo visible for children in growth vats.
[HarmonyPatch(typeof(Gizmo_GrowthTier), "Visible", MethodType.Getter)]
public static class Gizmo_GrowthTier_Visible_HP
{
    public static bool Postfix(bool __result, Gizmo_GrowthTier __instance)
    {
        Pawn child = Traverse.Create(__instance).Field("child").GetValue<Pawn>();
        return __result || (child.ParentHolder is Building_GrowthVat && (child.IsColonist || child.IsPrisonerOfColony || child.IsSlaveOfColony));
    }
}

//Override to pause vat growth while waiting for growth moment letter to be completed.
[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "ConfigureGrowthLetter")]
public static class ChoiceLetter_GrowthMoment_ConfigureGrowthLetter_HP
{
    public static void Postfix(Pawn pawn, ChoiceLetter_GrowthMoment __instance)
    {
        if (pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<EnhancedGrowthVatComp>() is { Enabled: true } comp)
            comp.PausedForLetter = true;
    }
}

//Override to unpause vat growth once the growth moment is completed.
[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "MakeChoices")]
public static class ChoiceLetter_GrowthMoment_MakeChoices_HP
{
    public static void Postfix(ChoiceLetter_GrowthMoment __instance)
    {
        if (__instance.pawn.ParentHolder is not Building_GrowthVat growthVat)
            return;

        if (growthVat.GetComp<EnhancedGrowthVatComp>() is not { Enabled: true } comp)
            return;

        comp.PausedForLetter = false;
        if (!__instance.pawn.ageTracker.Adult || comp.Mode != LearningMode.Play)
            return;

        Messages.Message($"{__instance.pawn.LabelCap} cannot use the play suite now they are no longer a child.", MessageTypeDefOf.RejectInput);
        comp.SetMode(LearningMode.Default);
    }
}

//override to use enhanced growth point rate if enhanced learning is enabled, and to modify
//this rate by child growth stage and storyteller settings so the results are more balanced
//to naturally grown pawns
[HarmonyPatch(typeof(Pawn_AgeTracker), "GrowthPointsPerDay", MethodType.Getter)]
public static class Pawn_AgeTracker_GrowthPointsPerDay_HP
{
    public static float Postfix(float __result, Pawn_AgeTracker __instance)
    {
        Traverse traverse = Traverse.Create(__instance);
        Pawn pawn = traverse.Field("pawn").GetValue<Pawn>();
        if (pawn.ParentHolder is not Building_GrowthVat growthVat || growthVat.GetComp<EnhancedGrowthVatComp>() is not { Enabled: true } comp)
            return __result;

        //get normal growth point value for learning need level, storyteller settings and age.
        float growthPointsPerDay = traverse.Method("GrowthPointsPerDayAtLearningLevel", pawn.needs.learning.CurLevel).GetValue<float>();
        //multiply by the quotient of vat aging factor over storyteller growth speed to get final growth points scaled to vat (and vat grow stat) aging speed
        return growthPointsPerDay * (comp.VatAgingFactorWithStatModifier(pawn) / Find.Storyteller.difficulty.childAgingRate);
    }
}

//override the input to the original function to use our growth factor.
[HarmonyPatch(typeof(Pawn_AgeTracker), "Notify_TickedInGrowthVat")]
public static class Pawn_AgeTracker_Notify_TickedInGrowthVat_HP
{
    public static bool Prefix(ref int ticks, Pawn_AgeTracker __instance)
    {
        Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

        if (pawn.ParentHolder is not Building_GrowthVat growthVat || growthVat.GetComp<EnhancedGrowthVatComp>() is not { } learningComp)
            return true; //should not happen unless I missed a call to this method

        //Check for default value used by growth vat so dev gizmo and other direct accessors can bypass
        int defaultTicks = Mathf.FloorToInt(Building_GrowthVat.AgeTicksPerTickInGrowthVat * pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed));
        if (ticks == defaultTicks && learningComp.Enabled)
        {
            //Prevent original from running if aging is paused for a growth moment letter
            if (learningComp.PausedForLetter)
            {
                ticks = 0;
                return false;
            }

            ticks = learningComp.VatAgingFactorWithStatModifier(pawn); //run our factor and ideo factor
        }

        learningComp.GrowthTracker?.TrackGrowthTicks(ticks, learningComp.Enabled, learningComp.Mode);
        return true;
    }
}

//Override to get our property instead. Destructive prefix to prevent a loop of referencing and re-caching the original VatLearning hediff
[HarmonyPatch(typeof(Building_GrowthVat), "VatLearning", MethodType.Getter)]
public static class Building_GrowthVat_VatLearning_HP
{
    public static bool Prefix(Building_GrowthVat __instance, ref Hediff __result)
    {
        //use our overloaded getter from the comp
        __result = __instance.GetComp<EnhancedGrowthVatComp>().VatLearning;
        //prevent original from running. Sorry other modders
        return false;
    }
}

//Override to turn off enhanced mode on pawn exit
[HarmonyPatch(typeof(Building_GrowthVat), "Notify_PawnRemoved")]
public static class Building_GrowthVat_Notify_PawnRemoved_HP
{
    public static void Postfix(Building_GrowthVat __instance) { __instance.GetComp<EnhancedGrowthVatComp>().SetEnabled(false); }
}

//Override to fix DEV gizmos
[HarmonyPatch(typeof(Building_GrowthVat), "GetGizmos")]
public static class Building_GrowthVat_GetGizmos_HP
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Building_GrowthVat __instance)
    {
        foreach (Gizmo gizmo in gizmos)
            //rebuild the dev:learn gizmo to use the right class
            if (gizmo is Command_Action { defaultLabel: "DEV: Learn" } command && __instance.GetComp<EnhancedGrowthVatComp>().VatLearning is Hediff_EnhancedVatLearning learnBetter)
                yield return new Command_Action
                {
                    defaultLabel = $"{command.defaultLabel} Enhanced",
                    action = learnBetter.EnhancedLearn
                };
            else //send the rest through untouched
                yield return gizmo;
    }
}
