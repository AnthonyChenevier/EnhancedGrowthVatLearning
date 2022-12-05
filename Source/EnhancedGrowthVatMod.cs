// EnhancedGrowthVatMod.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 1:59 PM
// Last edited by: Anthony Chenevier on 2022/11/04 1:59 PM


using System.Collections.Generic;
using System.Linq;
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

    public static void SetVatBackstoryFor(Pawn pawn, LearningMode mostUsedMode)
    {
        Pawn_SkillTracker skills = pawn.skills;

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
        if (!Settings.GenerateBackstories)
            return null;


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

    public static void RemoveMod()
    {
        //turn off all growth vat enhanced learning to remove hediffs
        int i = 0;
        foreach (Building_GrowthVat growthVat in from map in Current.Game.Maps from growthVat in map.spawnedThings.OfType<Building_GrowthVat>() select growthVat)
        {
            growthVat.GetComp<EnhancedGrowthVatComp>().Enabled = false;
            i++;
        }

        Log.Message($"EnhancedGrowthVatLearning :: Mod Removal Preparation - Turned off enhanced learning for {i} growth vats over {Current.Game.Maps.Count} maps.");

        //remove all of the mod's backstories from world pawns.
        int j = 0;
        List<Pawn> pawnsToCheck = Find.World.worldPawns.AllPawnsAliveOrDead;
        pawnsToCheck.AddRange(Current.Game.Maps.SelectMany(map => map.mapPawns.AllPawns));
        foreach (Pawn pawn in pawnsToCheck.Where(p => p.RaceProps.Humanlike))
            if (pawn.story.Childhood.spawnCategories != null && pawn.story.Childhood.spawnCategories.Contains("VatGrownEnhanced"))
            {
                pawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamed("VatgrownChild11");
                j++;
            }

        Log.Message($"EnhancedGrowthVatLearning :: Mod Removal Preparation - Changed {j} pawn childhood backstories back to default vatgrown child");

        //remove tracker repo from world components. Should still work for a bit with cache so save and quit is possible
        Find.World.components.Remove(GrowthTrackerRepository);
        Log.Message($"EnhancedGrowthVatLearning :: Mod Removal Preparation - Removed GrowthTrackerRepository from world components so it will not be saved.");
    }
}
