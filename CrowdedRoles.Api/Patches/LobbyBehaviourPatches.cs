using CrowdedRoles.Api.Components;
using HarmonyLib;
using UnityEngine;

namespace CrowdedRoles.Api.Patches
{
    internal static class LobbyBehaviourPatches
    {
        [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
        static class LobbyBehaviour_Start
        {
            static void Postfix()
            {
                var gameObject = new GameObject("CustomRoleOptions");
                gameObject.transform.SetParent(DestroyableSingleton<HudManager>.Instance.transform);
                gameObject.AddComponent<CustomGameOptions>();
            }
        }
    }
}