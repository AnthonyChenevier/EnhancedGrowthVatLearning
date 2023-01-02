// EnhancedGrowthVatSettings.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:35 PM
// Last edited by: Anthony Chenevier on 2022/11/04 12:35 PM


using System;
using System.Collections.Generic;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.Hediffs;
using GrowthVatsOverclocked.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked;

public class LearningModeSettings : IExposable
{
    public float baseLearningNeed;
    public int baseAgingFactor;
    public float skillXP;
    public Dictionary<string, float> skillSelectionWeights;

    public void ExposeData()
    {
        Scribe_Values.Look(ref baseLearningNeed, nameof(baseLearningNeed));
        Scribe_Values.Look(ref baseAgingFactor, nameof(baseAgingFactor));
        Scribe_Values.Look(ref skillXP, nameof(skillXP));
        Scribe_Collections.Look(ref skillSelectionWeights, nameof(skillSelectionWeights), LookMode.Value, LookMode.Value);
    }
}

public class GrowthVatsOverclockedSettings : ModSettings
{
    private Dictionary<LearningMode, LearningModeSettings> modeSettings = new()
    {
        { LearningMode.Default, new LearningModeSettings() },
        { LearningMode.Combat, new LearningModeSettings() },
        { LearningMode.Labor, new LearningModeSettings() },
        { LearningMode.Leader, new LearningModeSettings() },
        { LearningMode.Play, new LearningModeSettings() },
    };

    private float learningNeedVariance;
    private int learningNeedDailyChangeRate;

    private float overclockedPowerConsumption;

    private bool generateBackstories;
    private float vatDaysForBackstory;

    private float vatLearningHediffSeverityPerDay;


    public Dictionary<LearningMode, LearningModeSettings> ModeSettings => modeSettings;

    public float LearningNeedVariance => learningNeedVariance;
    public int LearningNeedDailyChangeRate => learningNeedDailyChangeRate;

    public float OverclockedPowerConsumption => overclockedPowerConsumption;


    public bool GenerateBackstories => generateBackstories;
    public float VatDaysForBackstory => vatDaysForBackstory;

    public float VatLearningHediffSeverityPerDay => vatLearningHediffSeverityPerDay;


    //Settings page stuff
    private ModSettingsTab _currentTab = ModSettingsTab.MainSettings;
    private float contentHeight = float.MaxValue;
    private Vector2 scrollPosition;

    private string[] mainSettingBuffers = new string[11];
    private string[] modeSettingBuffers = new string[11];
    private string[] skillBuffers = new string[60];

    internal float? initPowerConsumptionValue;
    internal bool SettingPowerDirty;
    internal float? initEnhancedLearningHediffValue;
    internal bool SettingLearningHediffDirty;

    private enum ModSettingsTab
    {
        MainSettings,
        ModeSettings
    }


    public GrowthVatsOverclockedSettings() { SetDefaults(); }

    private void SetDefaults()
    {
        mainSettingBuffers = new string[6];
        modeSettingBuffers = new string[15];
        skillBuffers = new string[60];

        learningNeedVariance = 0.15f;
        learningNeedDailyChangeRate = 8;

        overclockedPowerConsumption = 800f;

        generateBackstories = true;
        vatDaysForBackstory = 400f;

        vatLearningHediffSeverityPerDay = 3f;


        modeSettings[LearningMode.Default].baseLearningNeed = 0.7f;
        modeSettings[LearningMode.Default].baseAgingFactor = 18;
        modeSettings[LearningMode.Default].skillXP = 2000f;
        modeSettings[LearningMode.Default].skillSelectionWeights = new Dictionary<string, float>
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

        modeSettings[LearningMode.Labor].baseLearningNeed = 0.6f;
        modeSettings[LearningMode.Labor].baseAgingFactor = 18;
        modeSettings[LearningMode.Labor].skillXP = 2000f;
        modeSettings[LearningMode.Labor].skillSelectionWeights = new Dictionary<string, float>
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

        modeSettings[LearningMode.Combat].baseLearningNeed = 0.6f;
        modeSettings[LearningMode.Combat].baseAgingFactor = 18;
        modeSettings[LearningMode.Combat].skillXP = 2000f;
        modeSettings[LearningMode.Combat].skillSelectionWeights = new Dictionary<string, float>
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

        modeSettings[LearningMode.Leader].baseLearningNeed = 0.85f;
        modeSettings[LearningMode.Leader].baseAgingFactor = 16;
        modeSettings[LearningMode.Leader].skillXP = 2200f;
        modeSettings[LearningMode.Leader].skillSelectionWeights = new Dictionary<string, float>
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

        modeSettings[LearningMode.Play].baseLearningNeed = 0.98f;
        modeSettings[LearningMode.Play].baseAgingFactor = 18;
        modeSettings[LearningMode.Play].skillXP = 800f;
        modeSettings[LearningMode.Play].skillSelectionWeights = new Dictionary<string, float>
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
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref modeSettings, nameof(modeSettings), LookMode.Value, LookMode.Deep);

        Scribe_Values.Look(ref learningNeedVariance, nameof(learningNeedVariance));
        Scribe_Values.Look(ref learningNeedDailyChangeRate, nameof(learningNeedDailyChangeRate));

        Scribe_Values.Look(ref overclockedPowerConsumption, nameof(overclockedPowerConsumption));

        Scribe_Values.Look(ref generateBackstories, nameof(generateBackstories));
        Scribe_Values.Look(ref vatDaysForBackstory, nameof(vatDaysForBackstory));

        Scribe_Values.Look(ref vatLearningHediffSeverityPerDay, nameof(vatLearningHediffSeverityPerDay));

        //update all defs with power multi comp if setting is not default on load
        if (Scribe.mode == LoadSaveMode.LoadingVars && overclockedPowerConsumption != 800f)
            CompPowerMulti.ModifyPowerProfiles("Overclocked", overclockedPowerConsumption);

        //update all defs with power multi comp if setting is not default on load
        if (Scribe.mode == LoadSaveMode.LoadingVars && vatLearningHediffSeverityPerDay != 3f)
            HediffComp_VatLearningModeOverride.ModifySeverityPerDay(vatLearningHediffSeverityPerDay);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        //set up dirty setting checks (once per opening setting screen)
        initPowerConsumptionValue ??= overclockedPowerConsumption;
        initEnhancedLearningHediffValue ??= vatLearningHediffSeverityPerDay;

        DrawResetButton(inRect);
        DrawRemoveModButton(inRect);

        Listing_Tabbed list = new();

        list.Begin(inRect);
        Listing_Standard tabList = list.BeginTabSection(new List<TabRecord>
        {
            new("MainSettings_TabLabel".Translate(), () => _currentTab = ModSettingsTab.MainSettings, () => _currentTab == ModSettingsTab.MainSettings),
            new("ModeSettings_TabLabel".Translate(), () => _currentTab = ModSettingsTab.ModeSettings, () => _currentTab == ModSettingsTab.ModeSettings),
        });

        Rect contentRect = tabList.GetRect(inRect.ContractedBy(4f).height - TabDrawer.TabHeight);
        Listing_Standard scrollView = tabList.BeginScrollView(contentRect, contentHeight, ref scrollPosition);
        switch (_currentTab)
        {
            case ModSettingsTab.MainSettings:
                DoMainSettingsSection(scrollView);
                break;
            case ModSettingsTab.ModeSettings:
                DoModeSettingsSection(scrollView);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        tabList.EndScrollView(scrollView);
        list.EndTabSection(tabList);
        list.End();
        if (contentHeight != scrollView.CurHeight)
            contentHeight = scrollView.CurHeight;
    }

    private void DoMainSettingsSection(Listing_Standard scrollView)
    {
        int i = 0;
        scrollView.TextFieldNumericLabeledTooltip("learningNeedVariance_SettingsLabel".Translate(),
                                                  ref learningNeedVariance,
                                                  "learningNeedVariance_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  0f,
                                                  1f);

        scrollView.TextFieldNumericLabeledTooltip("learningNeedDailyChangeRate_SettingsLabel".Translate(),
                                                  ref learningNeedDailyChangeRate,
                                                  "learningNeedDailyChangeRate_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  1,
                                                  100000);

        scrollView.GapLine();

        scrollView.CheckboxLabeled("generateBackstories_SettingsLabel".Translate(), ref generateBackstories, "generateBackstories_Tooltip".Translate());
        scrollView.TextFieldNumericLabeledTooltip("vatDaysForBackstory_SettingsLabel".Translate(),
                                                  ref vatDaysForBackstory,
                                                  "vatDaysForBackstory_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  1f,
                                                  780f);

        scrollView.GapLine();

        scrollView.TextFieldNumericLabeledTooltip("overclockedPowerConsumption_SettingsLabel".Translate(),
                                                  ref overclockedPowerConsumption,
                                                  "overclockedPowerConsumption_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  80f,
                                                  100000f);

        //handle setting power consumption if it's not the initial value
        if (overclockedPowerConsumption != initPowerConsumptionValue)
            SettingPowerDirty = true;

        scrollView.GapLine();

        scrollView.TextFieldNumericLabeledTooltip("vatLearningHediffSeverityPerDay_SettingsLabel".Translate(),
                                                  ref vatLearningHediffSeverityPerDay,
                                                  "vatLearningHediffSeverityPerDay_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i],
                                                  0.001f,
                                                  100000f);

        //handle setting power consumption if it's not the initial value
        if (vatLearningHediffSeverityPerDay != initEnhancedLearningHediffValue)
            SettingLearningHediffDirty = true;


        scrollView.GapLine();
    }

    private void DoModeSettingsSection(Listing_Standard scrollView)
    {
        int j = 0;
        int k = 0;
        foreach ((LearningMode mode, LearningModeSettings settings) in modeSettings)
        {
            scrollView.TextFieldNumericLabeledTooltip("ModeLearningNeed_SettingsLabel".Translate(mode.Label()),
                                                      ref settings.baseLearningNeed,
                                                      "ModeLearningNeed_Tooltip".Translate(mode.Label()),
                                                      ref modeSettingBuffers[j++],
                                                      0.001f,
                                                      1f);

            scrollView.TextFieldNumericLabeledTooltip("ModeVatAgingFactor_SettingsLabel".Translate(mode.Label()),
                                                      ref settings.baseAgingFactor,
                                                      "ModeVatAgingFactor_Tooltip".Translate(mode.Label()),
                                                      ref modeSettingBuffers[j++],
                                                      1,
                                                      100000);


            scrollView.TextFieldNumericLabeledTooltip("ModeSkillXP_SettingsLabel".Translate(mode.Label()),
                                                      ref settings.skillXP,
                                                      "ModeSkillXP_Tooltip".Translate(),
                                                      ref modeSettingBuffers[j++],
                                                      0f,
                                                      100000f);

            scrollView.Gap();
            scrollView.Label("ModeSkillWeights_SettingsLabel".Translate(mode.Label()), -1f, "ModeSkillWeights_Tooltip".Translate(mode.Label()));
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs)
            {
                float setting = settings.skillSelectionWeights[skill.defName];
                scrollView.TextFieldNumericLabeled($"{skill.LabelCap} ", ref setting, ref skillBuffers[k++], 5, 50);
                settings.skillSelectionWeights[skill.defName] = setting;
            }

            scrollView.GapLine();
        }
    }

    private void DrawResetButton(Rect inRect)
    {
        Rect buttonRect = new(inRect)
        {
            xMin = inRect.xMax - 200f,
            xMax = inRect.xMax - 30f,
            yMin = inRect.yMin - 40f, //go up above inrect
            height = 30f
        };

        TooltipHandler.TipRegion(buttonRect, "ResetToDefaults_Tooltip".Translate());
        if (Widgets.ButtonText(buttonRect, "ResetToDefaults_Button".Translate()))
            SetDefaults();
    }

    private void DrawRemoveModButton(Rect inRect)
    {
        Rect buttonRect = new(inRect)
        {
            xMin = inRect.xMax - 380f,
            xMax = inRect.xMax - 210f,
            yMin = inRect.yMin - 40f, //go up above inrect
            height = 30f
        };

        //only active if in-game
        TooltipHandler.TipRegion(buttonRect, "RemoveMod_Tooltip".Translate());
        if (Widgets.ButtonText(buttonRect, "RemoveMod_Button".Translate(), active: Current.Game?.World != null))
            Find.WindowStack.Add(new Dialog_MessageBox("AreYouSureModRemoval_Dialog".Translate().Colorize(ColorLibrary.RedReadable),
                                                       buttonAText: "OK".Translate(),
                                                       buttonAAction: GrowthVatsOverclockedMod.RemoveMod,
                                                       buttonBText: "Cancel".Translate()));
    }
}
