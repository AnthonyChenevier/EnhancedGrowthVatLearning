﻿// Hediff_TimedLevel.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/26 1:32 AM
// Last edited by: Anthony Chenevier on 2022/11/26 1:32 AM


using System.Linq;
using Verse;

namespace EnhancedGrowthVatLearning.VatJuice;

public class Hediff_LevelWithComps : Hediff_Level
{
    public override string Label => base.Label + " (" + LabelInBrackets + ")";
    public override bool ShouldRemove => comps == null ? base.ShouldRemove : Enumerable.Any(comps, comp => comp.CompShouldRemove) || base.ShouldRemove;
}
