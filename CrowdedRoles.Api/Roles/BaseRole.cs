using System.Collections.Generic;
using BepInEx;
using BepInEx.IL2CPP;
using UnityEngine;

namespace CrowdedRoles.Api.Roles
{
    public abstract class BaseRole
    {
        internal RoleData Data { get; }
        public bool AbleToKill { get; set; } = false;
        
        public abstract string Name { get; }
        public abstract Color Color { get; }
        
        public virtual Side Side { get; } = Side.Crewmate;
        public virtual Visibility Visibility { get; } = Visibility.Myself;
        public virtual string StartTip { get; } = "Do nothing but [FF0000FF]kiss";
        public virtual PatchFilter PatchFilterFlags { get; } = PatchFilter.None;

        internal BaseRole(BasePlugin plugin)
        {
            var guid = MetadataHelper.GetMetadata(plugin).GUID;
            
            if (!RoleManager.Roles.ContainsKey(guid))
            {
                RoleManager.Roles.Add(guid, new Dictionary<byte, BaseRole>());
            }

            var localRoles = RoleManager.Roles[guid]!;
            Data = new RoleData(guid, (byte)localRoles.Count);
            
            localRoles.Add((byte)localRoles.Count, this);
            RoleManager.Limits.Add(this, 0);
        }
        public virtual string FormatName(string name)
        {
            return name;
        }

        public virtual bool KillFilter(PlayerControl me, GameData.PlayerInfo target)
        {
            return !target.Disconnected &&
                   me.PlayerId != target.PlayerId &&
                   !target.IsDead &&
                   !target.IsImpostor;
        }

        public static bool operator ==(BaseRole? me, BaseRole? other) => me?.Data == other?.Data;
        public static bool operator !=(BaseRole? me, BaseRole? other) => me?.Data != other?.Data;
        private bool Equals(BaseRole other)
        {
            return Data.Equals(other.Data);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) &&
                   obj.GetType() == this.GetType() && 
                   Equals((BaseRole) obj);
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }
}