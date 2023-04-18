// CompAssignableToPawn_GrowthVat.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// Adds pawn ownership to Growth Vats. Ownership is used so
// new job types can reference the vat the pawn grew in, and is
// not strictly enforced (does not impact player actions).
// 
// Created by: Anthony Chenevier on 2023/03/02 1:01 PM
// Last edited by: Anthony Chenevier on 2023/03/02 1:01 PM


using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.VatExtensions;

public class CompAssignableToPawn_GrowthVat : CompAssignableToPawn
{
    private const float MaxVatAge = 18f;
    private Building_GrowthVat Vat => (Building_GrowthVat)parent;

    //CompAssignableToPawn property overrides
    public override IEnumerable<Pawn> AssigningCandidates =>
        !parent.Spawned ? Enumerable.Empty<Pawn>() : parent.Map.mapPawns.FreeColonists.OrderByDescending(p => CanAssignTo(p).Accepted);

    public Pawn AssignedPawn => AssignedPawns.FirstOrDefault();


    //CompAssignableToPawn method overrides
    protected override bool ShouldShowAssignmentGizmo() => false; //unused
    public override AcceptanceReport CanAssignTo(Pawn pawn)
    {
      if (Vat.selectedEmbryo != null)
        return "EmbryoSelected".Translate();

      if (pawn.ageTracker.AgeBiologicalYearsFloat >= MaxVatAge)
        return "TooOld".Translate(pawn.Named("PAWN"), MaxVatAge.Named("AGEYEARS"));
      
      return pawn.IsColonist && !pawn.IsQuestLodger();
    }

    public override bool AssignedAnything(Pawn pawn) => pawn.ownership.AssignedGrowthVat() != null;
    public override void TryAssignPawn(Pawn pawn) => pawn.ownership.ClaimGrowthVat(Vat);
    public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false) => pawn.ownership.UnclaimGrowthVat();

    //ThingComp method overrides
    public override string CompInspectStringExtra()
    {
        if (AssignedPawns.FirstOrDefault() is { } pawn)
            return $"{"RegisteredOccupant".Translate().CapitalizeFirst()}: {pawn.Label}";

        return $"{"RegisteredOccupant".Translate().CapitalizeFirst()}: {"Nobody".Translate()}";
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;

        //claim any unassigned but occupied vats on init
        if (Vat.SelectedPawn is { } pawn && pawn.ownership.AssignedGrowthVat() != Vat)
            pawn.ownership.ClaimGrowthVat(Vat);
    }
}