using System.Collections.Generic;

namespace CrowdedRoles.Api.Roles
{
    internal static class RoleManager
    {
        public static readonly Dictionary<BaseRole, byte> Limits = new();
       
        public static readonly Dictionary<byte, BaseRole> PlayerRoles = new();
        public static readonly Dictionary<string, Dictionary<byte, BaseRole>> Roles = new();
        
        public static void AddRole(BaseRole role)
        {
            
        }

        public static BaseRole? GetRoleByData(RoleData data)
        {
            return Roles[data.pluginId]?[data.localId];
        }
    }
}
