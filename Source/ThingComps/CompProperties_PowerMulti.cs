// CompProperties_VariablePower.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/07 2:12 PM
// Last edited by: Anthony Chenevier on 2022/11/07 2:12 PM


using System.Collections.Generic;
using RimWorld;

namespace EnhancedGrowthVatLearning.ThingComps;

public class CompProperties_PowerMulti : CompProperties_Power
{
    public Dictionary<string, CompProperties_Power> powerProfiles;

    public CompProperties_PowerMulti() { compClass = typeof(CompPowerMulti); }
}
