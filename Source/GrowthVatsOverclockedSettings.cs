// EnhancedGrowthVatSettings.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:35 PM
// Last edited by: Anthony Chenevier on 2022/11/04 12:35 PM


using System;
using System.Collections.Generic;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.VatExtensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked;

public class GrowthVatsOverclockedSettings : ModSettings
{
    private enum ModSettingsTab
    {
        MainSettings,
        ModeSettings
    }

    //tabs and scroll view
    private ModSettingsTab _currentTab = ModSettingsTab.MainSettings;
    private float contentHeight = float.MaxValue;
    private Vector2 scrollPosition;

    //input buffers
    private string[] mainSettingBuffers = new string[11];
    private string[] modeSettingBuffers = new string[11];
    private string[] skillBuffers = new string[60];

    //dirty settings buffers and flags
    internal float? initOverclockedPowerConsumption;
    internal bool SettingPowerDirty;
    internal float? initLearningRate;
    internal bool SettingLearningRateDirty;

    public ModSettingsData Data { get; }

    public GrowthVatsOverclockedSettings()
    {
        Data = new ModSettingsData();
        ResetBuffers();
    }

    public override void ExposeData()
    {
        Data.Expose();

        if (Scribe.mode == LoadSaveMode.LoadingVars)
            CheckAndApplyNonDefaultDirtySettings();
    }

    private void ResetBuffers()
    {
        mainSettingBuffers = new string[6];
        modeSettingBuffers = new string[15];
        skillBuffers = new string[60];
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        InitDirtySettingChecks();

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

        CheckDirtySettings();
    }

    private void DoMainSettingsSection(Listing_Standard scrollView)
    {
        int i = 0;
        scrollView.TextFieldNumericLabeledTooltip("learningNeedVariance_SettingsLabel".Translate(),
                                                  ref Data.learningNeedVariance,
                                                  "learningNeedVariance_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  0f,
                                                  1f);

        scrollView.TextFieldNumericLabeledTooltip("learningNeedDailyChangeRate_SettingsLabel".Translate(),
                                                  ref Data.learningNeedDailyChangeRate,
                                                  "learningNeedDailyChangeRate_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  1,
                                                  100000);

        scrollView.GapLine();

        scrollView.CheckboxLabeled("generateBackstories_SettingsLabel".Translate(), ref Data.generateBackstories, "generateBackstories_Tooltip".Translate());
        scrollView.TextFieldNumericLabeledTooltip("vatDaysForBackstory_SettingsLabel".Translate(),
                                                  ref Data.vatDaysForBackstory,
                                                  "vatDaysForBackstory_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  1f,
                                                  780f);

        scrollView.GapLine();

        scrollView.TextFieldNumericLabeledTooltip("overclockedPowerConsumption_SettingsLabel".Translate(),
                                                  ref Data.overclockedPowerConsumption,
                                                  "overclockedPowerConsumption_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i++],
                                                  80f,
                                                  100000f);

        scrollView.GapLine();

        scrollView.TextFieldNumericLabeledTooltip("vatLearningHediffSeverityPerDay_SettingsLabel".Translate(),
                                                  ref Data.learningHediffRate,
                                                  "vatLearningHediffSeverityPerDay_Tooltip".Translate(),
                                                  ref mainSettingBuffers[i],
                                                  0.001f,
                                                  100000f);

        scrollView.GapLine();
    }

    private void DoModeSettingsSection(Listing_Standard scrollView)
    {
        int j = 0;
        int k = 0;
        foreach ((LearningMode mode, ModSettingsData.LearningModeSettings settings) in Data.modeSettings)
        {
            scrollView.Label($"<b>{mode.Label()}</b>");
            scrollView.TextFieldNumericLabeledTooltip("ModeLearningNeed_SettingsLabel".Translate(mode.Label()),
                                                      ref settings.baseLearningNeed,
                                                      "ModeLearningNeed_Tooltip".Translate(mode.Label()),
                                                      ref modeSettingBuffers[j++],
                                                      0.001f,
                                                      1f);

            scrollView.TextFieldNumericLabeledTooltip("ModeVatAgingFactor_SettingsLabel".Translate(mode.Label()),
                                                      ref settings.growthSpeed,
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
        {
            Data.SetDefaults();
            ResetBuffers();
        }
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

    private void InitDirtySettingChecks()
    { //set up dirty setting checks (once per opening setting screen)
        initOverclockedPowerConsumption ??= Data.overclockedPowerConsumption;
        initLearningRate ??= Data.learningHediffRate;
    }

    private void CheckDirtySettings()
    {
        //handle setting power consumption if it's not the initial value
        if (initLearningRate != Data.learningHediffRate)
            SettingLearningRateDirty = true;

        //handle setting power consumption if it's not the initial value
        if (initOverclockedPowerConsumption != Data.overclockedPowerConsumption)
            SettingPowerDirty = true;
    }

    //update all global settings if the setting is not default on load
    private void CheckAndApplyNonDefaultDirtySettings()
    {
        if (Data.overclockedPowerConsumption != 800f)
            CompPowerMulti.SetGlobalSetting_PowerConsumption("Overclocked", Data.overclockedPowerConsumption);

        if (Data.learningHediffRate != 3f)
            HediffComp_VatLearningExtension.SetGlobalSetting_SeverityPerDay(Data.learningHediffRate);
    }

    public void ApplyDirtySettings()
    {
        initOverclockedPowerConsumption = null;
        initLearningRate = null;
        //update all defs with power multi if the setting changed
        if (SettingPowerDirty)
        {
            CompPowerMulti.SetGlobalSetting_PowerConsumption("Overclocked", Data.overclockedPowerConsumption);
            SettingPowerDirty = false;
        }

        if (SettingLearningRateDirty)
        {
            HediffComp_VatLearningExtension.SetGlobalSetting_SeverityPerDay(Data.learningHediffRate);
            SettingLearningRateDirty = false;
        }
    }
}
