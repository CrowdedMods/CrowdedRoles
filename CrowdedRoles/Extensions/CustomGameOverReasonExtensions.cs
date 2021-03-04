using CrowdedRoles.GameOverReasons;
using HarmonyLib;

namespace CrowdedRoles.Extensions
{
    public static class CustomGameOverReasonExtensions
    {
        public static bool IsCustom(this GameOverReason reason)
        {
            return reason == CustomGameOverReasonManager.CustomReasonId;
        }

        public static void RpcCustomEndGame<T>(this PlayerControl sender) where T : CustomGameOverReason
        {
            var reason = CustomGameOverReasonManager.ReasonFromType<T>();
            if (reason == null)
            {
                RoleApiPlugin.Logger.LogError($"{typeof(T).FullDescription()} is not registered");
                return;
            }
            sender.RpcCustomEndGame(reason);
        }
        
        public static void RpcCustomEndGame(this PlayerControl sender, CustomGameOverReason reason)
        {
            if (sender.OwnerId != AmongUsClient.Instance.HostId)
            {
                return;
            }

            CustomGameOverReasonManager.EndReason = reason;
            ShipStatus.RpcEndGame(reason, true);
        }
    }
}