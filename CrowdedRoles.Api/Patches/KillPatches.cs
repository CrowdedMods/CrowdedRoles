using CrowdedRoles.Api.Extensions;
using CrowdedRoles.Api.Roles;
using HarmonyLib;
using UnityEngine;

namespace CrowdedRoles.Api.Patches
{
    internal static class KillPatches
    {
        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.PerformKill))]
        [HarmonyPriority(Priority.First)]
        private static class KillButtonManager_PerformKill
        {
            private static bool Prefix(ref KillButtonManager __instance)
            {
                PlayerControl localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer.Data.IsImpostor || !(localPlayer.GetRole()?.AbleToKill ?? false))
                {
                    return true;
                }

                if (__instance.isActiveAndEnabled && __instance.CurrentTarget && !__instance.CurrentTarget &&
                    !localPlayer.Data.IsDead && localPlayer.CanMove)
                {
                    localPlayer.RpcCustomMurderPlayer(__instance.CurrentTarget);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        [HarmonyPriority(Priority.First)]
        private static class PlayerControl_FindClosestTarget
        {
            private static bool Prefix(ref PlayerControl __instance, ref PlayerControl? __result)
            {
                BaseRole? role = __instance.GetRole();
                if (role is null)
                {
                    return true;
                }

                __result = null;
                if (!ShipStatus.Instance)
                {
                    return false;
                }

                Vector2 myPos = __instance.GetTruePosition();
                float lowestDistance =
                    GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
                foreach (GameData.PlayerInfo player in GameData.Instance.AllPlayers)
                {
                    PlayerControl obj = player.Object;
                    if(obj is null || !role.KillFilter(__instance, player)) continue;
                    Vector2 vec = obj.GetTruePosition() - myPos;
                    float magnitude = vec.magnitude;
                    if (magnitude < lowestDistance && !PhysicsHelpers.AnyNonTriggersBetween(myPos, vec.normalized,
                        magnitude, Constants.ShipAndObjectsMask))
                    {
                        __result = obj;
                        lowestDistance = magnitude;
                    }
                }

                return false;
            }
        }
    }
}