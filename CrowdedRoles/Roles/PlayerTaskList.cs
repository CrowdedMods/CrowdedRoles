using System;
using System.Collections.Generic;
using Hazel;
using Reactor;

namespace CrowdedRoles.Roles
{
    public enum TaskCompletion : byte
    {
        /// <summary>
        /// Required to win by tasks
        /// </summary>
        Required = 0,
        /// <summary>
        /// Player can do a task, but it's not required to win
        /// </summary>
        Optional = 1,
        /// <summary>
        /// Impostor-like faking
        /// </summary>
        Fake = 2
    }
    public class PlayerTaskList
    {
        /// <summary>
        /// Tasks holder
        /// </summary>
        public GameData.PlayerInfo Player { get; }
        private byte Id;
        internal Dictionary<byte, string> StringTasks { get; } = new();
        /// <summary>
        /// Tasks in dictionary id -> Task
        /// </summary>
        public List<GameData.TaskInfo> NormalTasks { get; } = new();
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
                    list.NormalTasks.Add(new GameData.TaskInfo(reader.ReadByte(), reader.ReadByte()));
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
            foreach (var task in NormalTasks)
            {
                writer.Write(task.TypeId);
                writer.Write((byte)task.Id);
            }
        }

        public PlayerTaskList(GameData.PlayerInfo player)
        {
            Player = player;
        }

        /// <summary>
        /// Add a text in a task list
        /// </summary>
        public void AddStringTask(string task)
        {
            StringTasks.Add(Id++, task);
        }

        /// <summary>
        /// Add a few strings in a task list
        /// </summary>
        public void AddStringTasks(IEnumerable<string> tasks)
        {
            foreach (string task in tasks)
            {
                AddStringTask(task);
            }
        }

        /// <summary>
        /// Add an in-game task (Id is being set by API)
        /// </summary>
        public void AddNormalTask(GameData.TaskInfo task)
        {
            task.Id = Id++;
            NormalTasks.Add(task);
        }

        /// <summary>
        /// Add a few in-game tasks
        /// </summary>
        public void AddNormalTasks(IEnumerable<GameData.TaskInfo> tasks)
        {
            foreach (var task in tasks)
            {
                AddNormalTask(task);
            }
        }
    }
}