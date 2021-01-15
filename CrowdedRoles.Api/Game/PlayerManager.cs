using System.Collections.Generic;
using CrowdedRoles.Api.Roles;

namespace CrowdedRoles.Api.Game
{
    public static class PlayerManager
    {
        private static readonly Dictionary<byte, CustomRole> _playerRoles = new Dictionary<byte, CustomRole>();

        public static void GameEnded()
        {
            _playerRoles.Clear();
        }

        public static void InitPlayer(byte player, CustomRole? role = null)
        {
            if(role != null)
            {
                _playerRoles.Add(player, role);
            }
        }

        // only for custom roles
        public static bool IsTeamedWith(byte person1, byte person2)
        {
            var role1 = GetRole(person1);
            if (role1 == null) return false;
            return role1.Id == GetRole(person2)?.Id;
        }

        public static List<PlayerControl> GetTeam(byte playerId)
        {
            var result = new List<PlayerControl>();
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if(IsTeamedWith(playerId, p.PlayerId))
                {
                    result.Add(p);
                }
            }

            return result;
        }

        public static CustomRole? GetRole(byte playerId)
        {
            return _playerRoles.GetValueOrDefault(playerId);
        }
    }
}
