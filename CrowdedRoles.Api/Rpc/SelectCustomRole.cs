using CrowdedRoles.Api.Extensions;
using CrowdedRoles.Api.Managers;
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
            if (innerNetObject.PlayerId != GameData.Instance.GetHost().PlayerId)
            {
                MainPlugin.Logger.LogError($"{innerNetObject.NetId} sent {nameof(SelectCustomRole)} but was not a host");
                return;
            }
            foreach (var id in data.holders)
            {
                GameData.Instance.GetPlayerById(id)?.Object.InitRole(RoleManager.GetRoleByData(data.role));
            }
        }
    }
}