using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using Hazel;
using System.Collections.Generic;
using Reactor;
using Reactor.Networking;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc((uint)CustomRpcCalls.SelectCustomRole)]
    internal class SelectCustomRole : PlayerCustomRpc<RoleApiPlugin, Dictionary<RoleData, byte[]>>
    {
        public SelectCustomRole(RoleApiPlugin plugin, uint id) : base(plugin, id)
        {
        }
        
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

        public override void Write(MessageWriter writer, Dictionary<RoleData, byte[]> data)
        {
            foreach (var (role, holders) in data)
            {
                role.Serialize(writer);
                writer.WriteBytesAndSize(holders);
            }
        }

        public override Dictionary<RoleData, byte[]> Read(MessageReader reader)
        {
            var result = new Dictionary<RoleData, byte[]>();
            while (reader.Position < reader.Length)
            {
                result.Add(
                    RoleData.Deserialize(reader),
                    reader.ReadBytesAndSize()
                );
            }

            return result;
        }

        public override void Handle(PlayerControl sender, Dictionary<RoleData, byte[]> data)
        {
            if (sender.OwnerId != AmongUsClient.Instance.HostId)
            {
                RoleApiPlugin.Logger.LogWarning($"{sender.NetId} sent {nameof(SelectCustomRole)} but was not a host");
                return;
            }

            if (RoleManager.RolesSet)
            {
                RoleApiPlugin.Logger.LogWarning($"{sender.NetId} tried to override roles");
                return;
            }
            
            foreach (var (role, ids) in data)
            {
                foreach (byte id in ids)
                {
                    GameData.Instance.GetPlayerById(id)?.Object.InitRole(RoleManager.GetRoleByData(role));
                }
            }

            RoleManager.RolesSet = true;
        }
    }
}