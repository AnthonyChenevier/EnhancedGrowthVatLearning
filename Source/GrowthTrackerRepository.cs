// GrowthTrackerRepository.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/11/22 9:50 AM
// Last edited by: Anthony Chenevier on 2022/11/22 9:50 AM


using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GrowthVatsOverclocked;

public class GrowthTrackerRepository : WorldComponent
{
    private readonly int _maxTicksForCleanup = GenDate.TicksPerYear * GrowthUtility.GrowthMomentAges[GrowthUtility.GrowthMomentAges.Length - 1];

    private Dictionary<int, VatGrowthTracker> _trackers = new();

    public Dictionary<int, VatGrowthTracker> Trackers => _trackers;

    public GrowthTrackerRepository() : base(Find.World) { }

    public GrowthTrackerRepository(World world) : base(world) { }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref _trackers, nameof(_trackers), LookMode.Value, LookMode.Deep);
        if (Scribe.mode != LoadSaveMode.LoadingVars)
            return;

        //on load
        //trim the repo of null, dead and too old pawns and report removals to the log
        //will add any other checks as I find I need them.
        List<VatGrowthTracker> trackers = _trackers.Values.ToList();
        List<int> ids = _trackers.Keys.ToList();
        for (int i = ids.Count - 1; i >= 0; i--)
        {
            Pawn pawn = trackers[i].Pawn;
            int id = ids[i];

            bool removeThis = false;
            string pawnID = "";
            string reason = "";
            if (pawn == null)
            {
                removeThis = true;
                pawnID = $"null (id:{id})";
                reason = "Tracked pawn reference was null on load. Check saved pawn ID, or maybe the pawn was completely removed by mods or dev tools.";
            }
            else if (pawn.Destroyed)
            {
                removeThis = true;
                pawnID = pawn.ThingID;
                reason = $"Tracked '{pawnID}' was completely destroyed.";
            }
            else if (pawn.ageTracker.AgeBiologicalTicks > _maxTicksForCleanup)
            {
                removeThis = true;
                pawnID = pawn.ThingID;
                reason =
                    $"Tracked '{pawnID}' has aged beyond their max growth moment without receiving a growth moment letter and removing their own tracker (not in player faction at the time?).";
            }

            if (!removeThis)
                continue;


            _trackers.Remove(id);
            Log.Message($"GrowthVatsOverclocked :: Removed growth tracker for '{pawnID}' from global repository. Reason: {reason}");
        }
    }
}
