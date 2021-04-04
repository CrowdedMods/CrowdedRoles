using CrowdedRoles.Extensions;
using Hazel;
using InnerNet;
using Reactor;
using Reactor.Networking;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc((uint)CustomRpcCalls.CmdCustomKill)]
    public class CmdCustomKill : PlayerCustomRpc<RoleApiPlugin, CmdCustomKill.Data>
    {
        public CmdCustomKill(RoleApiPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public struct Data
        {
            public PlayerControl target;
            public CustomMurderOptions options;
        }

        public override RpcLocalHandling LocalHandling { get; } = RpcLocalHandling.None;
        public override void Write(MessageWriter writer, Data data)
        {
            MessageExtensions.WriteNetObject(writer, data.target);
            writer.WritePacked((uint)data.options);
        }

        public override Data Read(MessageReader reader)
            => new()
            {
                target = MessageExtensions.ReadNetObject<PlayerControl>(reader),
                options = (CustomMurderOptions) reader.ReadPackedUInt32()
            };

        public override void Handle(PlayerControl killer, Data data)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                RoleApiPlugin.Logger.LogWarning($"{killer.PlayerId} sent me {nameof(CmdCustomKill)}, but I'm not a host");
                return;
            }
            
            if (!killer.CanKill(null) && !data.options.HasFlag(CustomMurderOptions.Force))
            {
                RoleApiPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill {data.target.PlayerId} with no kill perms");
                return;
            }

            if (AmongUsClient.Instance.IsGameOver)
            {
                RoleApiPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill when game is over");
                return;
            }

            if (data.target == null)
            {
                // ReSharper disable once Unity.NoNullPropagation
                RoleApiPlugin.Logger.LogWarning($"Null kill ({killer.PlayerId} -> {data.target?.PlayerId ?? -1})");
                return;
            }
            
            if(killer.Data.IsDead || killer.Data.Disconnected)
            {
                if (!data.options.HasFlag(CustomMurderOptions.Force))
                {
                    RoleApiPlugin.Logger.LogWarning($"Not allowed kill ({killer.PlayerId} -> {data.target.PlayerId})");
                    return;
                }
                RoleApiPlugin.Logger.LogDebug($"Forced bad kill ({killer.PlayerId} -> {data.target.PlayerId})");
            }

            if (data.target.Data == null || data.target.Data.IsDead)
            {
                RoleApiPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill {data.target.PlayerId}, but they are already dead");
                return;
            }
            
            Rpc<CustomKill>.Instance.Send(new CustomKill.Data
            {
                killer = killer,
                target = data.target,
                options = data.options
            }, true);
        }
    }
}