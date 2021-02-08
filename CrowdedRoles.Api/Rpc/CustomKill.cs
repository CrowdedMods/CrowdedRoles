using System;
using CrowdedRoles.Api.Extensions;
using Hazel;
using Reactor;

namespace CrowdedRoles.Api.Rpc
{
    [RegisterCustomRpc]
    public class CustomKill : PlayerCustomRpc<RoleApiPlugin, CustomKill.Data>
    {
        public CustomKill(RoleApiPlugin plugin) : base(plugin) {}

        public class Data
        {
            public byte target;
            public bool noSnap = true;
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;
        
        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.target);
            writer.Write(data.noSnap);
        }

        public override Data Read(MessageReader reader) => new()
        {
            target = reader.ReadByte(),
            noSnap = reader.ReadBoolean()
        };

        public override void Handle(PlayerControl sender, Data data)
        {
            sender.CustomMurderPlayer(
                GameData.Instance.GetPlayerById(data.target)?.Object, 
                data.noSnap
            );
        }
    }
}