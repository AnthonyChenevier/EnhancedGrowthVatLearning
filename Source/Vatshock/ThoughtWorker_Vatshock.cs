// ThoughtWorker_VatgrownStress.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/24 3:31 PM
// Last edited by: Anthony Chenevier on 2022/12/24 6:17 PM


using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.Vatshock;

public class ThoughtWorker_Vatshock : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn pawn)
    {
        //get expressed thought stage from hediff if it exists.
        if (pawn.health.hediffSet.GetFirstHediffOfDef(def.hediff)?.TryGetComp<HediffComp_Vatshock>() is { } vatshockComp)
            return ThoughtState.ActiveAtStage(vatshockComp.CurThoughtStageIndex);

        return ThoughtState.Inactive;
    }

    public void Notify_CounseledThoughtAdded(Pawn pawn, Thought_Counselled counseledThought)
    {
        if (pawn.health.hediffSet.GetFirstHediffOfDef(def.hediff)?.TryGetComp<HediffComp_Vatshock>() is not { } counselComp)
            return;

        counselComp.Counsel();
        pawn.needs.mood.thoughts.memories.RemoveMemory(counseledThought);
    }
}
