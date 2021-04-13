using System.Linq;
using Reactor.Networking;
using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;

namespace CrowdedRoles.Patches
{
    internal static class TaskPatches
    {
        private static readonly int Buckets = Shader.PropertyToID("_Buckets");
        private static readonly int FullBuckets = Shader.PropertyToID("_FullBuckets");

        [HarmonyPatch(typeof(GameData), nameof(GameData.RpcSetTasks))]
        public static class GameData_RpcSetTasks
        {
            public static void Postfix(
                GameData __instance,
                [HarmonyArgument(0)] byte playerId, 
                [HarmonyArgument(1)] ref Il2CppStructArray<byte> tasks
            )
            {
                var player = __instance.GetPlayerById(playerId);
                var role = player.GetRole();
                if (role == null) return;

                var list = new PlayerTaskList(player);
                role.AssignTasks(list, tasks.ToList().ConvertAll(i => new GameData.TaskInfo(i, 0)));
                
                Rpc<CustomSelectTasks>.Instance.Send(GameData.Instance, list);
                // we do not prevent setting default tasks to not break things like Impostor server
            }
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.SetTasks))]
        public static class GameData_SetTasks
        {
            public static bool Prefix(GameData __instance, [HarmonyArgument(0)] byte playerId) => !(__instance.GetPlayerById(playerId)?.HasCustomRole() ?? false); 
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
        public static class GameData_RecomputeTaskCount
        {
            public static bool Prefix(GameData __instance)
            {
                __instance.TotalTasks = 0;
                __instance.CompletedTasks = 0;

                foreach (var task in __instance.AllPlayers
                    .ToArray()
                    .Where(p => !p.Disconnected && p.Tasks != null && p.Object != null && p.GetTaskCompletion() == TaskCompletion.Required)
                    .SelectMany(p => p.Tasks.ToArray()))
                {
                    __instance.TotalTasks++;
                    if (task.Complete)
                    {
                        __instance.CompletedTasks++;
                    }
                }

                return false;
            }
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.FixedUpdate))]
        public static class ProgressTracker_FixedUpdate
        {
            private static void UpdateCurValue(ProgressTracker tracker, int players)
            {
                GameData instance = GameData.Instance;
                
                tracker.curValue = Mathf.Lerp(tracker.curValue,
                    (float)instance.CompletedTasks * players / instance.TotalTasks, Time.fixedDeltaTime * 2f);
            }
            public static bool Prefix(ProgressTracker __instance)
            {
                if (!__instance.TileParent.enabled || !GameData.Instance || GameData.Instance.TotalTasks <= 0) 
                {
                    return true;
                }

                int players = GameData.Instance.AllPlayers.ToArray().Count(p => !p.Disconnected && p.GetTaskCompletion() == TaskCompletion.Required);
                switch (PlayerControl.GameOptions.TaskBarMode)
                {
                    case TaskBarMode.Normal:
                        UpdateCurValue(__instance, players);
                        break;
                    case TaskBarMode.Invisible:
                        __instance.gameObject.SetActive(false);
                        break;
                    case TaskBarMode.MeetingOnly:
                        if (MeetingHud.Instance)
                        {
                            UpdateCurValue(__instance, players);
                        }

                        break;
                }
                
                __instance.TileParent.material.SetFloat(Buckets, players);
                __instance.TileParent.material.SetFloat(FullBuckets, __instance.curValue);

                return false;
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
        public static class Console_CanUse
        {
            public static void Postfix(
                Console __instance,
                [HarmonyArgument(0)] GameData.PlayerInfo pc,
                [HarmonyArgument(1)] ref bool canUse, 
                [HarmonyArgument(2)] ref bool couldUse
            )
            {
                canUse &= couldUse &= __instance.AllowImpostor || pc.GetTaskCompletion() != TaskCompletion.Fake;
            }
        }
    }
}