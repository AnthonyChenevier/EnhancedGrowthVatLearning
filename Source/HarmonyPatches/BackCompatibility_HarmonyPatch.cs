﻿// BackCompatibility_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/01 3:51 PM
// Last edited by: Anthony Chenevier on 2023/01/01 3:51 PM


using System;
using System.Xml;
using GrowthVatsOverclocked.Drugs;
using GrowthVatsOverclocked.GrowthTracker;
using GrowthVatsOverclocked.VatExtensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(BackCompatibility))]
public static class BackCompatibility_HarmonyPatch
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
        //growth tracker
        if (providedClassName is "EnhancedGrowthVatLearning.GrowthTrackerRepository")
            return typeof(GrowthTrackerRepository);

        if (providedClassName is "EnhancedGrowthVatLearning.VatGrowthTracker")
            return typeof(VatGrowthTracker);

        //mod hediffs
        if (providedClassName is "EnhancedGrowthVatLearning.VatJuice.IngestionOutcomeDoer_GiveHediff_Level")
            return typeof(IngestionOutcomeDoer_GiveHediff_Level);

        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.Hediff_LevelWithComps" or "EnhancedGrowthVatLearning.VatJuice.Hediff_LevelWithComps")
            return typeof(Hediff_LevelWithComps);

        //vat growth hediff overrides
        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.EnhancedVatGrowingHediff")
            return typeof(HediffComp_VatGrowing);

        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.HediffComp_EnhancedVatGrowing")
            return typeof(HediffComp_VatGrowingExtended);

        //vat learning hediff overrides
        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.Hediff_EnhancedVatLearning")
            return typeof(Hediff_VatLearning);

        if (providedClassName is "EnhancedGrowthVatLearning.Hediffs.HediffComp_VatLearningModeOverride")
            return typeof(HediffComp_OverclockedVatLearning);

        return __result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BackCompatibility.PostExposeData))]
    private static void PostExposeData_HP(object obj)
    {
        if (Scribe.mode == LoadSaveMode.Saving || VersionControl.CurrentBuild == ScribeMetaHeaderUtility.loadedGameVersionBuild)
            return;
    }
}
