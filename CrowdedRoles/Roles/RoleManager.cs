using System.Collections.Generic;
using System.Linq;

namespace CrowdedRoles.Roles
{
    internal static class RoleManager
    {
        public static readonly Dictionary<BaseRole, byte> Limits = new();
        public static Dictionary<BaseRole, byte> EditableLimits =>
            Limits.Where(r => !r.Key.PatchFilterFlags.HasFlag(PatchFilter.AmountOption))
                .ToDictionary(i => i.Key, i => i.Value);

        public static readonly Dictionary<byte, BaseRole> PlayerRoles = new();
        public static readonly Dictionary<string, Dictionary<byte, BaseRole>> Roles = new();
        public static readonly Dictionary<byte, TaskCompletion> TaskCompletions = new ();
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
        }
    }
}
