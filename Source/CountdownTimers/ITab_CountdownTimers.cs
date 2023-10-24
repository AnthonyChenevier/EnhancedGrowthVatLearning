// ITab_ActionTimer.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/09 10:19 AM
// Last edited by: Anthony Chenevier on 2023/03/09 11:18 AM


using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.CountdownTimers;

public class ITab_CountdownTimers : ITab
{
    private const float TimerHeight = 184f;
    public static readonly Vector2 WindowSize = new(432f, 480f);
    private Vector2 scrollPosition;

    protected virtual ICountdownTimerParent SelCountdownTimerParent =>
        SelObject switch
        {
            not Thing => AllSelObjects.Count <= 1 ? SelObject as ICountdownTimerParent : null,
            ICountdownTimerParent settingsParent => settingsParent,
            ThingWithComps thingWithComps => (ICountdownTimerParent)thingWithComps.AllComps.FirstOrDefault(c => c is ICountdownTimerParent),
            _ => null
        };

    public override bool IsVisible
    {
        get
        {
            if (SelObject != null)
            {
                if (ObjIsNotPlayerOwnedThing(SelObject))
                    return false;
            }
            else
            {
                if (AllSelObjects.Count <= 1 || AllSelObjects.Any(ObjIsNotPlayerOwnedThing))
                    return false;
            }

            return SelCountdownTimerParent is { TimerTabVisible: true };
        }
    }

    private static bool ObjIsNotPlayerOwnedThing(object obj) => obj is Thing { Faction: { } } thing && thing.Faction != Faction.OfPlayer;

    public ITab_CountdownTimers()
    {
        size = WindowSize;
        labelKey = "TabTimers";
    }

    protected override void FillTab()
    {
        List<CountdownTimerSettings> settingsList = SelCountdownTimerParent.GetSettings().ToList();
        Rect windowRect = new Rect(0f, 20f, WindowSize.x, WindowSize.y - 20f).ContractedBy(10f);
        float contentHeight = settingsList.Count * TimerHeight + (settingsList.Count - 1) * 12f;
        float contentWidth = contentHeight > windowRect.height ? windowRect.width - 20f : windowRect.width;
        Rect viewRect = new(0f, 0f, contentWidth, contentHeight);

        Widgets.BeginScrollView(windowRect, ref scrollPosition, viewRect);

        Listing_Standard listing = new();
        listing.Begin(viewRect);
        for (int i = 0; i < settingsList.Count; i++)
        {
            CountdownTimerSettings setting = settingsList[i];
            Rect inRect = listing.GetRect(TimerHeight);
            setting.DrawUI(inRect);
            if (i < settingsList.Count - 1)
                listing.Gap();
        }

        listing.End();
        Widgets.EndScrollView();
    }
}