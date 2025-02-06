using System;
using System.Collections;
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
    internal static bool IsInitializingGame = false;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    private static void MarkServerStart(StartOfRound __instance)
    {
        IsInitializingGame = true;
        __instance.StartCoroutine(WaitCoupleOfFrames());
    }

    private static IEnumerator WaitCoupleOfFrames()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        IsInitializingGame = false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static IEnumerable<CodeInstruction> RedirectSpawnOnGroundCheck(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        var itemPropertiesFld = AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.itemProperties));
        var spawnsOnGroundFld = AccessTools.Field(typeof(Item), nameof(Item.itemSpawnsOnGround));

        var replacementMethod = AccessTools.Method(typeof(OutOfBoundsItemsFix), nameof(NewSpawnOnGroundCheck));

        var matcher = new CodeMatcher(codes);


        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, itemPropertiesFld),
            new CodeMatch(OpCodes.Ldfld, spawnsOnGroundFld),
            new CodeMatch(OpCodes.Brfalse)
        );

        if (matcher.IsInvalid)
        {
            return codes;
        }

        matcher.Advance(1);

        matcher.RemoveInstructions(2);

        matcher.Insert(new CodeInstruction(OpCodes.Call, replacementMethod));

        TooManyItems.Log.LogDebug("GrabbableObject.Start patched!");

        return matcher.Instructions();
    }

    private static bool NewSpawnOnGroundCheck(GrabbableObject grabbableObject)
    {
        var ret = grabbableObject.itemProperties.itemSpawnsOnGround;

        //if it's one of the pre-existing items
        if (grabbableObject is ClipboardItem ||
            (grabbableObject is PhysicsProp && grabbableObject.itemProperties.itemName == "Sticky note"))
            return ret;

        if (StartOfRound.Instance.localPlayerController && !IsInitializingGame)
            return ret;

        ret = StartOfRound.Instance.IsServer;

        return ret;
    }

    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadUnlockables))]
    private static void CorrectlyPlaceAllUnlockables(StartOfRound __instance)
    {
        foreach (var placeableObject in Object.FindObjectsOfType<AutoParentToShip>()) 
            placeableObject.MoveToOffset();

        Physics.SyncTransforms();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
    private static void OnShipLeave(RoundManager __instance, bool despawnAllItems)
    {

        var shipTransform = StartOfRound.Instance.elevatorTransform;
        var grabbableObjects = shipTransform.GetComponentsInChildren<GrabbableObject>();

        var shipCollider = StartOfRound.Instance.shipInnerRoomBounds;

        var miny = shipCollider.bounds.min.y;

        foreach (var item in grabbableObjects)
        {
            if (item.NetworkObject.transform.parent != shipTransform)
            {
                continue;
            }

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

        TooManyItems.Log.LogDebug("SaveItemsInShip Patched");
        return matcher.Instructions();
    }

    private static Vector3 ApplyVerticalOffset(GrabbableObject grabbable, Vector3 position)
    {
        var newPos = position;

        if (grabbable.isHeld || grabbable.isHeldByEnemy)
            return position;

        if (!grabbable.hasHitGround)
            newPos = grabbable.targetFloorPosition;

        newPos += Vector3.down * grabbable.itemProperties.verticalOffset;
        newPos += Vector3.up   * 0.01f;

        return newPos;
    }
}
