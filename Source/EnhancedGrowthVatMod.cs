// EnhancedGrowthVatMod.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 1:59 PM
// Last edited by: Anthony Chenevier on 2022/11/04 1:59 PM


using UnityEngine;
using Verse;

namespace EnhancedGrowthVat;

public class EnhancedGrowthVatMod : Mod
{
    public override string SettingsCategory() { return "EnhancedGrowthVatSettings".Translate(); }
    public static EnhancedGrowthVatSettings Settings { get; private set; }

    public EnhancedGrowthVatMod(ModContentPack content) : base(content) { Settings = GetSettings<EnhancedGrowthVatSettings>(); }

    public override void DoSettingsWindowContents(Rect inRect) { Settings.DoSettingsWindowContents(inRect); }
}
