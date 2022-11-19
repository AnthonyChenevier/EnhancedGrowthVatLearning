// EnhancedGrowthVatMod.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 1:59 PM
// Last edited by: Anthony Chenevier on 2022/11/04 1:59 PM


using System;
using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning;

public class EnhancedGrowthVatMod : Mod
{
    public override string SettingsCategory() { return "EnhancedGrowthVatSettings".Translate(); }
    public static EnhancedGrowthVatSettings Settings { get; private set; }

    public EnhancedGrowthVatMod(ModContentPack content) : base(content) { Settings = GetSettings<EnhancedGrowthVatSettings>(); }

    public override void DoSettingsWindowContents(Rect inRect) { Settings.DoSettingsWindowContents(inRect); }

    public override void WriteSettings()
    {
        base.WriteSettings();

        Settings.initPowerConsumptionValue = null;
        //update all defs with power multi if the setting changed
        if (!Settings.SettingPowerDirty)
            return;

        CompPowerMulti.ModifyPowerProfiles("EnhancedLearning", Settings.EnhancedModePowerConsumption);
        Settings.SettingPowerDirty = false;
    }

    public static void SetVatBackstoryFor(Pawn pawn, LearningMode mostUsedMode, SkillRecord highestSkill)
    {
        pawn.story.Childhood = mostUsedMode switch
        {
            LearningMode.Combat => ModDefOf.VatGrownSoldierBackgroundDef,
            LearningMode.Labor => ModDefOf.VatGrownLaborerBackgroundDef,
            LearningMode.Leader => ModDefOf.VatGrownLeaderBackgroundDef,
            LearningMode.Play => ModDefOf.VatGrownPlaylandBackgroundDef,
            //LearningMode.Default => ModDefOf.VatGrownEnhancedBackgroundDef,
            _ => ModDefOf.VatGrownDefaultBackgroundDef
        };
    }

    public static void AddTrackerTo(Pawn pawn)
    {
        ThingComp trackerComp = null;
        try
        {
            trackerComp = (ThingComp)Activator.CreateInstance(typeof(VatGrowthTrackerComp));
            trackerComp.parent = pawn;
            pawn.AllComps.Add(trackerComp);
        }
        catch (Exception ex)
        {
            Log.Error("Could not instantiate or initialize VatGrowthTrackerComp: " + ex);
            pawn.AllComps.Remove(trackerComp);
        }
    }
}
