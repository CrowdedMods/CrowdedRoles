using System.Linq;
using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using Hazel;
using Il2CppSystem.Collections.Generic;
using Reactor;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc]
    public class CustomSelectTasks : CustomRpc<RoleApiPlugin, GameData, PlayerTaskList>
    {
        public CustomSelectTasks(RoleApiPlugin plugin) : base(plugin)
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
            for (uint i = 0; i < data.NormalTasks.Count; i++)
            {
                data.Player.Tasks.Add(new GameData.TaskInfo((byte)data.NormalTasks.Values.ElementAt((int)i).Index, i));
                data.Player.Tasks[(int)i].Id = i;
            }
            data.Player.Object.CustomSetTasks(data);
            sender.SetDirtyBit(1u << data.Player.PlayerId);
        }
    }
}