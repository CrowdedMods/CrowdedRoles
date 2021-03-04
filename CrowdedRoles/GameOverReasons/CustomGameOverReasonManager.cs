using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.IL2CPP;

namespace CrowdedRoles.GameOverReasons
{
    public static class CustomGameOverReasonManager
    {
        public const GameOverReason CustomReasonId = (GameOverReason)(-1);
        private static Dictionary<string, List<CustomGameOverReason>> CustomReasons { get; } = new();
        internal static CustomGameOverReason EndReason { get; set; } = null!; // Among Us style
        internal static List<WinningPlayerData> Winners { get; set; } = null!;
        internal static List<WinningPlayerData> ShownWinners { get; set; } = null!;

        public static void RegisterCustomGameOverReason(CustomGameOverReason reason, BasePlugin plugin)
        {
            string guid = MetadataHelper.GetMetadata(plugin).GUID;
            if (!CustomReasons.TryGetValue(guid, out var crs))
            {
                CustomReasons.Add(guid, crs = new List<CustomGameOverReason>());
            }

            reason.Data.localId = crs.Count;
            reason.Data.pluginId = guid;
            crs.Add(reason);
            CustomReasons[guid] = crs;
        }

        public static void RpcEndGame(CustomGameOverReason reason)
        {
            if (reason.Data.localId == -1)
            {
                throw new Exception($"{reason.Name} is not registered");
            }

            ShipStatus.RpcEndGame(reason, true);
        }

        internal static CustomGameOverReason? ReasonFromData(CustomGameOverReason.GlobalData data)
        {
            return CustomReasons.GetValueOrDefault(data.pluginId)?.ElementAtOrDefault(data.localId);
        }

        public static CustomGameOverReason? ReasonFromType<T>()
        {
            return CustomReasons.SelectMany(p => p.Value).SingleOrDefault(v => v is T);
        }
    }
}