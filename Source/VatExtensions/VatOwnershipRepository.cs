// VatOwnershipExtension.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/03 2:50 PM
// Last edited by: Anthony Chenevier on 2023/03/03 2:50 PM


using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

[HarmonyPatch(typeof(Pawn_Ownership))]
public static class Pawn_Ownership_HarmonyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_Ownership.UnclaimAll))]
    public static void UnclaimAll_Postfix(Pawn_Ownership __instance) { __instance.UnclaimGrowthVat(); }
}

public static class Pawn_OwnershipExtensions
{
    public static Pawn Pawn(this Pawn_Ownership ownership) => (Pawn)typeof(Pawn_Ownership).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ownership);

    public static bool ClaimGrowthVat(this Pawn_Ownership own, Building_GrowthVat newVat) => GrowthVatsOverclockedMod.VatOwnership.ClaimGrowthVat(own.Pawn(), newVat);
    public static bool UnclaimGrowthVat(this Pawn_Ownership own) => GrowthVatsOverclockedMod.VatOwnership.UnclaimGrowthVat(own.Pawn());
    public static Building_GrowthVat AssignedGrowthVat(this Pawn_Ownership own) => GrowthVatsOverclockedMod.VatOwnership.GetAssignedGrowthVat(own.Pawn());
}

public class VatOwnershipRepository : WorldComponent
{
    public Dictionary<Pawn, Building_GrowthVat> vatAssignedPawns = new();

    public Building_GrowthVat GetAssignedGrowthVat(Pawn ownerPawn) => vatAssignedPawns.ContainsKey(ownerPawn) ? vatAssignedPawns[ownerPawn] : null;

    private void SetAssignedGrowthVat(Pawn ownerPawn, Building_GrowthVat vat)
    {
        if (vat == null)
        {
            vatAssignedPawns.Remove(ownerPawn);
            return;
        }

        if (vatAssignedPawns.ContainsKey(ownerPawn))
            vatAssignedPawns[ownerPawn] = vat;
        else
            vatAssignedPawns.Add(ownerPawn, vat);
    }

    public VatOwnershipRepository(World world) : base(world) { }
    public VatOwnershipRepository() : base(Find.World) { }

    public bool ClaimGrowthVat(Pawn ownerPawn, Building_GrowthVat newVat)
    {
        Pawn assignedPawn = newVat.GetAssignedPawn();
        if (assignedPawn == ownerPawn)
            return false;

        UnclaimGrowthVat(ownerPawn);
        if (assignedPawn != null)
            UnclaimGrowthVat(assignedPawn);

        newVat.GetComp<CompAssignableToPawn>().ForceAddPawn(ownerPawn);
        SetAssignedGrowthVat(ownerPawn, newVat);
        return true;
    }

    public bool UnclaimGrowthVat(Pawn ownerPawn)
    {
        if (GetAssignedGrowthVat(ownerPawn) is not { } assignedGrowthVat)
            return false;

        assignedGrowthVat.GetComp<CompAssignableToPawn>().ForceRemovePawn(ownerPawn);
        SetAssignedGrowthVat(ownerPawn, null);
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref vatAssignedPawns, nameof(vatAssignedPawns), LookMode.Reference, LookMode.Reference);
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;

        foreach ((Pawn ownerPawn, Building_GrowthVat assignedGrowthVat) in vatAssignedPawns)
        {
            CompAssignableToPawn comp = assignedGrowthVat?.GetComp<CompAssignableToPawn>();
            if (comp == null || comp.AssignedPawns.Contains(ownerPawn))
                continue;

            UnclaimGrowthVat(ownerPawn);
            ClaimGrowthVat(ownerPawn, assignedGrowthVat);
        }
    }
}
