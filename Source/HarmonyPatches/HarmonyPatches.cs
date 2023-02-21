// HarmonyPatches.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 6:47 PM
// Last edited by: Anthony Chenevier on 2023/01/03 6:47 PM


using System.Collections.Generic;
using System.Reflection;
using GrowthVatsOverclocked.Data;
using GrowthVatsOverclocked.VatExtensions;
using GrowthVatsOverclocked.Vatshock;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[StaticConstructorOnStartup]
public static class PatchInitializer
{
    static PatchInitializer()
    {
        //Harmony.DEBUG = true;
        Harmony harmony = new("makeitso.growthvatsoverclocked");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

//Overclocked learning patches

//Patch vat learning hediff to use our comp instead
[HarmonyPatch(typeof(Hediff_VatLearning), "Learn")]
public static class Hediff_VatLearning_Learn_HP
{
    public static bool Learn_Prefix(Hediff_VatLearning __instance)
    {
        if (__instance.TryGetComp<HediffComp_OverclockedVatLearning>() is { } comp)
        {
            //use our comp instead of default Learn()
            comp.Learn();
            return false;
        }

        return true; //not our hediff, continue.
    }
}

//refresh vatcomp to apply hediffs when babies age up
[HarmonyPatch(typeof(LifeStageWorker_HumanlikeChild), nameof(LifeStageWorker_HumanlikeChild.Notify_LifeStageStarted))]
public static class LifeStageWorker_HumanlikeChild_Notify_LifeStageStarted_HP
{
    public static void Notify_LifeStageStarted_Postfix(Pawn pawn)
    {
        if (Current.ProgramState != ProgramState.Playing ||
            !pawn.IsColonist ||
            pawn.ParentHolder is not Building_GrowthVat growthVat ||
            growthVat.GetComp<CompOverclockedGrowthVat>() is not { } vatComp)
            return;

        //refresh
        vatComp.EnableOverclocking(vatComp.IsOverclocked);
    }
}

//set vat backstory on aging to adult
[HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult), nameof(LifeStageWorker_HumanlikeAdult.Notify_LifeStageStarted))]
public static class LifeStageWorker_HumanlikeAdult_Notify_LifeStageStarted_HP
{
    public static void Notify_LifeStageStarted_Postfix(Pawn pawn)
    {
        if (Current.ProgramState != ProgramState.Playing || !pawn.IsColonist || GrowthVatsOverclockedMod.GetTrackerFor(pawn) is not { } tracker)
            return;

        if (tracker.RequiresVatBackstory && !(tracker.NormalGrowthPercent > tracker.MostUsedModePercent))
            GrowthVatsOverclockedMod.SetVatBackstoryFor(pawn, tracker.MostUsedMode);

        //remove the tracker now we're done with the backstory. No littering!
        GrowthVatsOverclockedMod.RemoveTrackerFor(pawn);
    }
}

//makes the growth tier gizmo visible for all children in growth vats.
[HarmonyPatch(typeof(Gizmo_GrowthTier), "Visible", MethodType.Getter)]
public static class Gizmo_GrowthTier_Visible_HP
{
    [HarmonyPostfix]
    public static bool Visible_Postfix(bool __result, Pawn ___child) =>
        __result || ___child is { ParentHolder: Building_GrowthVat } and ({ IsColonist: true } or { IsPrisonerOfColony: true } or { IsSlaveOfColony: true });
}

//vat-juice hooks

//GoJuice Genes also remove chance of getting vat-juice pain effect
[HarmonyPatch(typeof(GeneDefGenerator), nameof(GeneDefGenerator.ImpliedGeneDefs))]
public static class GeneDefGenerator_ImpliedGeneDefs_HP
{
    public static IEnumerable<GeneDef> ImpliedGeneDefs_Postfix(IEnumerable<GeneDef> __result)
    {
        foreach (GeneDef geneDef in __result)
        {
            if (geneDef.defName is "ChemicalDependency_GoJuice" or "AddictionResistant_GoJuice" or "AddictionImmune_GoJuice")
                geneDef.makeImmuneTo.Add(GVODefOf.VatJuicePain);

            yield return geneDef;
        }
    }
}

//Vat exposure and vatshock hooks

//add check for vat exposure and notify it if learning complete
[HarmonyPatch(typeof(LearningUtility))]
public static class LearningUtility_HP
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(LearningUtility.LearningTickCheckEnd))]
    public static void LearningTickCheckEnd_Postfix(Pawn pawn, bool __result)
    {
        if (!__result) //quick exit if learning not complete
            return;

        if (pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatgrowthExposureHediff)?.TryGetComp<HediffComp_SeverityFromChildhoodEvent>() is { } eventComp)
            eventComp.Notify_LearningEvent();
    }
}

//similarly check for exposure 
[HarmonyPatch(typeof(Pawn_InteractionsTracker))]
public static class Pawn_InteractionsTracker_HP
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_InteractionsTracker.TryInteractWith))]
    public static void TryInteractWith_Postfix(bool __result, Pawn recipient, Pawn ___pawn, InteractionDef intDef)
    {
        if (!__result) //exit quickly if interaction failed or violent
            return;

        //try notifying both pawns if either have exposure hediff
        if (___pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatgrowthExposureHediff)?.TryGetComp<HediffComp_SeverityFromChildhoodEvent>() is { } interactorExposureComp)
            interactorExposureComp.Notify_SocialEvent(recipient);

        if (recipient.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatgrowthExposureHediff)?.TryGetComp<HediffComp_SeverityFromChildhoodEvent>() is { } recipientExposureComp)
            recipientExposureComp.Notify_SocialEvent(recipient);
    }
}

//check if a vatshock memory was successfully counseled and notify the thoughtWorker about it.
[HarmonyPatch(typeof(CompAbilityEffect_Counsel))]
public static class CompAbilityEffect_Counsel_HP
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CompAbilityEffect_Counsel.Apply))]
    public static void Apply_Postfix(LocalTargetInfo target)
    {
        Pawn pawn = target.Pawn;
        MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;
        if (memories.GetFirstMemoryOfDef(ThoughtDefOf.Counselled) is { } counselledThought &&
            memories.GetFirstMemoryOfDef(GVODefOf.VatshockFlashback) is { } vatshockThought &&
            counselledThought.MoodOffset() == -vatshockThought.MoodOffset() &&
            pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatshockHediff)?.TryGetComp<HediffComp_Vatshock>() is { } vatshockComp)
            vatshockComp.Notify_RecoveryAction();
    }
}

//check for inspiration use 
[HarmonyPatch(typeof(InspirationHandler))]
public static class InspirationHandler_HP
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(InspirationHandler.EndInspiration), typeof(InspirationDef))]
    public static void EndInspiration_Prefix(InspirationHandler __instance, InspirationDef inspirationDef)
    {
        //try notifying vatshock hediff
        if (__instance.CurStateDef == inspirationDef &&
            __instance.pawn.health.hediffSet.GetFirstHediffOfDef(GVODefOf.VatshockHediff)?.TryGetComp<HediffComp_Vatshock>() is { } vatshock)
            vatshock.Notify_RecoveryAction();
    }
}