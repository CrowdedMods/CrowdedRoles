using System.Linq;
using CrowdedRoles.Api.Extensions;
using HarmonyLib;
using Hazel;
using CrowdedRoles.Api.Roles;

namespace CrowdedRoles.Api.Patches
{
    internal static class Rpc
    {
        private static void SelectCustomRole(RoleData data, byte[] players)
        {
            foreach(var id in players)
            {
                var player = GameData.Instance.GetPlayerById(id);
                player?.Object.InitRole(RoleManager.GetRoleByData(data));
            }
        }

        public static void RpcSelectCustomRole(RoleData data, byte[] players)
        {
            if(AmongUsClient.Instance.AmClient)
            {
                SelectCustomRole(data, players);
            }
            var writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRpcCalls.SelectCustomRole, SendOption.Reliable);
            writer.Write(data);
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
                        var data = reader.Read<RoleData>();
                        var players = reader.ReadBytesAndSize();
                        SelectCustomRole(data, players);
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
