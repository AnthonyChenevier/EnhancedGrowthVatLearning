// HarmonyPatcher.cs
// 
// Part of BetterGrowthVatLearning - BetterGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/03 3:08 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 PM


using System.Collections.Generic;
using System.Reflection;
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

//[HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult), "Notify_LifeStageStarted")]
//public static class LifeStageWorker_HumanlikeAdult_Notify_LifeStageStarted_HP
//{
//    public static void Postfix(Pawn pawn, LifeStageDef previousLifeStage, Pawn_AgeTracker __instance)
//    {
//        if (Current.ProgramState != ProgramState.Playing || !pawn.IsColonist)
//            return;

//        BackstoryDef backstory = pawn.story.Childhood;
//        if (backstory.defName == "VatgrownChild11")
//            //FINISH ME
//    }
//}

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

        if (growthVat.GetComp<EnhancedGrowthVatComp>() is { Enabled: true } comp)
            comp.PausedForLetter = false;
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
        //multiply by the quotient of vat aging factor over storyteller growth speed to get final growth points scaled to vat aging speed
        return growthPointsPerDay * (comp.VatAgingFactor / Find.Storyteller.difficulty.childAgingRate);
    }
}

//override the input to the original function to use our growth factor.
[HarmonyPatch(typeof(Pawn_AgeTracker), "Notify_TickedInGrowthVat")]
public static class Pawn_AgeTracker_Notify_TickedInGrowthVat_HP
{
    public static bool Prefix(ref int ticks, Pawn_AgeTracker __instance, out EnhancedGrowthVatComp __state)
    {
        //store the learning comp for later
        __state = null;
        Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        if (pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<EnhancedGrowthVatComp>() is { } learningComp)
            __state = learningComp;

        //Check for default value used by growth vat
        //so dev gizmo and other direct accessors can bypass 
        //check for new ideo modifier too and add it to ticks
        if (ticks != Mathf.FloorToInt(Building_GrowthVat.AgeTicksPerTickInGrowthVat * pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed)) || __state is null)
            return true;

        switch (__state.Enabled)
        {
            //Prevent original (and postfix) from running if aging is paused for a growth moment letter
            case true when __state.PausedForLetter:
                __state = null;
                ticks = 0;
                return false;
            case true:
                ticks = Mathf.FloorToInt(__state.VatAgingFactor * pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed)); //run our factor and ideo factor
                break;
        }

        return true;
    }

    //also postfix to track growth ticks in our own comp
    //public static void Postfix(int ticks, Pawn_AgeTracker __instance, EnhancedGrowthVatComp __state)
    //{
    //    __state?.GrowthTracker.TrackGrowthTicks(ticks, __instance.CurLifeStageIndex, __state.Mode);
    //}
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
