﻿using System;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace TooManyItems.Patches;

[HarmonyPatch]
internal class RehostItemFixes
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
    [HarmonyPriority(20)]
    private static void SpawnPostfix(NetworkBehaviour __instance)
    {
        if (__instance is not GrabbableObject grabbable)
            return;

        if (grabbable is ClipboardItem ||
            (grabbable is PhysicsProp && grabbable.itemProperties.itemName == "Sticky note"))
            return;

        if (StartOfRound.Instance.localPlayerController && !OutOfBoundsItemsFix.IsInitializingGame)
            return;

        grabbable.isInElevator = true;
        grabbable.isInShipRoom = true;

        if (grabbable is not LungProp lungProp)
            return;

        lungProp.isLungDocked = false;
        lungProp.isLungPowered = false;
        lungProp.isLungDockedInElevator = false;
        lungProp.GetComponent<AudioSource>()?.Stop();

    }
    
}
