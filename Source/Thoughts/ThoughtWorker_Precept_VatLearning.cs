// ThoughtWorker_Precept_EnhancedVatLearning.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/11/17 10:56 PM
// Last edited by: Anthony Chenevier on 2022/11/17 10:56 PM


using GrowthVatsOverclocked.Data;

namespace GrowthVatsOverclocked.Thoughts;

public class ThoughtWorker_Precept_VatLearning : ThoughtWorker_Precept_EnhancedVat
{
    protected override bool ActiveForMode(LearningMode mode) => mode is LearningMode.Default or LearningMode.Combat or LearningMode.Labor or LearningMode.Leader;
}
