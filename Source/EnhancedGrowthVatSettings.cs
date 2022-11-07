// EnhancedGrowthVatSettings.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:35 PM
// Last edited by: Anthony Chenevier on 2022/11/04 12:35 PM


using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVat;

public class EnhancedGrowthVatSettings : ModSettings
{
    private float vatLearningRate;
    private int vatGrowthFactor;
    private float vatPowerModifier;
    private float vatLearningHediffSeverity;
    private float specializedRateModifier;
    private int leaderGrowthFactorModifier;
    private float growthPointDailyVariance;

    public float VatLearningHediffSeverity => vatLearningHediffSeverity;

    public float VatPowerModifier => vatPowerModifier;

    public float VatLearningRate => vatLearningRate;
    public int VatGrowthFactor => vatGrowthFactor;
    public float SpecializedRateModifier => specializedRateModifier;
    public int LeaderGrowthFactorModifier => leaderGrowthFactorModifier;
    public float GrowthPointDailyVariance => growthPointDailyVariance;


    public Dictionary<string, float> SkillsMatrix(string mode)
    {
        return mode switch
        {
            "Combat" => combatSkills,
            "Labor" => laborSkills,
            "Leader" => leaderSkills,
            _ => defaultSkills
        };
    }

    public Dictionary<string, float> XPToAward;

    //input buffers
    private string lrBuffer;
    private string gfBuffer;
    private string pmBuffer;
    private string lhBuffer;
    private Dictionary<string, float> defaultSkills;
    private Dictionary<string, float> combatSkills;
    private Dictionary<string, float> laborSkills;
    private Dictionary<string, float> leaderSkills;

    public EnhancedGrowthVatSettings() { SetDefaults(); }

    private void SetDefaults()
    {
        vatLearningRate = 3f;
        growthPointDailyVariance = 0.3f;
        specializedRateModifier = 0.75f;
        vatGrowthFactor = 18;
        leaderGrowthFactorModifier = 2;
        vatPowerModifier = 4f;
        vatLearningHediffSeverity = 0.75f;

        defaultSkills = new Dictionary<string, float>
        {
            { "Shooting", 10 },
            { "Melee", 10 },
            { "Medicine", 10 },
            { "Social", 10 },
            { "Animals", 10 },
            { "Cooking", 10 },
            { "Construction", 10 },
            { "Plants", 10 },
            { "Mining", 10 },
            { "Crafting", 10 },
            { "Artistic", 10 },
            { "Intellectual", 10 },
        };

        combatSkills = new Dictionary<string, float>
        {
            { "Shooting", 20 },
            { "Melee", 20 },
            { "Medicine", 15 },
            { "Social", 5 },
            { "Animals", 5 },
            { "Cooking", 5 },
            { "Construction", 15 },
            { "Plants", 5 },
            { "Mining", 15 },
            { "Crafting", 5 },
            { "Artistic", 5 },
            { "Intellectual", 5 },
        };

        laborSkills = new Dictionary<string, float>
        {
            { "Shooting", 5 },
            { "Melee", 5 },
            { "Medicine", 5 },
            { "Social", 5 },
            { "Animals", 15 },
            { "Cooking", 15 },
            { "Construction", 10 },
            { "Plants", 20 },
            { "Mining", 10 },
            { "Crafting", 20 },
            { "Artistic", 5 },
            { "Intellectual", 5 },
        };

        leaderSkills = new Dictionary<string, float>
        {
            { "Shooting", 10 },
            { "Melee", 10 },
            { "Medicine", 15 },
            { "Social", 20 },
            { "Animals", 5 },
            { "Cooking", 5 },
            { "Construction", 5 },
            { "Plants", 5 },
            { "Mining", 5 },
            { "Crafting", 5 },
            { "Artistic", 15 },
            { "Intellectual", 20 },
        };

        XPToAward = new Dictionary<string, float>
        {
            { "Default", 8000f },
            { "Combat", 8000f },
            { "Labor", 8000f },
            { "Leader", 8000f },
        };
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref vatLearningRate, nameof(vatLearningRate));
        Scribe_Values.Look(ref vatGrowthFactor, nameof(vatGrowthFactor));
        Scribe_Values.Look(ref vatPowerModifier, nameof(vatPowerModifier));
        Scribe_Values.Look(ref vatLearningHediffSeverity, nameof(vatLearningHediffSeverity));
        Scribe_Values.Look(ref growthPointDailyVariance, nameof(growthPointDailyVariance));

        Scribe_Values.Look(ref specializedRateModifier, nameof(specializedRateModifier));
        Scribe_Values.Look(ref leaderGrowthFactorModifier, nameof(leaderGrowthFactorModifier));

        Scribe_Collections.Look(ref defaultSkills, nameof(defaultSkills), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref combatSkills, nameof(combatSkills), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref laborSkills, nameof(laborSkills), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref leaderSkills, nameof(leaderSkills), LookMode.Value, LookMode.Value);

        Scribe_Collections.Look(ref XPToAward, nameof(XPToAward), LookMode.Value, LookMode.Value);
    }


    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();
        listing.Begin(inRect);
        Rect lrRect = listing.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(lrRect, "VatLearningRate_SettingsLabel".Translate(), ref vatLearningRate, ref lrBuffer, 2f, 100f);
        TooltipHandler.TipRegion(lrRect, "VatLearningRate_Tooltip".Translate());

        Rect gfRect = listing.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(gfRect, "VatGrowthFactor_SettingsLabel".Translate(), ref vatGrowthFactor, ref gfBuffer, 1, 20);
        TooltipHandler.TipRegion(gfRect, "VatGrowthFactor_Tooltip".Translate());

        Rect pmRect = listing.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(pmRect, "VatPowerModifier_SettingsLabel".Translate(), ref vatPowerModifier, ref pmBuffer, 1f, 20f);
        TooltipHandler.TipRegion(pmRect, "VatPowerModifier_Tooltip".Translate());

        Rect lhRect = listing.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(lhRect, "VatLearningHediffSeverity_SettingsLabel".Translate(), ref vatLearningHediffSeverity, ref lhBuffer, 0.001f, 10f);
        TooltipHandler.TipRegion(lhRect, "VatLearningHediffSeverity_Tooltip".Translate());


        listing.End();
    }
}
