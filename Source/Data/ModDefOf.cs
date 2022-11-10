// ModDefOf.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/03 3:02 PM
// Last edited by: Anthony Chenevier on 2022/11/03 3:02 PM


using Verse;

namespace EnhancedGrowthVat.Data;

[StaticConstructorOnStartup]
public static class ModDefOf
{
    public readonly static ResearchProjectDef EnhancedGrowthVatResearchProjectDef = ResearchProjectDef.Named("EnhancedGrowthVatResearch");

    public readonly static ResearchProjectDef VatLearningLaborProjectDef = ResearchProjectDef.Named("VatLearningLaborResearch");
    public readonly static ResearchProjectDef VatLearningSoldierProjectDef = ResearchProjectDef.Named("VatLearningSoldierResearch");
    public readonly static ResearchProjectDef VatLearningLeaderProjectDef = ResearchProjectDef.Named("VatLearningLeaderResearch");

    public readonly static HediffDef EnhancedVatGrowingHediffDef = HediffDef.Named("EnhancedVatGrowingHediff");
    public readonly static HediffDef EnhancedVatLearningHediffDef = HediffDef.Named("EnhancedVatLearningHediff");

    //TODO: implement backstories
    //public readonly static HediffDef VatGrownSoldierBackgroundDef = HediffDef.Named("VatgrownSoldierColonist");
    //public readonly static HediffDef VatGrownLaborerBackgroundDef = HediffDef.Named("VatgrownLaborerColonist");
    //public readonly static HediffDef VatGrownLeaderBackgroundDef = HediffDef.Named("VatgrownOfficerColonist");
}