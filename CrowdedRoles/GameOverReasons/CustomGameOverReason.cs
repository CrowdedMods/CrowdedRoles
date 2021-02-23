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

        protected CustomGameOverReason(BasePlugin plugin)
        {
            CustomGameOverReasonManager.RegisterCustomGameOverReason(this, plugin);
        }

        internal readonly GlobalData Data = new();

        internal void Deserialize(MessageWriter writer)
        {
            writer.Write(Data.pluginId);
            writer.Write(Data.localId);
        }

        internal static CustomGameOverReason Serialize(MessageReader reader)
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
        
        public abstract string Name { get; } // no uses yet
        public abstract string WinText { get; }
        public abstract IEnumerable<GameData.PlayerInfo> Winners { get; }
        public abstract Color GetWinTextColor(bool youWon);
        public abstract Color GetBackgroundColor(bool youWon);
        public virtual IEnumerable<GameData.PlayerInfo> ShownWinners => Winners;

        public virtual AudioClip? GetAudioClip(bool youWon) => null;

        public static implicit operator GameOverReason(CustomGameOverReason _) => CustomGameOverReasonManager.CustomReasonId;
    }
}