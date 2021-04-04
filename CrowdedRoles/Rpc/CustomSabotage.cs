using CrowdedRoles.Extensions;
using Hazel;
using InnerNet;
using Reactor;
using Reactor.Networking;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc((uint)CustomRpcCalls.CustomSabotage)]
    internal class CustomSabotage : CustomRpc<RoleApiPlugin, ShipStatus, CustomSabotage.Data>
    {
        public CustomSabotage(RoleApiPlugin plugin, uint id) : base(plugin, id)
        {
        }
        
        public class Data
        {
            public PlayerControl player = PlayerControl.LocalPlayer;
            public byte amount;
        }

        public override RpcLocalHandling LocalHandling { get; } = RpcLocalHandling.None;
        public override void Write(MessageWriter writer, Data data)
        {
            MessageExtensions.WriteNetObject(writer, data.player);
            writer.Write(data.amount);
        }

        public override Data Read(MessageReader reader)
        {
            return new ()
            {
                player = MessageExtensions.ReadNetObject<PlayerControl>(reader),
                amount = reader.ReadByte()
            };
        }

        public override void Handle(ShipStatus obj, Data data)
        {
            if (data.player.GetRole()?.CanSabotage(null) ?? false)
            {
                obj.RepairSystem(SystemTypes.Sabotage, data.player, data.amount);
            }
        }
    }
}