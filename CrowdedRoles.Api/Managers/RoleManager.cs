using System.Collections.Generic;
using System.Linq;
using CrowdedRoles.Api.Roles;

namespace CrowdedRoles.Api.Managers
{
    internal static class RoleManager
    {
        public static readonly Dictionary<BaseRole, byte> Limits = new();
        public static Dictionary<BaseRole, byte> EditableLimits =>
            Limits.Where(r => !r.Key.PatchFilterFlags.HasFlag(PatchFilter.AmountOption))
                .ToDictionary(i => i.Key, i => i.Value);

        public static readonly Dictionary<byte, BaseRole> PlayerRoles = new();
        public static readonly Dictionary<string, Dictionary<byte, BaseRole>> Roles = new();

        public static BaseRole? GetRoleByData(RoleData data)
        {
            return Roles[data.pluginId]?[data.localId];
        }
    }
}
