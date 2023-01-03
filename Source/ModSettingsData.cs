// ModSettingsData.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 8:34 PM
// Last edited by: Anthony Chenevier on 2023/01/03 8:34 PM


using System.Collections.Generic;
using GrowthVatsOverclocked.Data;
using Verse;

namespace GrowthVatsOverclocked;

public class ModSettingsData
{
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

    public Dictionary<LearningMode, LearningModeSettings> modeSettings = new()
    {
        { LearningMode.Default, new LearningModeSettings() },
        { LearningMode.Combat, new LearningModeSettings() },
        { LearningMode.Labor, new LearningModeSettings() },
        { LearningMode.Leader, new LearningModeSettings() },
        { LearningMode.Play, new LearningModeSettings() },
    };

    public float learningNeedVariance;
    public int learningNeedDailyChangeRate;
    public float overclockedPowerConsumption;
    public bool generateBackstories;
    public float vatDaysForBackstory;
    public float learningHediffRate;

    public void SetDefaults()
    {
        learningNeedVariance = 0.15f;
        learningNeedDailyChangeRate = 8;

        overclockedPowerConsumption = 800f;

        generateBackstories = true;
        vatDaysForBackstory = 400f;

        learningHediffRate = 3f;

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

    public void Expose()
    {
        Scribe_Collections.Look(ref modeSettings, nameof(modeSettings), LookMode.Value, LookMode.Deep);

        Scribe_Values.Look(ref learningNeedVariance, nameof(learningNeedVariance));
        Scribe_Values.Look(ref learningNeedDailyChangeRate, nameof(learningNeedDailyChangeRate));

        Scribe_Values.Look(ref overclockedPowerConsumption, nameof(overclockedPowerConsumption));

        Scribe_Values.Look(ref generateBackstories, nameof(generateBackstories));
        Scribe_Values.Look(ref vatDaysForBackstory, nameof(vatDaysForBackstory));

        Scribe_Values.Look(ref learningHediffRate, nameof(learningHediffRate));
    }
}
