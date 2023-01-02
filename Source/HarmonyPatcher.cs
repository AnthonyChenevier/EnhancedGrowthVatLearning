// HarmonyPatcher.cs
// 
// Part of BetterGrowthVatLearning - BetterGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/03 3:08 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 PM


using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.Hediffs;
using GrowthVatsOverclocked.ThingComps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked;

[StaticConstructorOnStartup]
public static class HarmonyPatcher
{
    static HarmonyPatcher()
    {
        //Harmony.DEBUG = true;
        Harmony harmony = new("makeitso.growthvatsoverclocked");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        //add our back compatibility converter to the (private) list here too.
        List<BackCompatibilityConverter> chain =
            (List<BackCompatibilityConverter>)typeof(BackCompatibility).GetField("conversionChain", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);

        chain?.Add(new BackCompatibilityConverter_EGVL_GVO());
    }
}

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
    public static bool Postfix(bool __result, Gizmo_GrowthTier __instance)
    {
        return __result ||
               (Pawn)typeof(Gizmo_GrowthTier).GetField("child", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance) is { ParentHolder: Building_GrowthVat }
               and ({ IsColonist: true } or { IsPrisonerOfColony: true } or { IsSlaveOfColony: true });
    }
}

//pause vat growth while waiting for growth moment letter to be completed.
[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "ConfigureGrowthLetter")]
public static class ChoiceLetter_GrowthMoment_ConfigureGrowthLetter_HP
{
    public static void Postfix(Pawn pawn, ChoiceLetter_GrowthMoment __instance)
    {
        if (pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<CompOverclockedGrowthVat>() is { Enabled: true } comp)
            comp.PausedForLetter = true;
    }
}

//unpause vat growth once the growth moment is completed.
//also check if the pawn is now an adult (teen) and kick them
//out of play mode. Welcome my son, welcome to the machine.
[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "MakeChoices")]
public static class ChoiceLetter_GrowthMoment_MakeChoices_HP
{
    public static void Postfix(ChoiceLetter_GrowthMoment __instance)
    {
        Pawn pawn = __instance.pawn;
        if (pawn.ParentHolder is not Building_GrowthVat growthVat || growthVat.GetComp<CompOverclockedGrowthVat>() is not { Enabled: true } comp)
            return;

        comp.PausedForLetter = false;

        if (!pawn.ageTracker.Adult || comp.Mode != LearningMode.Play)
            return;

        Messages.Message("PlayModeOver13_Message".Translate(pawn.LabelCap), MessageTypeDefOf.RejectInput);
        comp.Mode = LearningMode.Default;
    }
}

//override to use enhanced growth point rate if enhanced learning is enabled, and to modify
//this rate by child growth stage and storyteller settings so the results are more balanced
//to naturally grown pawns
[HarmonyPatch(typeof(Pawn_AgeTracker), "GrowthPointsPerDay", MethodType.Getter)]
public static class Pawn_AgeTracker_GrowthPointsPerDay_HP
{
    private static Pawn Pawn_AgeTracker_Pawn(Pawn_AgeTracker ageTracker) =>
        (Pawn)typeof(Pawn_AgeTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ageTracker);

    private static float Pawn_AgeTracker_GrowthPointsPerDayAtLearningLevel(Pawn_AgeTracker ageTracker, float level) =>
        (float)(typeof(Pawn_AgeTracker).GetMethod("GrowthPointsPerDayAtLearningLevel", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod)
                                       ?.Invoke(ageTracker, new object[] { level }) ??
                0f);

    public static void Postfix(ref float __result, Pawn_AgeTracker __instance)
    {
        if (Pawn_AgeTracker_Pawn(__instance) is not { ParentHolder: Building_GrowthVat growthVat } pawn ||
            growthVat.GetComp<CompOverclockedGrowthVat>() is not { Enabled: true } comp)
            return;

        //get normal growth point value for learning need level, storyteller settings and age.
        //multiply by the quotient of vat aging factor over storyteller growth speed to get final growth points scaled to vat (and vat grow stat) aging speed
        __result = Pawn_AgeTracker_GrowthPointsPerDayAtLearningLevel(__instance, (pawn.needs.learning?.CurLevel ?? 0f) * comp.DailyGrowthPointFactor);
    }
}

//override the input to the original function to use our growth factor.
[HarmonyPatch(typeof(Pawn_AgeTracker), "Notify_TickedInGrowthVat")]
public static class Pawn_AgeTracker_Notify_TickedInGrowthVat_HP
{
    private static Pawn Pawn_AgeTracker_Pawn(Pawn_AgeTracker ageTracker) =>
        (Pawn)typeof(Pawn_AgeTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ageTracker);

    public static bool Prefix(ref int ticks, Pawn_AgeTracker __instance)
    {
        Pawn pawn = Pawn_AgeTracker_Pawn(__instance);
        CompOverclockedGrowthVat learningComp = ((Building_GrowthVat)pawn?.ParentHolder)?.GetComp<CompOverclockedGrowthVat>();
        if (learningComp == null)
            return true; //should not happen unless I missed a call to this method

        //attempt simple check. Not as robust as using GetStatValue, but muuuuuch more performant
        //should only allow the dev gizmo through without modification now. 
        if (learningComp.Enabled && ticks < GenDate.TicksPerYear)
        {
            //Stop processing and prevent original tracker from running
            //if aging is paused for a growth moment letter
            if (learningComp.PausedForLetter)
                return false;

            //use Aging factor and decompose the modifier from ticks
            //with math so we don't have to touch GetStatValue at all here.
            ticks = Mathf.FloorToInt(learningComp.ModeAgingFactor * ((float)ticks / Building_GrowthVat.AgeTicksPerTickInGrowthVat));
        }

        //finally, track ticks for backstories and stats ourselves.
        //tracker might be null because of dev tools so check for that
        GrowthVatsOverclockedMod.GetTrackerFor(pawn)?.TrackGrowthTicks(ticks, learningComp.Enabled, learningComp.Mode);
        return true;
    }


    //Tick any hediffs that have our extension
    public static void Postfix(Pawn_AgeTracker __instance)
    {
        Pawn pawn = Pawn_AgeTracker_Pawn(__instance);

        //tick all hediffs with TickInGrowthVatComp
        List<Hediff> vatHediffs = pawn.health.hediffSet.hediffs.Where(h => h.TryGetComp<HediffComp_TickInGrowthVat>() is { tickInVat: true }).ToList();
        foreach (Hediff hediff in vatHediffs)
        {
            hediff.Tick();
            hediff.PostTick();
            if (hediff is { ShouldRemove: true })
                pawn.health.RemoveHediff(hediff);
        }
    }
}

//Override to get our property instead. Destructive prefix instead of postfix
//to prevent a loop of referencing and re-caching the original VatLearning hediff
[HarmonyPatch(typeof(Building_GrowthVat), "VatLearning", MethodType.Getter)]
public static class Building_GrowthVat_VatLearning_HP
{
    public static bool Prefix(Building_GrowthVat __instance, ref Hediff __result)
    {
        __result = __instance.GetComp<CompOverclockedGrowthVat>().VatLearning;
        return false;
    }
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

//Override to refresh enhanced mode on pawn entry (fixes extra Vat Learning hediff if kids enter when not babies)
[HarmonyPatch(typeof(Building_GrowthVat), "TryAcceptPawn")]
public static class Building_GrowthVat_TryAcceptPawn_HP
{
    public static void Postfix(Pawn pawn, Building_GrowthVat __instance)
    {
        if (pawn.ParentHolder == __instance)
            __instance.GetComp<CompOverclockedGrowthVat>().Refresh();
    }
}

//Override to fix DEV: Learn gizmo
[HarmonyPatch(typeof(Building_GrowthVat), "GetGizmos")]
public static class Building_GrowthVat_GetGizmos_HP
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Building_GrowthVat __instance)
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
