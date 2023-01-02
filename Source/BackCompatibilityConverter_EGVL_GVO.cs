// ModBackCompatibilityConverter.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/01 3:51 PM
// Last edited by: Anthony Chenevier on 2023/01/01 3:51 PM


using System;
using System.Xml;
using GrowthVatsOverclocked.Hediffs;
using Verse;

namespace GrowthVatsOverclocked;

//[HotSwappable]
public class BackCompatibilityConverter_EGVL_GVO : BackCompatibilityConverter
{
    public override bool AppliesToVersion(int majorVer, int minorVer) => true;

    public override string BackCompatibleDefName(
        Type defType, 
        string defName, 
        bool forDefInjections = false, 
        XmlNode node = null)
    {
        if (defType == typeof(ResearchProjectDef) && defName == "EnhancedGrowthVatLearningResearch")
                return "GrowthVatOverclockingResearch";

        return null;
    }

    public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
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

        return null;
    }

    public override void PostExposeData(object obj)
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars)
            return;
    }
}
