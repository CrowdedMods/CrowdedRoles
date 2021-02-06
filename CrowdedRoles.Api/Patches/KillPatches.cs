using CrowdedRoles.Api.Extensions;
using CrowdedRoles.Api.Roles;
using HarmonyLib;

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

        // Patches all (i hope) methods disabling kill button
        [HarmonyPatch]
        private static class KillButtonSetActivePatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
            private static void HudManager_SetHudActive(HudManager __instance)
            {
                BaseRole? role = PlayerControl.LocalPlayer.GetRole();
                if (role != null)
                {
                    __instance.KillButton.gameObject.SetActive(role.AbleToKill);
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
                        HudManager.Instance.KillButton.gameObject.SetActive(role.AbleToKill);
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControl.CoSetTasks__d), nameof(PlayerControl.CoSetTasks__d.MoveNext))]
            private static void PlayerControl_CoSetTasks(PlayerControl.CoSetTasks__d __instance, bool __result)
            {
                if (__result && __instance.__this.AmOwner) // yield break
                {
                    BaseRole? role = __instance.__this.GetRole();
                    if (role != null)
                    {
                        HudManager.Instance.KillButton.gameObject.SetActive(role.AbleToKill);
                    }
                }
            }
        }      
    }
}