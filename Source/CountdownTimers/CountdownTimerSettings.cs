// TimedActionSettings.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/22 5:30 PM
// Last edited by: Anthony Chenevier on 2023/03/22 5:30 PM


using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.CountdownTimers;

public class CountdownTimerSettings
{
    private readonly CountdownTimer timer;
    private readonly List<CountdownTimer.TickType> disallowedTickTypes;

    //unsaved values
    private string editBuffer;

    private float uiSpanAmount;
    private CountdownTimer.SpanType uiSpanType;
    private CountdownTimer.TickType uiTickType;
    private Vector2 scrollPosition;

    public bool SettingsApplied => uiSpanAmount == timer.TimerSpanAmount && uiSpanType == timer.TimerSpanType && uiTickType == timer.TimerTickType;

    //used for copy-paste only
    internal CountdownTimerSettings()
    {
        //create dummy timer to store settings
        timer = new CountdownTimer();
    }

    public CountdownTimerSettings(CountdownTimer timer, params CountdownTimer.TickType[] disallowedTickTypes)
    {
        this.timer = timer;
        this.disallowedTickTypes = disallowedTickTypes.ToList();
        ResetUIValues();
    }

    public void CopyFrom(CountdownTimerSettings other)
    {
        timer.CopyFrom(other.timer);
        ResetUIValues();
    }

    public void ResetUIValues()
    {
        uiSpanAmount = timer.TimerSpanAmount;
        uiSpanType = timer.TimerSpanType;
        uiTickType = timer.TimerTickType;
        editBuffer = $"{uiSpanAmount}";
    }

    private void ApplyUIValues() { timer.Set(uiSpanAmount, uiSpanType, uiTickType, true); }

    public void DrawUI(Rect inRect)
    {
        Widgets.DrawMenuSection(inRect);
        inRect = inRect.ContractedBy(4f);

        Rect headerRect = inRect with { height = 60f };

        Rect bodyRect = headerRect;
        bodyRect.y += 68f;

        Rect footerRect = inRect;
        footerRect.yMin += 136f;

        //Label, short description & enable timer controls
        Rect headerRect1 = headerRect with { height = Text.LineHeight };

        //Downsize text
        GameFont originalFont = Text.Font;
        Text.Font = GameFont.Tiny;
        string label = "CbxEnableTimer".Translate() + ":";
        float enableWidth = Text.CalcSize(label).x + 24f + 10f;

        Rect enableRect = headerRect1;
        enableRect.xMin = enableRect.xMax - enableWidth;
        bool timerEnabled = timer.IsEnabled;
        Widgets.CheckboxLabeled(enableRect, label, ref timerEnabled, placeCheckboxNearText: true);
        if (timerEnabled != timer.IsEnabled)
            timer.SetEnabled(timerEnabled);

        //reset text size
        Text.Font = originalFont;

        Rect timerLabelRect = headerRect1;
        timerLabelRect.xMax -= enableWidth;
        Widgets.Label(timerLabelRect, timer.Label.CapitalizeFirst());

        //Downsize text
        originalFont = Text.Font;
        Text.Font = GameFont.Tiny;

        Rect descRect = headerRect;
        descRect.yMin += headerRect1.height + 2f;
        float descContentHeight = Text.CalcHeight(timer.Desc, descRect.width - 20f);
        float descRectWidth = descContentHeight > descRect.height ? descRect.width - 20f : descRect.width;
        Rect viewRect = new(0f, 0f, descRectWidth, descContentHeight);

        Widgets.BeginScrollView(descRect, ref scrollPosition, viewRect);
        Widgets.Label(viewRect, timer.Desc);
        Widgets.EndScrollView();

        //reset text size
        Text.Font = originalFont;
        Widgets.DrawLineHorizontal(inRect.x, headerRect.yMax + 4f, inRect.width - 2f);
        Rect setTimerRect = bodyRect;

        GameFont originalFont1 = Text.Font;
        Text.Font = GameFont.Medium;
        Rect countRect = setTimerRect;
        //5-digit entry box
        countRect.width = Text.CalcSize("00000").x + 4f;

        Rect selectorsRect = setTimerRect;
        selectorsRect.xMin += countRect.width + 4f;

        Rect spanRect = selectorsRect.TopHalf();
        Rect tickRect = selectorsRect.BottomHalf();
        //get span amount
        Widgets.TextFieldNumeric(countRect, ref uiSpanAmount, ref editBuffer);
        Text.Font = originalFont1;
        //set span type
        ListingHelper.EnumSelector(spanRect, ref uiSpanType);

        //set ticker type
        ListingHelper.EnumSelector(tickRect, ref uiTickType, disallowedTickTypes);
        Widgets.DrawLineHorizontal(inRect.x, bodyRect.yMax + 4f, inRect.width - 2f);
        Rect timerStatusRect = footerRect.RightHalf();
        TextAnchor anchor = Text.Anchor;
        GameFont originalFont2 = Text.Font;
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.LowerRight;
        Widgets.Label(timerStatusRect, timer.TimerStatus);
        Text.Anchor = anchor;
        Text.Font = originalFont2;

        Rect buttonsRect = footerRect.LeftHalf();

        Rect setButtonRect = buttonsRect with { width = 140f };

        if (Widgets.ButtonText(setButtonRect, "BtnSetTimer".Translate(), active: !SettingsApplied))
            ApplyUIValues();

        Rect resetButtonRect = buttonsRect with { y = setButtonRect.y + (footerRect.height - 36f) / 2f, xMin = setButtonRect.xMax + 4f, width = 36f, height = 36f };
        if (!SettingsApplied)
            if (Widgets.ButtonImageFitted(resetButtonRect, ContentFinder<Texture2D>.Get("UI/Widgets/RotLeft")))
                ResetUIValues();
    }
}
