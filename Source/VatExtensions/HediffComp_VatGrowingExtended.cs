// HediffComp_EnhancedVatGrowing.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/04 12:27 AM
// Last edited by: Anthony Chenevier on 2022/11/04 12:27 AM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class HediffCompProperties_VatGrowingExtension : HediffCompProperties
{
    public string descriptionExtraOverclocked;
    public string tipStringExtraPaused;
    public string tipStringExtraGrowthSpeedStat;

    public HediffCompProperties_VatGrowingExtension() { compClass = typeof(HediffComp_VatGrowingExtended); }
}

/// <summary>
/// Extends VatGrowing hediff to display actual growing speed. Replaces original comp in patch
/// </summary>
public class HediffComp_VatGrowingExtended : HediffComp
{
    private HediffCompProperties_VatGrowingExtension Props => (HediffCompProperties_VatGrowingExtension)props;

    private CompOverclockedGrowthVat GrowthVatComp => ((Building_GrowthVat)Pawn.ParentHolder).GetComp<CompOverclockedGrowthVat>();

    public override bool CompShouldRemove => Pawn.Spawned || Pawn.ParentHolder is not Building_GrowthVat;

    public override string CompLabelInBracketsExtra => $"x{(GrowthVatComp.VatgrowthPaused ? 0 : GrowthVatComp.ModeGrowthSpeed)}";

    public override string CompDescriptionExtra => GrowthVatComp.IsOverclocked ? $"{Props.descriptionExtraOverclocked}" : "";

    public override string CompTipStringExtra
    {
        get
        {
            if (GrowthVatComp.VatgrowthPaused)
                return Props.tipStringExtraPaused;

            float growStat = Pawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed);
            return growStat == 1f
                       ? AgeSpeedDescription(GrowthVatComp.ModeGrowthSpeed) //default format display aging speed
                       : $"{AgeSpeedDescription(GrowthVatComp.StatDerivedGrowthSpeed)}\n\t{Props.tipStringExtraGrowthSpeedStat.Translate(growStat.ToStringPercent())}"; //explain growth stat speed increase
        }
    }

    private static string AgeSpeedDescription(float growthSpeed) { return $"{"AgingSpeed".Translate()}: x{growthSpeed}"; }
}