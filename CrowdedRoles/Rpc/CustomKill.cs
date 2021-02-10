using System;
using CrowdedRoles.Extensions;
using Hazel;
using Reactor;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc]
    public class CustomKill : PlayerCustomRpc<RoleApiPlugin, CustomKill.Data>
    {
        public CustomKill(RoleApiPlugin plugin) : base(plugin) {}

        public struct Data
        {
            public byte target;
            public CustomMurderOptions options;
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;
        
        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.target);
            writer.Write((uint)data.options);
        }

        public override Data Read(MessageReader reader) => new()
        {
            target = reader.ReadByte(),
            options = (CustomMurderOptions)reader.ReadUInt32()
        };

        public override void Handle(PlayerControl sender, Data data)
        {
            sender.CustomMurderPlayer(
                GameData.Instance.GetPlayerById(data.target)?.Object, 
                data.options
            );
        }
    }
}