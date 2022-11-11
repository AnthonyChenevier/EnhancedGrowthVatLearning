// CompVariablePowerTrader.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 2:41 AM
// Last edited by: Anthony Chenevier on 2022/11/04 2:41 AM


using System.Linq;
using RimWorld;
using Verse;

namespace EnhancedGrowthVatLearning.ThingComps;

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
}