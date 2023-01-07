// CompVariablePowerTrader.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 2:41 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 AM


using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class CompProperties_PowerMulti : CompProperties_Power
{
    public Dictionary<string, CompProperties_Power> powerProfiles;

    public CompProperties_PowerMulti() { compClass = typeof(CompPowerMulti); }
}

public class CompPowerMulti : ThingComp
{
    private CompProperties_PowerMulti Props => (CompProperties_PowerMulti)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        SetPowerProfile(Props.powerProfiles.Keys.ElementAt(0));
    }

    private void SetPowerProfile(string profileKey) { parent.GetComp<CompPowerTrader>().props = Props.powerProfiles[profileKey]; }

    public bool TrySetPowerProfile(string profileName)
    {
        if (!Props.powerProfiles.ContainsKey(profileName))
            return false;

        SetPowerProfile(profileName);
        return true;
    }

    public static void SetGlobalSetting_PowerConsumption(string profileKey, float powerConsumption)
    {
        IEnumerable<CompProperties_PowerMulti> powerMultis = DefDatabase<ThingDef>.AllDefs.Where(t => t.GetCompProperties<CompProperties_PowerMulti>() is not null)
                                                                                  .Select(t => t.GetCompProperties<CompProperties_PowerMulti>());

        //set private basePowerConsumption field to given value for all multi power profiles
        foreach (CompProperties_PowerMulti multiPowerComp in powerMultis.Where(pm => pm.powerProfiles.ContainsKey(profileKey)))
            typeof(CompProperties_Power).GetField("basePowerConsumption", BindingFlags.NonPublic | BindingFlags.Instance)
                                        ?.SetValue(multiPowerComp.powerProfiles[profileKey], powerConsumption);
    }
}