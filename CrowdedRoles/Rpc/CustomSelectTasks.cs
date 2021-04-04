using System;
using System.Linq;
using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using Hazel;
using Il2CppSystem.Collections.Generic;
using Reactor;
using Reactor.Networking;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc((uint)CustomRpcCalls.CustomSelectTasks)]
    internal class CustomSelectTasks : CustomRpc<RoleApiPlugin, GameData, PlayerTaskList>
    {
        public CustomSelectTasks(RoleApiPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public override RpcLocalHandling LocalHandling { get; } = RpcLocalHandling.After;
        
        public override void Write(MessageWriter writer, PlayerTaskList data)
            => data.Serialize(writer);

        public override PlayerTaskList Read(MessageReader reader)
            => PlayerTaskList.Deserialize(reader);

        public override void Handle(GameData sender, PlayerTaskList data)
        {
            if (data.Player.Disconnected || !data.Player.Object)
            {
                return;
            }

            data.Player.Tasks = new List<GameData.TaskInfo>(data.NormalTasks.Count);
            for (var i = 0; i < data.NormalTasks.Count; i++)
            {
                data.Player.Tasks.Add(new GameData.TaskInfo(data.NormalTasks.ElementAt(i).TypeId, (uint)i));
                data.Player.Tasks[(Index) i].Cast<GameData.TaskInfo>().Id = (uint)i;
            }
            Coroutines.Start(data.Player.Object.CustomSetTasks(data));
            sender.SetDirtyBit(1u << data.Player.PlayerId);
        }
    }
}