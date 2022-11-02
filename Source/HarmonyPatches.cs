using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterGrowthVatLearning;

[StaticConstructorOnStartup]
public static class BetterGrowthVatLearning
{
    public readonly static HediffDef BetterVatLearningHediff;
    public readonly static ResearchProjectDef BetterGrowthVatResearchProjectDef;

    static BetterGrowthVatLearning()
    {
        BetterVatLearningHediff = HediffDef.Named("BetterVatLearning");
        BetterGrowthVatResearchProjectDef = ResearchProjectDef.Named("BetterGrowthVats");

        //Harmony.DEBUG = true;
        Harmony harmony = new("makeitso.bettergrowthvat");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

[HarmonyPatch(typeof(Building_GrowthVat), "VatLearning", MethodType.Getter)]
public static class HarmonyPatches
{
    //destructive prefix. Done to prevent accidentally referencing and re-creating a cached hediff that we may have removed previously
    public static bool Prefix(Building_GrowthVat __instance, ref Hediff __result)
    {
        if (!BetterGrowthVatLearning.BetterGrowthVatResearchProjectDef.IsFinished)
            return true; //noooooo research, no cryyyyyyy (skip this and use original)

        Pawn_HealthTracker pawnHealthTracker = __instance.SelectedPawn.health;

        //we are taking over, destroy old vat learning hediff if it exists
        if (pawnHealthTracker.hediffSet.HasHediff(HediffDefOf.VatLearning))
            pawnHealthTracker.RemoveHediff(pawnHealthTracker.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatLearning));

        __result = pawnHealthTracker.hediffSet.GetFirstHediffOfDef(BetterGrowthVatLearning.BetterVatLearningHediff) ??
                   pawnHealthTracker.AddHediff(BetterGrowthVatLearning.BetterVatLearningHediff);

        return false; //prevent original from running
    }
}
