using System;
using HarmonyLib;
using TooManyItems.Dependency;
using TooManyItems.Patches.Utility;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TooManyItems.Patches;

[HarmonyPatch]
internal class OutOfBoundsItemsFix
{
        
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
    private static void ObjectCreation(NetworkBehaviour __instance)
    {
        if (__instance is not GrabbableObject obj)
            return;

        if (obj is ClipboardItem || (obj is PhysicsProp && obj.itemProperties.itemName == "Sticky note"))
            return;

        if (!obj.isInShipRoom && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position))
            return;
            
        if (StartOfRound.Instance.localPlayerController != null && !StartOfRound.Instance.localPlayerController.justConnected)
            GrabbableObjectUtility.AppendToHolder(obj, nameof(OutOfBoundsItemsFix), (int)GrabbableObjectUtility.DelayValues.OutOfBounds, UpdateCallback);
        else if(obj.IsServer)
            GrabbableObjectUtility.AppendToHolder(obj,nameof(OutOfBoundsItemsFix), (int)GrabbableObjectUtility.DelayValues.OutOfBoundsServer, UpdateCallback);
        else
            GrabbableObjectUtility.AppendToHolder(obj,nameof(OutOfBoundsItemsFix), (int)GrabbableObjectUtility.DelayValues.OutOfBoundsClient, UpdateCallback);
    }

    private static void UpdateCallback(GrabbableObject __instance,
        GrabbableObjectUtility.UpdateHolder updateHolder)
    {
        var collider = StartOfRound.Instance.shipInnerRoomBounds;

        var closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
        var transform = __instance.transform;

        if (closet == null || transform.parent != closet.transform)
        {
            var position = updateHolder.OriginalPos;
            position += Vector3.up * (-Math.Min(0, __instance.itemProperties.verticalOffset) + 0.2f);
                
            if (position.y < collider.bounds.min.y)
                position = collider.bounds.center;
                
            transform.position = position;
            __instance.targetFloorPosition = transform.localPosition;
                                
            __instance.FallToGround();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
    private static void ShipLeave(RoundManager __instance, bool despawnAllItems)
    {

        if (AsyncLoggerProxy.Enabled)
            AsyncLoggerProxy.WriteEvent(TooManyItems.NAME, "ShipLeave", $"Called");
            
        GrabbableObject[] objectsOfType = Object.FindObjectsOfType<GrabbableObject>();

        var collider = StartOfRound.Instance.shipInnerRoomBounds;

        foreach (var item in objectsOfType)
        {
            if (item is ClipboardItem || (item is PhysicsProp && item.itemProperties.itemName == "Sticky note"))
                continue;

            if (!item.isInShipRoom)
                continue;

            var transform = item.transform;
            if (transform.position.y < collider.bounds.min.y)
            {
                transform.position = collider.bounds.center;
                item.targetFloorPosition = transform.localPosition;
                item.FallToGround();
            }
        }
    }
}