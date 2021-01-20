using System.Linq;
using CrowdedRoles.Api.Extensions;
using HarmonyLib;
using Hazel;
using CrowdedRoles.Api.Roles;

namespace CrowdedRoles.Api.Patches
{
    public static class Rpc
    {
        private static void SelectCustomRole(byte roleId, byte[] players)
        {
            foreach(var id in players)
            {
                var player = GameData.Instance.GetPlayerById(id);
                player?.Object.InitRole(RoleManager.GetRoleById(roleId));
            }
        }

        public static void RpcSelectCustomRole(byte roleId, byte[] players)
        {
            if(AmongUsClient.Instance.AmClient)
            {
                SelectCustomRole(roleId, players);
            }
            var writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRpcCalls.SelectCustomRole, SendOption.Reliable);
            writer.Write(roleId);
            writer.Write(players.ToArray());
            writer.EndMessage();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        [HarmonyPatch(new[] { typeof(byte), typeof(MessageReader) })]
        private static class PlayerControl_HandleRpc
        {
            static bool Prefix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
            {
                switch((CustomRpcCalls)callId)
                {
                    case CustomRpcCalls.SelectCustomRole:
                        var roleId = reader.ReadByte();
                        var players = reader.ReadBytesAndSize();
                        RpcSelectCustomRole(roleId, players);
                        break;
                    case CustomRpcCalls.SyncCustomSettings:
                        var version = reader.ReadByte();
                        // do stuff
                        break;
                    default:
                        return true;
                }
                return false;
            }
        }
    }
}
