// HarmonyPatcher.cs
// 
// Part of BetterGrowthVatLearning - BetterGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/03 3:08 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 PM


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

//
[HarmonyPatch(typeof(LifeStageWorker_HumanlikeChild), "Notify_LifeStageStarted")]
public static class LifeStageWorker_HumanlikeChild_Notify_LifeStageStarted_HP
{
    public static void Postfix(Pawn pawn)
    {
        if (Current.ProgramState != ProgramState.Playing ||
            !pawn.IsColonist ||
            pawn.ParentHolder is not Building_GrowthVat growthVat ||
            growthVat.GetComp<EnhancedGrowthVatComp>() is not { } vatComp)
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
        if (Current.ProgramState != ProgramState.Playing || !pawn.IsColonist || EnhancedGrowthVatMod.GetTrackerFor(pawn) is not { } tracker)
            return;

        if (tracker.RequiresVatBackstory && !(tracker.NormalGrowthPercent > tracker.MostUsedModePercent))
            EnhancedGrowthVatMod.SetVatBackstoryFor(pawn, tracker.MostUsedMode, pawn.skills.skills.MaxBy(s => s.Level));

        //remove the tracker now we're done with the backstory. No littering!
        EnhancedGrowthVatMod.RemoveTrackerFor(pawn);
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

//pause vat growth while waiting for growth moment letter to be completed.
[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "ConfigureGrowthLetter")]
public static class ChoiceLetter_GrowthMoment_ConfigureGrowthLetter_HP
{
    public static void Postfix(Pawn pawn, ChoiceLetter_GrowthMoment __instance)
    {
        if (pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<EnhancedGrowthVatComp>() is { Enabled: true } comp)
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
        if (__instance.pawn.ParentHolder is not Building_GrowthVat growthVat || growthVat.GetComp<EnhancedGrowthVatComp>() is not { Enabled: true } comp)
            return;

        comp.PausedForLetter = false;
        if (!__instance.pawn.ageTracker.Adult || comp.Mode != LearningMode.Play)
            return;

        Messages.Message($"{__instance.pawn.LabelCap} cannot use the play suite now they are no longer a child.", MessageTypeDefOf.RejectInput);
        comp.Mode = LearningMode.Default;
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
        return growthPointsPerDay * (comp.VatTicks / Find.Storyteller.difficulty.childAgingRate);
    }
}

//override the input to the original function to use our growth factor.
[HarmonyPatch(typeof(Pawn_AgeTracker), "Notify_TickedInGrowthVat")]
public static class Pawn_AgeTracker_Notify_TickedInGrowthVat_HP
{
    public static bool Prefix(ref int ticks, Pawn_AgeTracker __instance, out Pawn __state)
    {
        __state = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        EnhancedGrowthVatComp learningComp = ((Building_GrowthVat)__state.ParentHolder).GetComp<EnhancedGrowthVatComp>();
        if (learningComp == null)
            return true; //should not happen unless I missed a call to this method

        if (learningComp.Enabled)
            //Check for default value used by growth vat so dev gizmo and other direct accessors can bypass
            //NOTE: GetStatValue seems like a hungry beast performance-wise.
            //float statValue = pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed);
            //int defaultTicks = Mathf.FloorToInt(Building_GrowthVat.AgeTicksPerTickInGrowthVat * statValue);
            //if (ticks == defaultTicks)

            //attempt much simpler check. Not as robust, but muuuuuch more performant
            //should only allow the dev gizmo through without modification now. 
            if (ticks < GenDate.TicksPerYear)
            {
                //Stop processing and prevent original tracker from running
                //if aging is paused for a growth moment letter
                if (learningComp.PausedForLetter)
                    return false;

                //use our tick value instead: NOPE
                //no, actually use Aging factor and decompose the modifier from ticks
                //with math so we don't have to touch GetStatValue at all here.
                ticks = Mathf.FloorToInt(learningComp.ModeAgingFactor * ((float)ticks / Building_GrowthVat.AgeTicksPerTickInGrowthVat)); //learningComp.VatTicks;
            }

        //finally, track ticks for backstories and stats ourselves.
        //tracker might be null because of dev tools so check for that
        EnhancedGrowthVatMod.GetTrackerFor(__state)?.TrackGrowthTicks(ticks, learningComp.Enabled, learningComp.Mode);
        return true;
    }

    public static void Postfix(Pawn_AgeTracker __instance, Pawn __state)
    {
        //Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); //for separating mod
        //Insert vat juice hediff ticker. VatLearning is called the same way just after this
        //method is called so this is the best place for it
        Hediff vatjuiceHediff = __state.health.hediffSet.GetFirstHediffOfDef(ModDefOf.VatJuiceEffect);
        vatjuiceHediff?.Tick();
        vatjuiceHediff?.PostTick();
        if (vatjuiceHediff is { ShouldRemove: true })
            __state.health.RemoveHediff(vatjuiceHediff);
    }
}

//Override to get our property instead. Destructive prefix instead of postfix
//to prevent a loop of referencing and re-caching the original VatLearning hediff
[HarmonyPatch(typeof(Building_GrowthVat), "VatLearning", MethodType.Getter)]
public static class Building_GrowthVat_VatLearning_HP
{
    public static bool Prefix(Building_GrowthVat __instance, ref Hediff __result)
    {
        __result = __instance.GetComp<EnhancedGrowthVatComp>().VatLearning;
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
            __instance.GetComp<EnhancedGrowthVatComp>().Refresh();
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
                ((Hediff_VatLearning)__instance.GetComp<EnhancedGrowthVatComp>().VatLearning)?.TryGetComp<HediffComp_VatLearningModeOverride>() is { } comp)
                yield return new Command_Action
                {
                    defaultLabel = $"{command.defaultLabel} Enhanced",
                    action = comp.Learn
                };
            else //send the rest through untouched
                yield return gizmo;
    }
}
