// EnhancedGrowthVatSettings.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:35 PM
// Last edited by: Anthony Chenevier on 2022/11/04 12:35 PM


using System.Collections.Generic;
using System.Linq;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning;

public class EnhancedGrowthVatSettings : ModSettings
{
    private float defaultModeLearningNeed;
    private float specializedModesLearningNeed;
    private float leaderModeLearningNeed;
    private float playModeLearningNeed;

    private float learningNeedVariance;
    private int learningNeedDailyChangeRate;

    private int vatAgingFactor;
    private int leaderAgingFactorModifier;

    private float enhancedModePowerConsumption;

    private float vatDaysForBackstory;

    private float vatLearningHediffSeverityPerDay;
    public Dictionary<string, float> xpToAward;

    private Dictionary<string, float> defaultSkills;
    private Dictionary<string, float> combatSkills;
    private Dictionary<string, float> laborSkills;
    private Dictionary<string, float> leaderSkills;


    public float DefaultModeLearningNeed => defaultModeLearningNeed;
    public float SpecializedModesLearningNeed => specializedModesLearningNeed;
    public float LeaderModeLearningNeed => leaderModeLearningNeed;

    public float PlayModeLearningNeed => playModeLearningNeed;

    public float LearningNeedVariance => learningNeedVariance;
    public int LearningNeedDailyChangeRate => learningNeedDailyChangeRate;

    public int VatAgingFactor => vatAgingFactor;
    public int LeaderAgingFactorModifier => leaderAgingFactorModifier;

    public float EnhancedModePowerConsumption => enhancedModePowerConsumption;

    public float VatDaysForBackstory => vatDaysForBackstory;

    public float VatLearningHediffSeverityPerDay => vatLearningHediffSeverityPerDay;
    public Dictionary<string, float> XpToAward => xpToAward;


    //Settings page stuff
    private float contentHeight = float.MaxValue;
    private Vector2 scrollPosition;
    private string[] settingBuffers = new string[11];
    private string[] skillXPBuffers = new string[5];
    private string[] defaultSkillBuffers = new string[12];
    private string[] laborSkillBuffers = new string[12];
    private string[] combatSkillBuffers = new string[12];
    private string[] leaderSkillBuffers = new string[12];
    internal float? initPowerConsumptionValue;
    internal bool SettingPowerDirty;


    public EnhancedGrowthVatSettings() { SetDefaults(); }

    private void SetDefaults()
    {
        settingBuffers = new string[11];
        skillXPBuffers = new string[5];
        defaultSkillBuffers = new string[12];
        laborSkillBuffers = new string[12];
        combatSkillBuffers = new string[12];
        leaderSkillBuffers = new string[12];

        defaultModeLearningNeed = 0.7f;
        specializedModesLearningNeed = 0.6f;
        leaderModeLearningNeed = 0.85f;
        playModeLearningNeed = 0.98f;

        learningNeedVariance = 0.15f;
        learningNeedDailyChangeRate = 8;

        vatAgingFactor = 18;
        leaderAgingFactorModifier = 2;

        enhancedModePowerConsumption = 800f;

        vatDaysForBackstory = 400f;

        vatLearningHediffSeverityPerDay = 3f;

        xpToAward = new Dictionary<string, float>
        {
            { "Default", 2000f },
            { "Combat", 2000f },
            { "Labor", 2000f },
            { "Leader", 2200f },
            { "Play", 200f }
        };

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
            { "Intellectual", 40 }, //double because its only available to train after 13 years of age and we want a chance to get it.
        };
    }

    public Dictionary<string, float> SkillsMatrix(string mode)
    {
        return mode switch
        {
            "Play" => defaultSkills,
            "Combat" => combatSkills,
            "Labor" => laborSkills,
            "Leader" => leaderSkills,
            _ => defaultSkills
        };
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref defaultModeLearningNeed, nameof(defaultModeLearningNeed));
        Scribe_Values.Look(ref specializedModesLearningNeed, nameof(specializedModesLearningNeed));
        Scribe_Values.Look(ref leaderModeLearningNeed, nameof(leaderModeLearningNeed));
        Scribe_Values.Look(ref playModeLearningNeed, nameof(playModeLearningNeed));

        Scribe_Values.Look(ref learningNeedVariance, nameof(learningNeedVariance));
        Scribe_Values.Look(ref learningNeedDailyChangeRate, nameof(learningNeedDailyChangeRate));

        Scribe_Values.Look(ref vatAgingFactor, nameof(vatAgingFactor));
        Scribe_Values.Look(ref leaderAgingFactorModifier, nameof(leaderAgingFactorModifier));

        Scribe_Values.Look(ref enhancedModePowerConsumption, nameof(enhancedModePowerConsumption));

        Scribe_Values.Look(ref vatDaysForBackstory, nameof(vatDaysForBackstory));

        Scribe_Values.Look(ref vatLearningHediffSeverityPerDay, nameof(vatLearningHediffSeverityPerDay));
        Scribe_Collections.Look(ref xpToAward, nameof(xpToAward), LookMode.Value, LookMode.Value);

        Scribe_Collections.Look(ref defaultSkills, nameof(defaultSkills), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref combatSkills, nameof(combatSkills), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref laborSkills, nameof(laborSkills), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref leaderSkills, nameof(leaderSkills), LookMode.Value, LookMode.Value);

        //update all defs with power multi comp if setting is not default on load
        if (Scribe.mode == LoadSaveMode.LoadingVars && enhancedModePowerConsumption != 800f)
            CompPowerMulti.ModifyPowerProfiles("EnhancedLearning", enhancedModePowerConsumption);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        //set initial power consumption for dirty setting check if it's unset (once per opening setting screen)
        initPowerConsumptionValue ??= enhancedModePowerConsumption;

        Rect resetButtonRect = new(inRect)
        {
            xMin = inRect.xMax - 200f,
            xMax = inRect.xMax - 30f,
            yMin = inRect.yMin - 40f, //go up above inrect
            height = 30f
        };

        if (Widgets.ButtonText(resetButtonRect, "ResetToDefaults_Button".Translate()))
            SetDefaults();

        Listing_Standard listing = new();
        listing.Begin(inRect);
        Rect contentRect = listing.GetRect(inRect.height - 12f);
        Listing_Standard scrollView = listing.BeginScrollView(contentRect, contentHeight, ref scrollPosition);

        int i = 0;
        DoSetting(scrollView, nameof(defaultModeLearningNeed), ref defaultModeLearningNeed, ref settingBuffers[i++], 0.001f, 1f);
        DoSetting(scrollView, nameof(specializedModesLearningNeed), ref specializedModesLearningNeed, ref settingBuffers[i++], 0.001f, 1f);
        DoSetting(scrollView, nameof(leaderModeLearningNeed), ref leaderModeLearningNeed, ref settingBuffers[i++], 0.001f, 1f);
        DoSetting(scrollView, nameof(playModeLearningNeed), ref playModeLearningNeed, ref settingBuffers[i++], 0.001f, 1f);
        scrollView.Gap();

        DoSetting(scrollView, nameof(learningNeedVariance), ref learningNeedVariance, ref settingBuffers[i++], 0f, 1f);
        DoSetting(scrollView, nameof(learningNeedDailyChangeRate), ref learningNeedDailyChangeRate, ref settingBuffers[i++], 1, 100000);
        scrollView.Gap();

        DoSetting(scrollView, nameof(vatAgingFactor), ref vatAgingFactor, ref settingBuffers[i++], 1, 100000);
        DoSetting(scrollView, nameof(leaderAgingFactorModifier), ref leaderAgingFactorModifier, ref settingBuffers[i++], 0, 1000000);
        scrollView.Gap();
        DoSetting(scrollView, nameof(vatDaysForBackstory), ref vatDaysForBackstory, ref settingBuffers[i++], 1f, 780f);
        scrollView.Gap();
        DoSetting(scrollView, nameof(enhancedModePowerConsumption), ref enhancedModePowerConsumption, ref settingBuffers[i++], 80f, 100000f);
        //handle setting power consumption if it's not the initial value
        if (enhancedModePowerConsumption != initPowerConsumptionValue)
            SettingPowerDirty = true;


        scrollView.Gap();

        DoSetting(scrollView, nameof(vatLearningHediffSeverityPerDay), ref vatLearningHediffSeverityPerDay, ref settingBuffers[i], 0.001f, 100000f);
        i = 0;
        scrollView.Label($"{$"{nameof(xpToAward)}_SettingsLabel".Translate()} ", -1f, $"{nameof(xpToAward)}_Tooltip".Translate());
        List<string> keys = xpToAward.Keys.ToList();
        foreach (string key in keys)
        {
            float xpForMode = xpToAward[key];
            scrollView.TextFieldNumericLabeled($"{$"{key}XP_SettingsLabel".Translate()} ", ref xpForMode, ref skillXPBuffers[i++], 0f, 100000f);
            xpToAward[key] = xpForMode;
        }

        scrollView.Gap();


        i = 0;
        scrollView.Label($"{$"{nameof(defaultSkills)}_SettingsLabel".Translate()} ", -1f, "skillsMatrix_Tooltip".Translate());
        foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs)
        {
            float setting = defaultSkills[skill.defName];
            scrollView.TextFieldNumericLabeled($"{skill.LabelCap} ", ref setting, ref defaultSkillBuffers[i++], 5, 50);
            defaultSkills[skill.defName] = setting;
        }

        scrollView.Gap();

        scrollView.Label($"{$"{nameof(laborSkills)}_SettingsLabel".Translate()} ", -1f, "skillsMatrix_Tooltip".Translate());
        i = 0;
        foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs)
        {
            float setting = laborSkills[skill.defName];
            scrollView.TextFieldNumericLabeled($"{skill.LabelCap} ", ref setting, ref laborSkillBuffers[i++], 5, 50);
            laborSkills[skill.defName] = setting;
        }

        scrollView.Gap();

        scrollView.Label($"{$"{nameof(combatSkills)}_SettingsLabel".Translate()} ", -1f, "skillsMatrix_Tooltip".Translate());
        i = 0;
        foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs)
        {
            float setting = combatSkills[skill.defName];
            scrollView.TextFieldNumericLabeled($"{skill.LabelCap} ", ref setting, ref combatSkillBuffers[i++], 5, 50);
            combatSkills[skill.defName] = setting;
        }

        scrollView.Gap();

        scrollView.Label($"{$"{nameof(leaderSkills)}_SettingsLabel".Translate()} ", -1f, "skillsMatrix_Tooltip".Translate());
        i = 0;
        foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs)
        {
            float setting = leaderSkills[skill.defName];
            scrollView.TextFieldNumericLabeled($"{skill.LabelCap} ", ref setting, ref leaderSkillBuffers[i++], 5, 50);
            leaderSkills[skill.defName] = setting;
        }

        scrollView.Gap();


        listing.EndScrollView(scrollView);
        listing.End();
        if (contentHeight != scrollView.CurHeight)
            contentHeight = scrollView.CurHeight;
    }

    private void DoSetting(Listing_Standard listing, string settingName, ref int setting, ref string inputBuffer, int min, int max)
    {
        Rect rect = listing.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(rect, $"{$"{settingName}_SettingsLabel".Translate()} ", ref setting, ref inputBuffer, min, max);
        TooltipHandler.TipRegion(rect, $"{settingName}_Tooltip".Translate());
    }

    private void DoSetting(Listing_Standard listing, string settingName, ref float setting, ref string inputBuffer, float min, float max)
    {
        Rect rect = listing.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(rect, $"{$"{settingName}_SettingsLabel".Translate()} ", ref setting, ref inputBuffer, min, max);
        TooltipHandler.TipRegion(rect, $"{settingName}_Tooltip".Translate());
    }
}
