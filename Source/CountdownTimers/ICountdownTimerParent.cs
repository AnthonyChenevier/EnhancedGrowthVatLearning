// ITimerSettingsParent.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/03/10 12:37 PM
// Last edited by: Anthony Chenevier on 2023/03/10 12:37 PM


using System.Collections.Generic;
using Verse;

namespace GrowthVatsOverclocked.CountdownTimers;

public interface ICountdownTimerParent
{
    bool TimerTabVisible { get; }
    int TimeFactor { get; }
    IEnumerable<CountdownTimerSettings> GetSettings();
    int GetTicks(CountdownTimer.TickType tickType, bool startTicks = false);
    AcceptanceReport TimerCanStart(CountdownTimer timer);
    AcceptanceReport ValidateSettings(float newSpanAmount, CountdownTimer.SpanType newSpanType, CountdownTimer.TickType newTickType);
}
