// ThoughtWorker_Precept_EnhancedVatPlaying.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/11/17 10:57 PM
// Last edited by: Anthony Chenevier on 2022/11/17 10:57 PM


using EnhancedGrowthVatLearning.Data;

namespace EnhancedGrowthVatLearning.Thoughts;

public class ThoughtWorker_Precept_VatPlaying : ThoughtWorker_Precept_EnhancedVat
{
    protected override bool ActiveForMode(LearningMode mode) => mode is LearningMode.Play;
}
