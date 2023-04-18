// BackCompatibility_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/01 3:51 PM
// Last edited by: Anthony Chenevier on 2023/01/01 3:51 PM


using System;
using System.Xml;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.Drugs;
using GrowthVatsOverclocked.GrowthTracker;
using GrowthVatsOverclocked.VatExtensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(BackCompatibility))]
public static class ClassExtension_BackCompatibility
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BackCompatibility.BackCompatibleDefName))]
    private static string GetBackCompatibleDefName_HP(string __result, Type defType, string defName)
    {
        if (defType == typeof(ResearchProjectDef) && defName == "EnhancedGrowthVatLearningResearch")
            return "GrowthVatOverclockingResearch";

        if (defType == typeof(HediffDef) && defName == "EnhancedVatLearningHediff")
            return "OverclockedVatLearningHediff";

        if (defType == typeof(HediffDef) && defName == "EnhancedVatGrowingHediff")
            return HediffDefOf.VatGrowing.defName;

        return __result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BackCompatibility.GetBackCompatibleType))]
    private static Type GetBackCompatibleType_HP(Type __result, Type baseType, string providedClassName, XmlNode node)
    {
        //growth tracker
        if (providedClassName is "EnhancedGrowthVatLearning.GrowthTrackerRepository")
            return typeof(GrowthTrackerRepository);

        if (providedClassName is "EnhancedGrowthVatLearning.VatGrowthTracker")
            return typeof(VatGrowthTracker);

        //vat juice ingestion
        if (providedClassName is "EnhancedGrowthVatLearning.VatJuice.IngestionOutcomeDoer_GiveHediff_Level")
            return typeof(IngestionOutcomeDoer_GiveHediff_Level);

        //vat juice base class
        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.Hediff_LevelWithComps" or "EnhancedGrowthVatLearning.VatJuice.Hediff_LevelWithComps")
            return typeof(Hediff_LevelWithComps);

        //growth vat overclocked comp class
        if (providedClassName == "EnhancedGrowthVatLearning.ThingComps.EnhancedGrowthVatComp")
            return typeof(CompOverclockedGrowthVat);

        if (providedClassName == "EnhancedGrowthVatLearning.ThingComps.HediffComp_VatLearningModeOverride")
            return typeof(HediffComp_OverclockedVatLearning);

        return __result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BackCompatibility.PostExposeData))]
    private static void PostExposeData_HP(object obj)
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars)
            return;

        if (obj is CompOverclockedGrowthVat vatComp)
        {
            bool? oldEnabled = new();
            LearningMode? oldMode = new();
            bool? oldPaused = new();

            Scribe_Values.Look(ref oldEnabled, "enabled");
            Scribe_Values.Look(ref oldMode, "mode");
            Scribe_Values.Look(ref oldPaused, "pausedForLetter");

            if (oldEnabled.HasValue)
                vatComp.overclockingEnabled = oldEnabled.Value;

            if (oldMode.HasValue)
                vatComp.currentMode = oldMode.Value;

            if (oldPaused.HasValue)
                vatComp.vatgrowthPaused = oldPaused.Value;

            if (oldPaused.HasValue || oldEnabled.HasValue || oldMode.HasValue)
                Log.Message($"GrowthVatsOverclockedMod :: converted old save values for {vatComp.ToStringSafe()}");
        }
    }
}
