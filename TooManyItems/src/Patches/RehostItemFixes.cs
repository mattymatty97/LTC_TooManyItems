using HarmonyLib;
using Unity.Netcode;

namespace TooManyItems.Patches;

[HarmonyPatch]
internal class RehostItemFixes
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
    private static void OnNetworkSpawn(NetworkBehaviour __instance)
    {
        var grabbable = __instance as GrabbableObject;
        if( grabbable == null)
            return;

        var startOfRound = StartOfRound.Instance;
        if (GameNetworkManager.Instance.gameHasStarted || !startOfRound.inShipPhase)
            return;

        grabbable.isInElevator = true;
        grabbable.isInShipRoom = true;
        
        if (grabbable.radarIcon != null)
            UnityEngine.Object.Destroy(grabbable.radarIcon.gameObject);
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
    
}