// ModDefOf.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/03 3:02 PM
// Last edited by: Anthony Chenevier on 2022/11/03 3:02 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Data;

[DefOf]
public static class GVODefOf
{
    //researches
    public static ResearchProjectDef GrowthVatOverclockingResearch;

    public static ResearchProjectDef VatLearningLaborResearch;
    public static ResearchProjectDef VatLearningSoldierResearch;
    public static ResearchProjectDef VatLearningLeaderResearch;
    public static ResearchProjectDef VatLearningPlayResearch;

    //backstories
    public static BackstoryDef VatgrownChildEGVL;
    public static BackstoryDef VatgrownSoldierEGVL;
    public static BackstoryDef VatgrownLaborerEGVL;
    public static BackstoryDef VatgrownLeaderEGVL;
    public static BackstoryDef VatgrownPlaylandEGVL;

    public static HediffDef VatgrowthExposureHediff;
    public static HediffDef VatshockHediff;
    public static ThoughtDef VatshockFlashback;


    public static HediffDef VatJuiceEffect;
    public static HediffDef VatJuicePain;

    //other


    public static StatDef VatGrowthTime;

    static GVODefOf() { DefOfHelper.EnsureInitializedInCtor(typeof(GVODefOf)); }
}