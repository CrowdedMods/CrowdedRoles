using System.Collections.Generic;

namespace CrowdedRoles.Api.Roles
{
    public static class RoleManager
    {
        private static readonly Dictionary<byte, CustomRole?> _roles = new Dictionary<byte, CustomRole?>();
        internal static readonly Dictionary<byte, byte> Limits = new Dictionary<byte, byte>();

        public static void AddRole(CustomRole role)
        {
            role.Id = (byte)_roles.Count;
            _roles.Add(role.Id, role);
            Limits.Add(role.Id, 0);
        }

        public static CustomRole? GetRoleById(byte roleId)
        {
            return _roles.GetValueOrDefault(roleId);
        }
    }
}
