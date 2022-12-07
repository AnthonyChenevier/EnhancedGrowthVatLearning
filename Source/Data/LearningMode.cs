// LearningMode.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 10:00 PM
// Last edited by: Anthony Chenevier on 2022/11/04 10:00 PM


using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning.Data;

public enum LearningMode
{
    Default,
    Combat,
    Labor,
    Leader,
    Play,
}

public static class LearningModeExtensions
{
    public static LearningModeSettings Settings(this LearningMode mode) => EnhancedGrowthVatMod.Settings.ModeSettings[mode];
    public static string Label(this LearningMode mode) => $"{mode}Mode".Translate();
    public static Texture2D Icon(this LearningMode mode) => ContentFinder<Texture2D>.Get($"UI/Gizmos/LearningMode{mode}");
    public static string Description(this LearningMode mode) => $"{$"{mode}Mode_Desc".Translate()}{(mode == LearningMode.Play ? "" : $"\n\n{mode.TrainingPriorities()}")}";

    public static string TrainingPriorities(this LearningMode mode)
    {
        return $"{"TrainingPrioritiesList".Translate()}:\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Shooting)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Melee)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Construction)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Mining)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Cooking)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Plants)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Animals)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Crafting)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Artistic)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Medicine)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Social)}\n" +
               $"\t{mode.WeightedSkillColor(SkillDefOf.Intellectual)}";
    }

    public static string WeightedSkillColor(this LearningMode mode, SkillDef skill)
    {
        //use hex colors instead of .Colorize(), hard to get good color scale
        return mode.Settings().skillSelectionWeights[skill.defName] switch
        {
            > 5f and <= 10f => $"<color=#5c7d59>{skill.LabelCap}: +</color>", //muted green
            > 10f and <= 15f => $"<color=#57b94d>{skill.LabelCap}: ++</color>", //midGreen
            >= 20f => $"<color=#18ea03>{skill.LabelCap}: +++</color>", //brightGreen
            _ => $"<color=#434343>{skill.LabelCap}: -</color>", //grey
        };
    }
}
