using System;
using CrowdedRoles.Api.Extensions;
using CrowdedRoles.Api.Roles;
using Hazel;
using Reactor;

namespace CrowdedRoles.Api.Rpc
{
    [RegisterCustomRpc]
    public class SelectCustomRole : CustomRpc<MainPlugin, PlayerControl, SelectCustomRole.Data>
    {
        public SelectCustomRole(MainPlugin plugin) : base(plugin){}

        public struct Data
        {
            public RoleData role;
            public byte[] holders;
        }
        
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.role);
            writer.WriteBytesAndSize(data.holders);
        }

        public override Data Read(MessageReader reader) => new()
            {
                role = reader.Read<RoleData>(),
                holders = reader.ReadBytesAndSize()
            };

        public override void Handle(PlayerControl innerNetObject, Data data)
        {
            foreach (var id in data.holders)
            {
                GameData.Instance.GetPlayerById(id)?.Object.InitRole(RoleManager.GetRoleByData(data.role));
            }
        }
    }
}