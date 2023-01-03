// ChoiceLetter_GrowthMoment_HarmonyPatch.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 7:01 PM
// Last edited by: Anthony Chenevier on 2023/01/03 7:01 PM


using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.ThingComps;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment))]
public static class ChoiceLetter_GrowthMoment_HarmonyPatch
{
    //pause vat growth while waiting for growth moment letter to be completed.
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ChoiceLetter_GrowthMoment.ConfigureGrowthLetter))]
    public static void ConfigureGrowthLetter_Postfix(Pawn pawn)
    {
        if (pawn.ParentHolder is Building_GrowthVat growthVat && growthVat.GetComp<CompOverclockedGrowthVat>() is { Enabled: true } comp)
            comp.PausedForLetter = true;
    }

    //unpause vat growth once the growth moment is completed.
    //also check if the pawn is now an adult (teen) and kick them
    //out of play mode. Welcome my son, welcome to the machine.
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ChoiceLetter_GrowthMoment.MakeChoices))]
    public static void MakeChoices_Postfix(ChoiceLetter_GrowthMoment __instance)
    {
        Pawn pawn = __instance.pawn;
        if (pawn.ParentHolder is not Building_GrowthVat growthVat || growthVat.GetComp<CompOverclockedGrowthVat>() is not { Enabled: true } comp)
            return;

        comp.PausedForLetter = false;

        if (!pawn.ageTracker.Adult || comp.Mode != LearningMode.Play)
            return;

        Messages.Message("PlayModeOver13_Message".Translate(pawn.LabelCap), MessageTypeDefOf.RejectInput);
        comp.Mode = LearningMode.Default;
    }
}
