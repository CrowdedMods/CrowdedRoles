using System;
using Hazel;
using Reactor;

namespace CrowdedRoles.Api.Rpc
{
    [RegisterCustomRpc]
    public class CustomKill : CustomRpc<MainPlugin, PlayerControl, CustomKill.Data>
    {
        public CustomKill(MainPlugin plugin) : base(plugin) {}

        public class Data
        {
            public byte killer;
            public byte target;
            public bool noSnap = true;
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;
        
        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.killer);
            writer.Write(data.target);
            writer.Write(data.noSnap);
        }

        public override Data Read(MessageReader reader) => new()
        {
            killer = reader.ReadByte(),
            target = reader.ReadByte(),
            noSnap = reader.ReadBoolean()
        };

        public override void Handle(PlayerControl innerNetObject, Data data)
        {
            throw new NotImplementedException();
        }
    }
}