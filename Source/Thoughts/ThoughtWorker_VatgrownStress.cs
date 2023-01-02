// ThoughtWorker_VatgrownStress.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2022/12/24 3:31 PM
// Last edited by: Anthony Chenevier on 2022/12/24 6:17 PM


using GrowthVatsOverclocked.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.Thoughts;

public class ThoughtWorker_VatgrownStress : ThoughtWorker
{
    //mostly works the same as ThoughtWorker_Hediff, but
    //also looks at counsel count to reduce stage expressed
    protected override ThoughtState CurrentStateInternal(Pawn pawn)
    {
        if (pawn.health.hediffSet.GetFirstHediffOfDef(def.hediff) is not { } hediff || hediff.TryGetComp<HediffComp_CounselHediffThought>() is not { } stressComp)
            return ThoughtState.Inactive;

        return ThoughtState.ActiveAtStage(Mathf.Min(Mathf.Max(hediff.CurStageIndex - stressComp.CounselCount, 0), hediff.def.stages.Count - 1, def.stages.Count - 1));
    }
}
