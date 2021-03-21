using System.Collections.Generic;
using System.Linq;
using CrowdedRoles.UI;

namespace CrowdedRoles.Roles
{
    internal static class RoleManager
    {
        public static readonly Dictionary<BaseRole, byte> Limits = new();
        public static Dictionary<BaseRole, byte> EditableLimits =>
            Limits.Where(r => !r.Key.PatchFilterFlags.HasFlag(PatchFilter.AmountOption))
                .ToDictionary(i => i.Key, i => i.Value);

        public static Dictionary<byte, BaseRole> PlayerRoles { get; } = new();
        public static Dictionary<string, Dictionary<byte, BaseRole>> Roles { get; } = new();
        public static Dictionary<byte, TaskCompletion> TaskCompletions { get; } = new ();
        public static bool rolesSet;

        public static BaseRole? GetRoleByData(RoleData data)
        {
            return Roles[data.pluginId]?[data.localId];
        }

        public static void GameEnded()
        {
            PlayerRoles.Clear();
            TaskCompletions.Clear();
            rolesSet = false;
            ButtonManager.ResetButtons();
        }
    }
}
