using CrowdedRoles.Extensions;
using Hazel;
using Reactor;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc]
    internal class CustomKill : PlayerCustomRpc<RoleApiPlugin, CustomKill.Data>
    {
        public CustomKill(RoleApiPlugin plugin) : base(plugin) {}

        public struct Data
        {
            public PlayerControl killer;
            public PlayerControl target;
            public CustomMurderOptions options;
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;
        
        public override void Write(MessageWriter writer, Data data)
        {
            MessageExtensions.WriteNetObject(writer, data.killer);
            MessageExtensions.WriteNetObject(writer, data.target);
            writer.Write((uint)data.options);
        }

        public override Data Read(MessageReader reader) => new()
        {
            killer = MessageExtensions.ReadNetObject<PlayerControl>(reader),
            target = MessageExtensions.ReadNetObject<PlayerControl>(reader),
            options = (CustomMurderOptions)reader.ReadUInt32()
        };

        public override void Handle(PlayerControl sender, Data data)
        {
            if (sender.OwnerId != AmongUsClient.Instance.HostId)
            {
                RoleApiPlugin.Logger.LogWarning($"{sender.OwnerId} sent {nameof(CustomKill)}, but was not a host");
                return;
            }
            
            data.killer.CustomMurderPlayer(data.target, data.options);
        }
    }
}