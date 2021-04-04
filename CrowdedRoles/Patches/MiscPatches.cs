using CrowdedRoles.Extensions;
using HarmonyLib;

namespace CrowdedRoles.Patches
{
    internal static class MiscPatches
    {
        [HarmonyPatch(typeof(GameData), nameof(GameData.AddPlayer))]
        public static class GameData_AddPlayer
        {
            public static void Postfix([HarmonyArgument(0)] PlayerControl player)
            {
                if (AmongUsClient.Instance.AmHost && !player.AmOwner)
                {
                    PlayerControl.LocalPlayer.RpcSyncCustomSettings(player.OwnerId);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ResetAnimState))]
        public static class PlayerPhysics_ResetAnimState
        {
            public static void Postfix(PlayerPhysics __instance)
            {
                if (__instance.myPlayer && __instance.myPlayer.Data.IsDead)
                {
                    __instance.myPlayer.Visible = PlayerControl.LocalPlayer.Data.IsDead;
                }
            }
        }
    }
}