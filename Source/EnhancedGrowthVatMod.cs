// EnhancedGrowthVatMod.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 1:59 PM
// Last edited by: Anthony Chenevier on 2022/11/04 1:59 PM


using EnhancedGrowthVatLearning.Data;
using EnhancedGrowthVatLearning.ThingComps;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace EnhancedGrowthVatLearning;

public class EnhancedGrowthVatMod : Mod
{
    private static GrowthTrackerRepository _growthTrackerRepository;

    private static GrowthTrackerRepository GrowthTrackerRepository
    {
        get
        {
            if (_growthTrackerRepository != null)
                return _growthTrackerRepository;

            World world = Find.World;
            GrowthTrackerRepository repository = world.GetComponent<GrowthTrackerRepository>();
            if (repository == null)
            {
                _growthTrackerRepository = new GrowthTrackerRepository();
                world.components.Add(_growthTrackerRepository);
            }
            else
            {
                _growthTrackerRepository = repository;
            }

            return _growthTrackerRepository;
        }
    }

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
            _ => ModDefOf.VatGrownDefaultBackgroundDef
        };
    }

    public static VatGrowthTracker GetTrackerFor(Pawn pawn)
    {
        int id = pawn.thingIDNumber;

        if (GrowthTrackerRepository.Trackers.ContainsKey(id) && GrowthTrackerRepository.Trackers[id] is { } tracker)
            return tracker;

        //don't add trackers to teens/adults
        if (pawn.ageTracker.CurLifeStageRace.minAge >= GrowthUtility.GrowthMomentAges[GrowthUtility.GrowthMomentAges.Length - 1])
            return null;

        //setup a new growth tracker for our held pawn
        tracker = new VatGrowthTracker(pawn);
        GrowthTrackerRepository.Trackers.Add(id, tracker);
        return tracker;
    }

    public static void RemoveTrackerFor(Pawn pawn)
    {
        if (GrowthTrackerRepository.Trackers.ContainsKey(pawn.thingIDNumber))
            GrowthTrackerRepository.Trackers.Remove(pawn.thingIDNumber);
    }
}
