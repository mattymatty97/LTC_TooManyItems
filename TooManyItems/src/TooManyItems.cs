﻿using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TooManyItems.Dependency;
using TooManyItems.Patches;

namespace TooManyItems
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mattymatty.LobbyControl",  Flags:BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mattymatty.MattyFixes",  Flags:BepInDependency.DependencyFlags.SoftDependency)]
    internal class TooManyItems : BaseUnityPlugin
    {
        public const string GUID = "mattymatty.TooManyItems";
        public const string NAME = "TooManyItems";
        public const string VERSION = "1.4.1";

        internal static ManualLogSource Log;
        
        private void Awake()
        {
            Log = Logger;
            try
            {
                    if (LobbyCompatibilityChecker.Enabled)
                        LobbyCompatibilityChecker.Init();
                    
                    Log.LogInfo("Patching Methods");
                    var harmony = new Harmony(GUID);
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mattymatty.LobbyControl"))
                        harmony.PatchAll(typeof(LimitPatcher));
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mattymatty.MattyFixes"))
                    {
                        harmony.PatchAll(typeof(RadarPatch));
                        harmony.PatchAll(typeof(OutOfBoundsItemsFix));
                        harmony.PatchAll(typeof(RehostItemFixes));
                    }
                    Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
            }
            catch (Exception ex)
            {
                Log.LogError("Exception while initializing: \n" + ex);
            }
        }
    }
}
