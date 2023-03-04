// CompAssignableToPawn_GrowthVat.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// Adds pawn ownership to Growth Vats. Ownership is used so
// new job types can reference the vat the pawn grew in, and is
// not strictly enforced (does not impact player actions).
// 
// Created by: Anthony Chenevier on 2023/03/02 1:01 PM
// Last edited by: Anthony Chenevier on 2023/03/02 1:01 PM


using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class CompAssignableToPawn_GrowthVat : CompAssignableToPawn
{
    public const float AutoEjectAge = 18f; //copy of Building_GrowthVat.AutoEjectAge

    //don't show assignment gizmo. We'll hook into the vat's pawn
    //selection gizmo for assignment and auto-unassign when they
    //reach AutoEjectAge. Players can always override ownership by
    //inserting a new pawn, which will force the old occupant to look
    //for an unoccupied vat or cancel the job.
    protected override bool ShouldShowAssignmentGizmo() => false;
    private CompOverclockedGrowthVat OverclockingComp => parent.GetComp<CompOverclockedGrowthVat>();
    private Building_GrowthVat Vat => (Building_GrowthVat)parent;

    public override IEnumerable<Pawn> AssigningCandidates =>
        !parent.Spawned ? Enumerable.Empty<Pawn>() : parent.Map.mapPawns.FreeColonists.OrderByDescending(p => CanAssignTo(p).Accepted);

    public override string CompInspectStringExtra() =>
        AssignedPawnsForReading.Count switch
        {
            0 => $"{"RegisteredOccupant".Translate()}: {"Nobody".Translate()}",
            1 => $"{"RegisteredOccupant".Translate()}: {AssignedPawnsForReading[0].Label}",
            _ => ""
        };

    public override bool AssignedAnything(Pawn pawn) => pawn.ownership.AssignedGrowthVat() != null;
    public override void TryAssignPawn(Pawn pawn) => pawn.ownership.ClaimGrowthVat(Vat);
    public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false) => pawn.ownership.UnclaimGrowthVat();

    //can't be assigned to 18+
    public override AcceptanceReport CanAssignTo(Pawn pawn)
    {
        return Vat.CanAcceptPawn(pawn);
        //pawn.ageTracker.AgeBiologicalYearsFloat >= AutoEjectAge ? "TooOld".Translate(pawn.Named("PAWN"), AutoEjectAge.Named("AGEYEARS")) : base.CanAssignTo(pawn);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;

        if (Vat.SelectedPawn is { } pawn && pawn.ownership.AssignedGrowthVat() != Vat)
        {
            pawn.ownership.UnclaimGrowthVat();
            pawn.ownership.ClaimGrowthVat(Vat);
        }
    }
}