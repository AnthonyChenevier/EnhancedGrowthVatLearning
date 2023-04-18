﻿// CountdownTimer.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/10 12:36 PM
// Last edited by: Anthony Chenevier on 2023/03/31 5:06 PM


using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatsOverclocked.CountdownTimers;

/// <summary>
/// A timer that calls a given action when it counts down to 0.
/// UI tab can be used to set countdown time, enable/disable the
/// timer completely and check it's status.
/// </summary>
public class CountdownTimer : IExposable
{
    public enum SpanType
    {
        HourSpan,
        DaySpan,
        QuadrumSpan,
        YearSpan
    }

    public enum TickType
    {
        GameTimeTick,
        PawnAgeTick,
        VatTimeTick
    }

    private readonly ICountdownTimerParent parent;
    private readonly string name;
    private readonly Action onCountdownCallback;

    //saved data
    private bool enabled;
    private float spanAmount = 1;
    private SpanType spanType = SpanType.HourSpan;
    private int startTick = -1;
    private int countdownTicks;
    private TickType tickType = TickType.GameTimeTick;

    public float TimerSpanAmount => spanAmount;
    public SpanType TimerSpanType => spanType;
    public TickType TimerTickType => tickType;

    public string Label => $"{name}Label".Translate();
    public string Desc => $"{name}Desc".Translate();


    public bool IsEnabled => enabled;
    public bool IsRunning => startTick >= 0;
    public int TicksRemaining => Mathf.Max(startTick + countdownTicks - ParentTicks(), 0);

    public string CurrentSetting => $"{countdownTicks.ToStringTicksToPeriod()} ({tickType.ToString().Translate()})";

    private AcceptanceReport ParentCanStart
    {
        get
        {
            if (parent != null)
                return parent.TimerCanStart(this);

            Log.Error("GrowthVatsOverclocked :: Attempted to access null timer parent. Using default values. ");
            return false;
        }
    }

    private int ParentVatFactor
    {
        get
        {
            if (parent != null)
                return parent.VatTimeFactor;

            Log.Error("GrowthVatsOverclocked :: Attempted to access null timer parent. Using default values. ");
            return Building_GrowthVat.AgeTicksPerTickInGrowthVat;
        }
    }

    private int ParentTicks(bool startTicks = false)
    {
        if (parent != null)
            return parent.GetTicks(tickType, startTicks);

        Log.Error("GrowthVatsOverclocked :: Attempted to access null timer parent. Using default values. ");
        return -1;
    }

    public string TimerStatus
    {
        get
        {
            if (IsRunning && IsEnabled)
            {
                int timeRemaining = tickType == TickType.VatTimeTick ? TicksRemaining / ParentVatFactor : TicksRemaining;
                return "TimerRunning".Translate(timeRemaining.ToStringTicksToPeriod()).Colorize(ColorLibrary.BrightGreen);
            }

            string stoppedReason;
            if (IsEnabled)
            {
                AcceptanceReport canStart = ParentCanStart;
                stoppedReason = canStart.Accepted ? "TimerFinished".Translate().Colorize(ColorLibrary.BabyBlue) : canStart.Reason;
            }
            else
            {
                stoppedReason = "TimerDisabled".Translate();
            }

            return "TimerStopped".Translate(stoppedReason).Colorize(ColorLibrary.RedReadable);
        }
    }

    //used for copy-paste only
    internal CountdownTimer() : this(null, "_", null) { }

    public CountdownTimer(ICountdownTimerParent parent, string name, Action onCountdownCallback)
    {
        this.parent = parent;
        this.name = name;
        this.onCountdownCallback = onCountdownCallback;
    }

    public CountdownTimer(ICountdownTimerParent parent, string name, Action onCountdownCallback, float defaultSpanAmount, SpanType defaultSpanType, TickType defaultTickType) :
        this(parent, name, onCountdownCallback) =>
        Set(defaultSpanAmount, defaultSpanType, defaultTickType);

    public void CopyFrom(CountdownTimer other)
    {
        enabled = other.enabled;
        startTick = other.startTick;
        Set(other.spanAmount, other.spanType, other.tickType);
    }

    private AcceptanceReport ValidateSettings(float newSpanAmount, SpanType newSpanType, TickType newTickType)
    {
        if (parent != null)
            return parent.ValidateSettings(newSpanAmount, newSpanType, newTickType);


        Log.Error("GrowthVatsOverclocked :: Attempted to access null timer parent. Using default values. ");
        return false;
    }

    public void Set(float newSpanAmount, SpanType newSpanType, TickType newTickType)
    {
        AcceptanceReport settingValid = ValidateSettings(newSpanAmount, newSpanType, newTickType);
        if (!settingValid)
        {
            Messages.Message("TimerSettingsNotValid".Translate(Label, settingValid.Reason).CapitalizeFirst(), null, MessageTypeDefOf.RejectInput, false);
            return;
        }

        spanAmount = newSpanAmount;
        spanType = newSpanType;

        countdownTicks = Mathf.RoundToInt(newSpanAmount * TicksPerSpan(newSpanType));

        Messages.Message("TimerSet".Translate(Label, CurrentSetting).CapitalizeFirst(), null, MessageTypeDefOf.NeutralEvent, false);

        if (tickType == newTickType)
            return;

        tickType = newTickType;
        //change in tick type requires restarting timer
        //as start tick value is no longer relevant to the tick type
        if (IsRunning)
            Start();
    }

    public static int TicksPerSpan(SpanType spanType)
    {
        return spanType switch
        {
            SpanType.YearSpan => GenDate.TicksPerYear,
            SpanType.QuadrumSpan => GenDate.TicksPerQuadrum,
            SpanType.DaySpan => GenDate.TicksPerDay,
            SpanType.HourSpan or _ => GenDate.TicksPerHour
        };
    }

    public void SetEnabled(bool value)
    {
        enabled = value;
        if (enabled)
            Start();
        else if (IsRunning)
            Stop();
    }

    public void Start(bool ignoreStartCondition = false)
    {
        if (ignoreStartCondition || ParentCanStart)
            startTick = ParentTicks(true);
    }

    public void Stop() => startTick = -1;

    public void Check()
    {
        if (!IsEnabled || !IsRunning || TicksRemaining > 0)
            return;

        Stop();
        if (onCountdownCallback == null)
        {
            Log.Error("GrowthVatsOverclocked :: Attempted to access null timer callback. Skipping.");
            return;
        }

        onCountdownCallback.Invoke();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref enabled, nameof(enabled));
        Scribe_Values.Look(ref startTick, nameof(startTick));
        Scribe_Values.Look(ref spanAmount, nameof(spanAmount));
        Scribe_Values.Look(ref spanType, nameof(spanType));
        Scribe_Values.Look(ref countdownTicks, nameof(countdownTicks));
        Scribe_Values.Look(ref tickType, nameof(tickType));
    }
}