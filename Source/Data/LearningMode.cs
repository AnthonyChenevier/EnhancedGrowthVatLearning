// LearningMode.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 10:00 PM
// Last edited by: Anthony Chenevier on 2022/11/04 10:00 PM


using System;

namespace EnhancedGrowthVatLearning.Data;

[Flags]
public enum LearningMode
{
    Default = 1 << 0,
    Combat = 1 << 1,
    Labor = 1 << 2,
    Leader = 1 << 3,
    Play = 1 << 4,
}

public static class LearningModeExtensions
{
    public static bool HasMode(this LearningMode value, LearningMode check) { return (value & check) != 0; }
}
