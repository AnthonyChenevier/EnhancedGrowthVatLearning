// CompTimedActionOwner.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/25 6:08 PM
// Last edited by: Anthony Chenevier on 2023/03/25 6:08 PM


using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GrowthVatsOverclocked.CountdownTimers;

//IGameTimerParent implementation. Subclasses must implement
//GetTimers, GetSettings, TimerCanStart and GetTicks.
public abstract class CompCountdownTimerOwner : ThingComp, ICountdownTimerParent
{
    public virtual bool TimerTabVisible => GetTimers().Any();
    public virtual int VatTimeFactor => Building_GrowthVat.AgeTicksPerTickInGrowthVat;

    public abstract IEnumerable<CountdownTimer> GetTimers();
    public abstract IEnumerable<CountdownTimerSettings> GetSettings();
    public abstract AcceptanceReport TimerCanStart(CountdownTimer timer);

    public abstract AcceptanceReport ValidateSettings(float newSpanAmount, CountdownTimer.SpanType newSpanType, CountdownTimer.TickType newTickType);
    public abstract int GetTicks(CountdownTimer.TickType tickType, bool startTicks = false);

    public override void CompTick()
    {
        base.CompTick();
        foreach (CountdownTimer timedAction in GetTimers())
            timedAction.Check();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (TimerTabVisible)
        {
            foreach (Gizmo gizmo in TimerControlGizmo(GetTimers()))
                yield return gizmo;

            foreach (Gizmo gizmo in TimerSettingsClipboard.CopyPasteGizmosFor(GetSettings()))
                yield return gizmo;
        }

        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
    }

    private IEnumerable<Gizmo> TimerControlGizmo(IEnumerable<CountdownTimer> settings)
    {
        foreach (CountdownTimer timer in settings)
        {
            Command_Toggle gizmo = new()
            {
                defaultLabel = "ToggleTimer_Label".Translate(timer.Label), defaultDesc = "ToggleTimer_Desc".Translate(timer.Label, timer.CurrentSetting, timer.Desc),
                //icon = ContentFinder<Texture2D>.Get("UI/Gizmos/TimerControlGizmo"),
                activateSound = timer.IsEnabled ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn, isActive = () => timer.IsEnabled,
                toggleAction = () => { timer.SetEnabled(!timer.IsEnabled); },
            };

            yield return gizmo;
        }
    }
}

