using System;
using System.Collections.Generic;
using Hazel;

namespace CrowdedRoles.Roles
{
    public enum TaskCompletion : byte
    {
        Required = 0,
        Optional = 1,
        Fake = 2
    }
    public class PlayerTaskList
    {
        public GameData.PlayerInfo Player { get; }
        private byte Id;
        internal Dictionary<byte, string> StringTasks { get; } = new();
        public Dictionary<byte, NormalPlayerTask> NormalTasks { get; } = new();
        public TaskCompletion TaskCompletion { get; set; } = TaskCompletion.Required;

        internal static PlayerTaskList Deserialize(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            GameData.PlayerInfo player = GameData.Instance.GetPlayerById(playerId);
            if (player == null)
            {
                throw new NullReferenceException($"Can't find player by id {playerId}");
            }

            var list = new PlayerTaskList(player)
            {
                TaskCompletion = (TaskCompletion) reader.ReadByte()
            };
            {
                int count = reader.ReadPackedInt32();
                for (int i = 0; i < count; i++)
                {
                    list.StringTasks.Add(reader.ReadByte(), reader.ReadString());
                }
            }

            {
                int count = reader.ReadPackedInt32();
                for (int i = 0; i < count; i++)
                {
                    list.NormalTasks.Add(reader.ReadByte(), ShipStatus.Instance.GetTaskById(reader.ReadByte()));
                }
            }

            return list;
        }

        internal void Serialize(MessageWriter writer)
        {
            writer.Write(Player.PlayerId);
            writer.Write((byte)TaskCompletion);
            
            writer.WritePacked(StringTasks.Count);
            foreach (var (id, stringTask) in StringTasks)
            {
                writer.Write(id);
                writer.Write(stringTask);
            }
            
            writer.WritePacked(NormalTasks.Count);
            foreach (var (id, normalTask) in NormalTasks)
            {
                writer.Write(id);
                writer.Write((byte)normalTask.Index);
            }
        }

        public PlayerTaskList(GameData.PlayerInfo player)
        {
            Player = player;
        }

        public void AddStringTask(string task)
        {
            StringTasks.Add(Id++, task);
        }

        public void AddStringTasks(IEnumerable<string> tasks)
        {
            foreach (string task in tasks)
            {
                AddStringTask(task);
            }
        }

        public void AddNormalTask(NormalPlayerTask task)
        {
            NormalTasks.Add(Id++, task);
        }

        public void AddNormalTasks(IEnumerable<NormalPlayerTask> tasks)
        {
            foreach (NormalPlayerTask task in tasks)
            {
                AddNormalTask(task);
            }
        }
    }
}