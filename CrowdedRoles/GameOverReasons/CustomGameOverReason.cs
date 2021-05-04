using System;
using System.Collections.Generic;
using BepInEx.IL2CPP;
using Hazel;
using UnityEngine;

namespace CrowdedRoles.GameOverReasons
{
    public abstract class CustomGameOverReason
    {
        internal class GlobalData
        {
            public string pluginId = "broken";
            public int localId = -1;
        }

        protected internal CustomGameOverReason(BasePlugin plugin)
        {
            CustomGameOverReasonManager.RegisterCustomGameOverReason(this, plugin);
        }

        internal readonly GlobalData Data = new();

        internal void Serialize(MessageWriter writer)
        {
            writer.Write(Data.pluginId);
            writer.Write(Data.localId);
        }

        internal static CustomGameOverReason Deserialize(MessageReader reader)
        {
            var data = new GlobalData
            {
                pluginId = reader.ReadString(),
                localId = reader.ReadInt32()
            };
            CustomGameOverReason? result = CustomGameOverReasonManager.ReasonFromData(data);
            if (result == null)
            {
                throw new NullReferenceException($"Cannot find {nameof(CustomGameOverReason)} with data {data.pluginId}:{data.localId}");
            }

            return result;

        }
        
        /// <summary>
        /// Not used yet.
        /// </summary>
        public abstract string Name { get; }
        public abstract string WinText { get; }
        /// <summary>
        /// Players who will actually win
        /// </summary>
        public abstract IEnumerable<GameData.PlayerInfo> Winners { get; }
        public abstract Color GetWinTextColor(bool youWon);
        public abstract Color GetBackgroundColor(bool youWon);
        
        /// <summary>
        /// Players who will be showed on end screen
        /// </summary>
        public virtual IEnumerable<GameData.PlayerInfo> ShownWinners => Winners;
        public virtual AudioClip? Stinger => null;

        public static implicit operator GameOverReason(CustomGameOverReason _) => CustomGameOverReasonManager.CustomReasonId;
    }
}