// HediffComp_SeverityFromGrowthTier.cs
// 
// Part of EnhancedGrowthVatLearning - EnhancedGrowthVatLearning
// 
// Created by: Anthony Chenevier on 2022/12/24 12:52 PM
// Last edited by: Anthony Chenevier on 2022/12/24 12:52 PM


using Verse;

namespace EnhancedGrowthVatLearning.Hediffs;

public class HediffCompProperties_SeverityFromVatLearning : HediffCompProperties
{
    public float severityPerGrowthTier;
    public HediffCompProperties_SeverityFromVatLearning() { compClass = typeof(HediffComp_SeverityFromVatLearning); }
}

public class HediffComp_SeverityFromVatLearning : HediffComp
{
    private HediffCompProperties_SeverityFromVatLearning Props => (HediffCompProperties_SeverityFromVatLearning)props;

    public void Notify_GrowthMomentPassed(int growthTierAtMoment) { parent.Severity += growthTierAtMoment * Props.severityPerGrowthTier; }
}
