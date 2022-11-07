// HarmonyPatcher.cs
// 
// Part of BetterGrowthVatLearning - BetterGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/03 3:08 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 PM


using System.Collections.Generic;
using System.Reflection;
using EnhancedGrowthVat.Hediffs;
using EnhancedGrowthVat.ThingComps;
using HarmonyLib;
using RimWorld;
using Verse;

namespace EnhancedGrowthVat;

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

//makes the growth tier gizmo visible for all children in growth vats
[HarmonyPatch(typeof(Gizmo_GrowthTier), "Visible", MethodType.Getter)]
public static class Gizmo_GrowthTier_Visible_HP
{
    public static bool Postfix(bool __result, Gizmo_GrowthTier __instance)
    {
        Pawn child = Traverse.Create(__instance).Field("child").GetValue<Pawn>();
        return __result || (child.ParentHolder is Building_GrowthVat && (child.IsColonist || child.IsPrisonerOfColony || child.IsSlaveOfColony));
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
        if (traverse.Field("pawn").GetValue<Pawn>().ParentHolder is not Building_GrowthVat growthVat)
            return __result;

        //override if enabled. Use Pawn_AgeTracker's private GrowthPointsPerDayAtLearningLevel
        //method to adjust result for age and storyteller growth multiplier
        return growthVat.GetComp<EnhancedGrowthVatComp>() is { Enabled: true } comp
                   ? traverse.Method("GrowthPointsPerDayAtLearningLevel", comp.GrowthPointsPerDay).GetValue<float>()
                   : __result;
    }
}

//override the input to the original function to use our growth
//factor. Check for 20 so dev gizmo and other direct accessors still work
[HarmonyPatch(typeof(Pawn_AgeTracker), "Notify_TickedInGrowthVat")]
public static class Pawn_AgeTracker_Notify_TickedInGrowthVat_HP
{
    public static void Prefix(ref int ticks, Pawn_AgeTracker __instance)
    {
        if (ticks == 20 && Traverse.Create(__instance).Field("pawn").GetValue<Pawn>().ParentHolder is Building_GrowthVat growthVat)
            ticks = growthVat.GetComp<EnhancedGrowthVatComp>().GrowthFactor;
    }
}

//Destructive prefix to prevent a loop of referencing and re-caching the original VatLearning hediff
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

//Override to turn off enhanced mode on exit
[HarmonyPatch(typeof(Building_GrowthVat), "Notify_PawnRemoved")]
public static class Building_GrowthVat_Notify_PawnRemoved_HP
{
    public static void Postfix(Building_GrowthVat __instance) { __instance.GetComp<EnhancedGrowthVatComp>().SetEnabled(false); }
}

//Override to fix gizmos
[HarmonyPatch(typeof(Building_GrowthVat), "GetGizmos")]
public static class Building_GrowthVat_GetGizmos_HP
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Building_GrowthVat __instance)
    {
        foreach (Gizmo gizmo in gizmos)
        {
            //rebuild the dev:learn gizmo to use the right class
            if (gizmo is Command_Action { defaultLabel: "DEV: Learn" } command)
            {
                if (__instance.GetComp<EnhancedGrowthVatComp>().VatLearning is not Hediff_EnhancedVatLearning learnBetter)
                    yield return gizmo;
                else
                    yield return new Command_Action
                    {
                        defaultLabel = command.defaultLabel + "Enhanced",
                        action = learnBetter.Learn
                    };
            }

            //send the rest through untouched
            yield return gizmo;
        }
    }
}
