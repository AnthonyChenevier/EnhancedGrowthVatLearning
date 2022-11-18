// ModDefOf.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/03 3:02 PM
// Last edited by: Anthony Chenevier on 2022/11/03 3:02 PM


using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.Data;

[StaticConstructorOnStartup]
public static class ModDefOf
{
    public readonly static ResearchProjectDef EnhancedGrowthVatResearchProjectDef = ResearchProjectDef.Named("EnhancedGrowthVatLearningResearch");

    public readonly static ResearchProjectDef VatLearningLaborProjectDef = ResearchProjectDef.Named("VatLearningLaborResearch");
    public readonly static ResearchProjectDef VatLearningSoldierProjectDef = ResearchProjectDef.Named("VatLearningSoldierResearch");
    public readonly static ResearchProjectDef VatLearningLeaderProjectDef = ResearchProjectDef.Named("VatLearningLeaderResearch");
    public static ResearchProjectDef VatLearningPlayProjectDef = ResearchProjectDef.Named("VatLearningPlayResearch");

    public readonly static HediffDef EnhancedVatGrowingHediffDef = HediffDef.Named("EnhancedVatGrowingHediff");
    public readonly static HediffDef EnhancedVatLearningHediffDef = HediffDef.Named("EnhancedVatLearningHediff");


    public readonly static StatDef VatGrowthStatDef = StatDef.Named("VatGrowthTime");

    public readonly static BackstoryDef VatGrownDefaultBackgroundDef = DefDatabase<BackstoryDef>.GetNamed("VatgrownChildEGVL");
    public readonly static BackstoryDef VatGrownSoldierBackgroundDef = DefDatabase<BackstoryDef>.GetNamed("VatgrownSoldierEGVL");
    public readonly static BackstoryDef VatGrownLaborerBackgroundDef = DefDatabase<BackstoryDef>.GetNamed("VatgrownLaborerEGVL");
    public readonly static BackstoryDef VatGrownLeaderBackgroundDef = DefDatabase<BackstoryDef>.GetNamed("VatgrownLeaderEGVL");
    public readonly static BackstoryDef VatGrownPlaylandBackgroundDef = DefDatabase<BackstoryDef>.GetNamed("VatgrownPlaylandEGVL");
}