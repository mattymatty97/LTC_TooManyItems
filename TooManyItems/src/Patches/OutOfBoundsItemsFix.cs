﻿using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TooManyItems.Patches;

[HarmonyPatch]
internal class OutOfBoundsItemsFix
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadUnlockables))]
    private static void CorrectlyPlaceAllUnlockables(StartOfRound __instance)
    {
        foreach (var placeableObject in Object.FindObjectsOfType<AutoParentToShip>()) 
            placeableObject.MoveToOffset();

        Physics.SyncTransforms();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static Collider GetVehicleCollider()
    {
        return Object.FindObjectOfType<VehicleController>()?.boundsCollider;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
    private static void ShipLeave(RoundManager __instance, bool despawnAllItems)
    {
        var objectsOfType = Object.FindObjectsOfType<GrabbableObject>();

        var shipCollider = StartOfRound.Instance.shipInnerRoomBounds;

        Collider vehicleCollider = null;
        try
        {
            vehicleCollider = GetVehicleCollider();
        }
        catch (TypeLoadException)
        {
            //ignore for pre-cruiser compatibility
        }

        var miny = vehicleCollider == null
            ? shipCollider.bounds.min.y
            : Math.Min(shipCollider.bounds.min.y, vehicleCollider.bounds.min.y);

        foreach (var item in objectsOfType)
        {
            if (!item.isInShipRoom)
                continue;

            var transform = item.transform;
            if (transform.position.y >= miny)
                continue;

            transform.position = shipCollider.bounds.center;
            item.targetFloorPosition = transform.localPosition;
            item.FallToGround();
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveItemsInShip))]
    private static IEnumerable<CodeInstruction> SaveItemsCorrectly(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilGenerator)
    {
        var codes = instructions.ToList();
        var newOffsetMethod = AccessTools.Method(typeof(OutOfBoundsItemsFix), nameof(ApplyVerticalOffset));
        var getTransformMethod = AccessTools.Property(typeof(Component), nameof(Component.transform)).GetMethod;
        var getPositionMethod = AccessTools.Property(typeof(Transform), nameof(Transform.position)).GetMethod;

        var matcher = new CodeMatcher(codes, ilGenerator);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Ldloc_0),
            new CodeMatch(OpCodes.Ldloc_S),
            new CodeMatch(OpCodes.Ldelem_Ref),
            new CodeMatch(OpCodes.Callvirt, getTransformMethod),
            new CodeMatch(OpCodes.Callvirt, getPositionMethod)
        );

        if (matcher.IsInvalid)
        {
            TooManyItems.Log.LogError("Cannot patch SaveItemsInShip");
            TooManyItems.Log.LogDebug(string.Join("\n", codes));
            return codes;
        }

        matcher.Advance(4);
        matcher.Insert(new CodeInstruction(OpCodes.Dup));
        matcher.Advance(3);
        matcher.Insert(new CodeInstruction(OpCodes.Call, newOffsetMethod));

        TooManyItems.Log.LogInfo("SaveItemsInShip Patched");
        return matcher.Instructions();
    }

    private static Vector3 ApplyVerticalOffset(GrabbableObject grabbable, Vector3 position)
    {
        var newPos = position + Vector3.down * grabbable.itemProperties.verticalOffset;
        TooManyItems.Log.LogDebug($"{grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) fixing saved position pos:{position} newpos:{newPos}");
        return newPos;
    }

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    [HarmonyPriority(Priority.Last)]
    internal class ObjectCreationPatch
    {
        private static void Prefix(GrabbableObject __instance, out bool __state)
        {
            __state = __instance.itemProperties.itemSpawnsOnGround;

            if (__instance is ClipboardItem ||
                (__instance is PhysicsProp && __instance.itemProperties.itemName == "Sticky note"))
                return;

            //only run patch on join ( playerObject not yet assigned )
            if (StartOfRound.Instance.localPlayerController != null)
                return;

            __instance.itemProperties.itemSpawnsOnGround = __instance.IsServer;

            if (!__instance.IsServer)
                return;

            __instance.transform.position += Vector3.up * 0.02f;
        }

        private static void Postfix(GrabbableObject __instance, bool __state)
        {
            __instance.itemProperties.itemSpawnsOnGround = __state;
        }
    }
}