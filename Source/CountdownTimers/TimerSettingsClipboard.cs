// TimerSettingsClipboard.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/04/18 1:25 AM
// Last edited by: Anthony Chenevier on 2023/04/18 1:25 AM


using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GrowthVatsOverclocked.CountdownTimers;

public static class TimerSettingsClipboard
{
    private static List<CountdownTimerSettings> clipboard = new();
    private static bool copied;

    public static bool HasCopiedSettings => copied;

    public static void Copy(IEnumerable<CountdownTimerSettings> s)
    {
        clipboard.Clear();
        foreach (CountdownTimerSettings settings in s)
        {
            CountdownTimerSettings clip = new();
            clip.CopyFrom(settings);
            clipboard.Add(clip);
        }

        copied = true;
        Messages.Message("TimerSettingsCopiedToClipboard".Translate(), null, MessageTypeDefOf.NeutralEvent, false);
    }

    public static void PasteInto(IEnumerable<CountdownTimerSettings> s)
    {
        List<CountdownTimerSettings>.Enumerator clipEnumerator = clipboard.GetEnumerator();
        foreach (CountdownTimerSettings settings in s)
            if (clipEnumerator.MoveNext())
                settings.CopyFrom(clipEnumerator.Current);
            else
                Log.Error("GrowthVatsOverclocked :: attempt to enumerate timer clipboard failed - reached end of clipboard before all timers were copied");

        clipEnumerator.Dispose();
        Messages.Message("TimerSettingsPastedFromClipboard".Translate(), null, MessageTypeDefOf.NeutralEvent, false);
    }

    public static IEnumerable<Gizmo> CopyPasteGizmosFor(IEnumerable<CountdownTimerSettings> s)
    {
        Command_Action copyGizmo = new()
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings"), defaultLabel = "CommandCopyTimerSettingsLabel".Translate(),
            defaultDesc = "CommandCopyTimerSettingsDesc".Translate(), action = () =>
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Copy(s);
            },
        };

        yield return copyGizmo;

        Command_Action pasteGizmo = new()
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings"), defaultLabel = "CommandPasteTimerSettingsLabel".Translate(),
            defaultDesc = "CommandPasteTimerSettingsDesc".Translate(), action = () =>
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                PasteInto(s);
            },
        };

        if (!HasCopiedSettings)
            pasteGizmo.Disable();

        yield return pasteGizmo;
    }
}
