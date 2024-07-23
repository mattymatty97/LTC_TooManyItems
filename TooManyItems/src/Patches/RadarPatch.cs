﻿using System;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TooManyItems.Patches;

[HarmonyPatch]
internal class RadarPatch
{
    [HarmonyPatch]
    internal class ItemInShipPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
        [HarmonyPriority(Priority.Last)]
        private static void UpdatePatch(GrabbableObject __instance, bool __runOriginal)
        {
            if (!__runOriginal)
                return;

            if (__instance.isInShipRoom && __instance.radarIcon != null)
                Object.Destroy(__instance.radarIcon.gameObject);
        }
    }

    [HarmonyPatch]
    internal class DeletedObjectPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnDestroy))]
        private static void DestroyPatch(NetworkBehaviour __instance)
        {
            var obj = __instance as GrabbableObject;
            if (obj != null && obj.radarIcon != null && obj.radarIcon.gameObject != null)
                Object.Destroy(obj.radarIcon.gameObject);
        }
    }
}