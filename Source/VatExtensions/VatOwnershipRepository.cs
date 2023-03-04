// VatOwnershipExtension.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/03 2:50 PM
// Last edited by: Anthony Chenevier on 2023/03/03 2:50 PM


using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public static class Pawn_OwnershipExtensions
{
    public static Pawn Pawn(this Pawn_Ownership ownership) => (Pawn)typeof(Pawn_Ownership).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ownership);

    public static bool ClaimGrowthVat(this Pawn_Ownership own, Building_GrowthVat newVat) =>
        GrowthVatsOverclockedMod.VatOwnership.ClaimGrowthVat(own.Pawn(), newVat.GetComp<CompOverclockedGrowthVat>());

    public static bool UnclaimGrowthVat(this Pawn_Ownership own) => GrowthVatsOverclockedMod.VatOwnership.UnclaimGrowthVat(own.Pawn());
    public static Building_GrowthVat AssignedGrowthVat(this Pawn_Ownership own) => AssignedGrowthVatOverclockingComp(own)?.parent as Building_GrowthVat;
    public static CompOverclockedGrowthVat AssignedGrowthVatOverclockingComp(this Pawn_Ownership own) => GrowthVatsOverclockedMod.VatOwnership.GetAssignedGrowthVat(own.Pawn());
}

public class VatOwnershipRepository : WorldComponent
{
    public Dictionary<Pawn, CompOverclockedGrowthVat> vatAssignedPawns = new();

    public CompOverclockedGrowthVat GetAssignedGrowthVat(Pawn ownerPawn) => vatAssignedPawns.ContainsKey(ownerPawn) ? vatAssignedPawns[ownerPawn] : null;

    private void SetAssignedGrowthVat(Pawn ownerPawn, CompOverclockedGrowthVat vat)
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

    public bool ClaimGrowthVat(Pawn ownerPawn, CompOverclockedGrowthVat newVat)
    {
        if (newVat.AssignedPawn == ownerPawn)
            return false;

        UnclaimGrowthVat(ownerPawn);
        if (newVat.AssignedPawn != null)
            UnclaimGrowthVat(newVat.AssignedPawn);

        newVat.CompAssignableToPawn.ForceAddPawn(ownerPawn);
        SetAssignedGrowthVat(ownerPawn, newVat);
        return true;
    }

    public bool UnclaimGrowthVat(Pawn ownerPawn)
    {
        if (GetAssignedGrowthVat(ownerPawn) is not { } assignedGrowthVat)
            return false;

        assignedGrowthVat.CompAssignableToPawn.ForceRemovePawn(ownerPawn);
        SetAssignedGrowthVat(ownerPawn, null);
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref vatAssignedPawns, nameof(vatAssignedPawns), LookMode.Reference, LookMode.Reference);
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;

        foreach ((Pawn ownerPawn, CompOverclockedGrowthVat assignedGrowthVat) in vatAssignedPawns)
        {
            CompAssignableToPawn_GrowthVat comp = assignedGrowthVat?.CompAssignableToPawn;
            if (comp == null || comp.AssignedPawns.Contains(ownerPawn))
                continue;

            UnclaimGrowthVat(ownerPawn);
            ClaimGrowthVat(ownerPawn, assignedGrowthVat);
        }
    }
}
