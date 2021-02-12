using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using HarmonyLib;
using System;
using CrowdedRoles.Rpc;
using Reactor;
using UnityEngine;

namespace CrowdedRoles.Patches
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
                if (localPlayer.Data.IsImpostor || !(localPlayer.GetRole()?.Abilities.HasFlag(PlayerAbilities.Kill) ?? false))
                {
                    return true;
                }

                if (__instance.isActiveAndEnabled && __instance.CurrentTarget && !__instance.isCoolingDown &&
                    !localPlayer.Data.IsDead && localPlayer.CanMove)
                {
                    localPlayer.RpcCustomMurderPlayer(__instance.CurrentTarget);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcRepairSystem))]
        [HarmonyPriority(Priority.First)]
        private static class ShipStatus_RpcRepairSystem
        {
            private static bool Prefix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes type, [HarmonyArgument(1)] int someEnumProbably)
            {
                if (AmongUsClient.Instance.AmHost || type != SystemTypes.Sabotage ||
                    PlayerControl.LocalPlayer.Data.IsImpostor || 
                    !(PlayerControl.LocalPlayer.GetRole()?.Abilities.HasFlag(PlayerAbilities.Sabotage) ?? false))
                {
                    return true;
                }
                
                Rpc<CustomSabotage>.Instance.SendTo(__instance, AmongUsClient.Instance.HostId, new CustomSabotage.Data
                {
                    amount = (byte)someEnumProbably
                });
                
                return false;
            }
        }

        // Patches all (i hope) methods disabling special buttons
        [HarmonyPatch]
        internal static class ButtonsSetActivePatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
            private static void HudManager_SetHudActive(HudManager __instance, [HarmonyArgument(0)] bool isActive)
            {
                BaseRole? role = PlayerControl.LocalPlayer.GetRole();
                if (role != null)
                {
                    __instance.KillButton.gameObject.SetActive(isActive && role.Abilities.HasFlag(PlayerAbilities.Kill));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Revive))]
            private static void PlayerControl_Revive(PlayerControl __instance)
            {
                if (__instance.AmOwner)
                {
                    BaseRole? role = __instance.GetRole();
                    if (role != null)
                    {
                        HudManager.Instance.KillButton.gameObject.SetActive(role.Abilities.HasFlag(PlayerAbilities.Kill));
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControl.CoSetTasks__d), nameof(PlayerControl.CoSetTasks__d.MoveNext))]
            private static void PlayerControl_CoSetTasks(PlayerControl.CoSetTasks__d __instance)
            {
                if (__instance.__this.AmOwner)
                {
                    BaseRole? role = __instance.__this.GetRole();
                    if (role != null)
                    {
                        HudManager.Instance.KillButton.gameObject.SetActive(role.Abilities.HasFlag(PlayerAbilities.Kill));
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UseButtonManager), nameof(UseButtonManager.SetTarget))]
            private static void UseButtonManager_SetTarget(UseButtonManager __instance, [HarmonyArgument(0)] IUsable? target)
            {
                if (target == null && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null &&
                    (PlayerControl.LocalPlayer.GetRole()?.Abilities.HasFlag(PlayerAbilities.Sabotage) ?? false) &&
                    PlayerControl.LocalPlayer.CanMove)
                {
                    __instance.UseButton.sprite = __instance.SabotageImage;
                    CooldownHelpers.SetCooldownNormalizedUvs(__instance.UseButton);
                    __instance.UseButton.color = UseButtonManager.EnabledColor;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UseButtonManager), nameof(UseButtonManager.DoClick))]
            private static void UseButtonManager_DoClick(UseButtonManager __instance)
            {
                if (__instance.isActiveAndEnabled && PlayerControl.LocalPlayer != null &&
                    PlayerControl.LocalPlayer.Data != null && __instance.currentTarget == null &&
                    (PlayerControl.LocalPlayer.GetRole()?.Abilities.HasFlag(PlayerAbilities.Sabotage) ?? false))
                {
                    HudManager.Instance.ShowMap((Action<MapBehaviour>)(m => m.ShowInfectedMap()));
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
            private static bool Vent_CanUse(Vent __instance, 
                [HarmonyArgument(0)] GameData.PlayerInfo player, 
                [HarmonyArgument(1)] ref bool canUse,
                [HarmonyArgument(2)] ref bool couldUse,
                ref float __result)
            {
                PlayerControl pc = player.Object;
                if (!(pc.GetRole()?.Abilities.HasFlag(PlayerAbilities.Vent) ?? false))
                {
                    return true;
                }
                __result = float.MaxValue;
                canUse = couldUse = !player.IsDead && (pc.CanMove || pc.inVent);
                if (canUse)
                {
                    Vector3 myPos = pc.GetTruePosition();
                    Vector3 ventPos = __instance.transform.position;
                    __result = Vector2.Distance(myPos, ventPos);
                    canUse = __result <= __instance.UsableDistance &&
                             !PhysicsHelpers.AnythingBetween(myPos, ventPos, Constants.ShipOnlyMask, false);
                }

                return false;
            }
        }      
    }
}