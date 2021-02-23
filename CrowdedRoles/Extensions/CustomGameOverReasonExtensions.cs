using CrowdedRoles.GameOverReasons;
using CrowdedRoles.Rpc;
using System;
using Reactor;

namespace CrowdedRoles.Extensions
{
    public static class CustomGameOverReasonExtensions
    {
        public static bool IsCustom(this GameOverReason reason)
        {
            return reason == CustomGameOverReasonManager.CustomReasonId;
        }

        /*private static void CustomOnGameEnd(this AmongUsClient client, CustomGameOverReason reason)
        {
            StatsManager.Instance.BanPoints -= 1.5f; // why not
            StatsManager.Instance.LastGameStarted = Il2CppSystem.DateTime.MinValue;
            client.DisconnectHandlers.Clear();
            if (Minigame.Instance)
            {
                try
                {
                    Minigame.Instance.Close();
                    Minigame.Instance.Close();
                }
                catch
                {
                    // ignored
                }
            }
            
            CustomGameOverReasonManager.EndReason = reason;
            TempData.EndReason = reason;
            client.StartCoroutine(client.CoEndGame());
        }
        
        public static void CustomEndGame(this AmongUsClient client, CustomGameOverReason reason)
        {
            if (client.GameState == InnerNetClient.GameStates.Ended)
            {
                return;
            }
            client.GameState = InnerNetClient.GameStates.Ended;
            lock (client.allClients) // idk stolen from dnSpy
            {
                client.allClients.Clear();
            }

            lock (client.Dispatcher)
            {
                client.Dispatcher.Add((Action)(() => client.CustomOnGameEnd(reason)));
                // return;
            }

            // lock (client.Dispatcher)
            // {
            //     client.Dispatcher.Add((Action)(() => client.OnW));
            // }
        }
        
        public static void CustomEndGame<T>(this AmongUsClient client) where T: CustomGameOverReason
        {
            var reason = CustomGameOverReasonManager.ReasonFromType<T>();
            if (reason == null)
            {
                RoleApiPlugin.Logger.LogError($"{nameof(T)} is not registered");
                return;
            }
            client.CustomEndGame(reason);
        }*/

        public static void RpcCustomEndGame<T>(this PlayerControl sender) where T : CustomGameOverReason
        {
            var reason = CustomGameOverReasonManager.ReasonFromType<T>();
            if (reason == null)
            {
                RoleApiPlugin.Logger.LogError($"{nameof(T)} is not registered");
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
            //Rpc<CustomEndGame>.Instance.Send(reason);
        }
    }
}