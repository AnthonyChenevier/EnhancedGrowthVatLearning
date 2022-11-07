// Hediff_EnhancedVatLearning.cs
// 
// Part of EnhancedGrowthVat - EnhancedGrowthVat
// 
// Created by: Anthony Chenevier on 2022/11/03 3:02 PM
// Last edited by: Anthony Chenevier on 2022/11/03 3:02 PM


using System.Linq;
using EnhancedGrowthVat.ThingComps;
using RimWorld;
using Verse;

namespace EnhancedGrowthVat.Hediffs;

public class Hediff_EnhancedVatLearning : Hediff_VatLearning
{
    public override string Description =>
        pawn.ParentHolder is not Building_GrowthVat vat ? base.Description : $"{base.Description}\n\n{vat.GetComp<EnhancedGrowthVatComp>().ModeDisplay}";

    public Hediff_EnhancedVatLearning()
    {
        //set severity from mod settings
        comps.Add(new HediffComp_SeverityPerDay()
        {
            parent = this,
            props = new HediffCompProperties_SeverityPerDay()
            {
                severityPerDay = EnhancedGrowthVatMod.Settings.VatLearningHediffSeverity
            },
        });
    }


    public override void PostTick()
    {
        base.PostTick();
        if (Severity < (double)def.maxSeverity)
            return;

        Learn();
    }

    public new void Learn()
    {
        Severity = def.initialSeverity;
        if (pawn.skills == null || pawn.ParentHolder is not Building_GrowthVat vat)
            return;

        string learningMode = vat.GetComp<EnhancedGrowthVatComp>().Mode.ToString();
        SkillRecord randomSkill = pawn.skills.skills.Where(s => !s.TotallyDisabled)
                                      .RandomElementByWeight(s => EnhancedGrowthVatMod.Settings.SkillsMatrix(learningMode)[s.def.defName]);

        randomSkill.Learn(EnhancedGrowthVatMod.Settings.XPToAward[learningMode], true);
    }
}