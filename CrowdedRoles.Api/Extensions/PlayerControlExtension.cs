using System;
using System.Collections.Generic;
using CrowdedRoles.Api.Roles;

namespace CrowdedRoles.Api.Extensions
{
    public static class PlayerControlExtension
    {
        public static void InitRole(this PlayerControl player, BaseRole? role = null)
        {
            if (role is null) return;
            try
            {
                RoleManager.PlayerRoles.Add(player.PlayerId, role);
            }
            catch(ArgumentException)
            {
                MainPlugin.Logger.LogWarning($"{role.Name} already exists in {nameof(RoleManager.PlayerRoles)}, redefining...");
                RoleManager.PlayerRoles[player.PlayerId] = role;
            }
        }

        public static BaseRole? GetRole(this PlayerControl player)
        {
            return RoleManager.PlayerRoles.GetValueOrDefault(player.PlayerId);
        }

        public static T? GetRole<T>(this PlayerControl player) where T : BaseRole
        {
            BaseRole? baseRole = player.GetRole();
            return baseRole is T role ? role : null;
        }

        public static bool Is<T>(this PlayerControl player) where T : BaseRole
        {
            return player.GetRole<T>() != null;
        }

        public static bool IsTeamedWith(this PlayerControl me, byte other) 
            => me.IsTeamedWith(GameData.Instance.GetPlayerById(other));

        public static bool IsTeamedWith(this PlayerControl me, GameData.PlayerInfo other)
        {
            var myRole = me.GetRole();
            if(myRole == null)
            {
                return me.Data.IsImpostor == other.IsImpostor;
            }
            return myRole.Equals(other.Object.GetRole());
        }
        
        public static List<PlayerControl> GetTeam(this PlayerControl player)
        {
            var result = new List<PlayerControl>();
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if(p != null && player.IsTeamedWith(p.PlayerId))
                {
                    result.Add(p);
                }
            }

            return result;
        }

        public static bool CanSee(this PlayerControl me, PlayerControl whom)
        {
            Visibility? state = whom.GetRole()?.Visibility;
            return state switch
            {
                Visibility.Myself => me.PlayerId == whom.PlayerId,
                Visibility.Team => whom.IsTeamedWith(me.PlayerId),
                Visibility.Everyone => true,
                _ => false
            };
        }
    }
}
