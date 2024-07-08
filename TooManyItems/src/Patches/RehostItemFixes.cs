using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace TooManyItems.Patches;

[HarmonyPatch]
internal class RehostItemFixes
{

    private static bool _fullyLoaded = false;
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
    private static void OnNetworkSpawn(NetworkBehaviour __instance)
    {
        if (__instance.transform.name is "ClipboardManual" or "StickyNoteItem")
            return;
        
        var grabbable = __instance as GrabbableObject;
        if( grabbable == null)
            return;
        

        if (_fullyLoaded)
            return;

        grabbable.isInElevator = true;
        grabbable.isInShipRoom = true;
        
        if (grabbable.radarIcon != null)
            UnityEngine.Object.Destroy(grabbable.radarIcon.gameObject);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static void AfterStart(GrabbableObject __instance)
    {
        if (__instance.radarIcon != null && __instance.isInShipRoom)
            UnityEngine.Object.Destroy(__instance.radarIcon.gameObject);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnDestroy))]
    private static void OnDestroy(NetworkBehaviour __instance)
    {
        var grabbable = __instance as GrabbableObject;
        if( grabbable == null)
            return;
        
        if (grabbable.radarIcon != null)
            UnityEngine.Object.Destroy(grabbable.radarIcon.gameObject);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    private static void OnFinishedLoading()
    {
        TooManyItems.Log.LogDebug($"{nameof(RehostItemFixes)} player fully loaded! {_fullyLoaded}");
        _fullyLoaded = true;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnLocalDisconnect))]
    private static void OnDisconnect()
    {
        TooManyItems.Log.LogDebug($"{nameof(RehostItemFixes)} reset1! {_fullyLoaded}");
        _fullyLoaded = false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnDestroy))]
    private static void OnDestroy()
    {
        TooManyItems.Log.LogDebug($"{nameof(RehostItemFixes)} reset2! {_fullyLoaded}");
        _fullyLoaded = false;
    }
    
}