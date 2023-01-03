// ModBackCompatibilityConverter.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/01 3:51 PM
// Last edited by: Anthony Chenevier on 2023/01/01 3:51 PM


using System;
using System.Xml;
using GrowthVatsOverclocked.Hediffs;
using HarmonyLib;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(BackCompatibility))]
public static class ModBackCompatibilityPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BackCompatibility.BackCompatibleDefName))]
    private static string GetBackCompatibleDefName_HP(string __result, Type defType, string defName)
    {
        if (defType == typeof(ResearchProjectDef) && defName == "EnhancedGrowthVatLearningResearch")
                return "GrowthVatOverclockingResearch";

        return __result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BackCompatibility.GetBackCompatibleType))]
    private static Type GetBackCompatibleType_HP(Type __result, Type baseType, string providedClassName, XmlNode node)
    {
        if (providedClassName is "EnhancedGrowthVatLearning.GrowthTrackerRepository")
            return typeof(GrowthTrackerRepository);

        if (providedClassName is "EnhancedGrowthVatLearning.VatJuice.IngestionOutcomeDoer_GiveHediff_Level")
            return typeof(IngestionOutcomeDoer_GiveHediff_Level);

        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.Hediff_LevelWithComps" or "EnhancedGrowthVatLearning.VatJuice.Hediff_LevelWithComps")
            return typeof(Hediff_LevelWithComps);

        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.HediffComp_EnhancedVatGrowing")
            return typeof(HediffComp_EnhancedVatGrowing);

        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.Hediff_EnhancedVatLearning")
            return typeof(Hediff_VatLearning);

        return __result;
    }
}
