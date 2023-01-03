// PatchInitializer.cs
// 
// Part of GrowthVatsOverclocked - GrowthVatsOverclocked
// 
// Created by: Anthony Chenevier on 2023/01/03 6:47 PM
// Last edited by: Anthony Chenevier on 2023/01/03 6:47 PM


using System.Reflection;
using HarmonyLib;
using Verse;

namespace GrowthVatsOverclocked.HarmonyPatches;

[StaticConstructorOnStartup]
public static class HarmonyPatchInitializer
{
    static HarmonyPatchInitializer()
    {
        //Harmony.DEBUG = true;
        Harmony harmony = new("makeitso.growthvatsoverclocked");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
